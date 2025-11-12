using Microsoft.AspNetCore.Mvc;
using DistributedDbApi.Services;
using DistributedDbApi.DTOs;

namespace DistributedDbApi.Controllers;

/// <summary>
/// ReportsController - Báo cáo và phân tích trên CSDL phân tán
/// Thể hiện: Distributed aggregation, Multi-site JOIN, Fan-out/Fan-in patterns
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Báo cáo học bổng theo khoa
    /// </summary>
    /// <remarks>
    /// Tính năng phân tán:
    /// - Fan-out query: Query Site 3 (K1) và/hoặc Site 4 (K2)
    /// - Predicate pushdown: Filter minAmount tại database sites
    /// - Fan-in aggregation: Merge + sort kết quả từ 2 sites tại Gateway
    /// - Optimization: Nếu chỉ định khoa → query chỉ 1 site
    /// 
    /// Cost: 
    /// - Có khoa: 1 site access
    /// - Không khoa: 2 site accesses + merge overhead
    /// </remarks>
    [HttpGet("scholarships")]
    [ProducesResponseType(typeof(ApiResponse<List<ScholarshipReportDto>>), 200)]
    public async Task<IActionResult> GetScholarships(
        [FromQuery] string? khoa = null,
        [FromQuery] decimal minAmount = 0,
        [FromQuery] int top = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (top > 100) top = 100;

            var results = await _reportService.GetScholarshipsReportAsync(khoa, minAmount, top, ct);

            return Ok(new ApiResponse<List<ScholarshipReportDto>>(
                true,
                results,
                $"Tìm thấy {results.Count} sinh viên có học bổng"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo báo cáo học bổng");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Báo cáo điểm trung bình theo môn và khoa
    /// </summary>
    /// <remarks>
    /// Tính năng phân tán (PHỨC TẠP NHẤT):
    /// - Multi-site JOIN: JOIN 3 tables từ 3 sites (5, 6/7)
    /// - Distributed aggregation: AVG calculation tại Gateway sau khi JOIN
    /// - Fan-out: Query Site 5 + Site 6 + Site 7
    /// - Semi-join: Chỉ transfer data cần thiết (theo msmon filter)
    /// 
    /// Execution plan:
    /// 1. Query Site 5 (DangKyDiem1) với filter msmon → diem1 data
    /// 2. Parallel query Site 6 + Site 7 với filter msmon → diem2/3 data
    /// 3. Gateway JOIN: diem1 ⋈ diem23 on (mssv, msmon)
    /// 4. Gateway AGG: GROUP BY msmon, khoa + AVG(diem1, diem2, diem3)
    /// 
    /// Cost: 3 site accesses + JOIN + AGG tại Gateway
    /// </remarks>
    [HttpGet("averages")]
    [ProducesResponseType(typeof(ApiResponse<List<AverageScoreReportDto>>), 200)]
    public async Task<IActionResult> GetAverageScores(
        [FromQuery] string? khoa = null,
        [FromQuery] string? msmon = null,
        CancellationToken ct = default)
    {
        try
        {
            var results = await _reportService.GetAverageScoresAsync(khoa, msmon, ct);

            return Ok(new ApiResponse<List<AverageScoreReportDto>>(
                true,
                results,
                $"Điểm trung bình của {results.Count} môn học"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo báo cáo điểm TB");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Báo cáo sinh viên rớt môn (điểm TB &lt; threshold)
    /// </summary>
    /// <remarks>
    /// Tính năng phân tán:
    /// - Multi-site JOIN: 4 sites (Site 3/4 + Site 5 + Site 6/7)
    /// - Distributed filtering: Filter điểm TB &lt; threshold SAU KHI JOIN
    /// - Complex query: JOIN → CALCULATE → FILTER → JOIN (student info)
    /// 
    /// Query flow:
    /// 1. Site 5 (diem1) ⋈ Site 6/7 (diem23) → scores với diem1/2/3
    /// 2. Gateway calculate: diemTB = (d1 + d2 + d3) / 3
    /// 3. Gateway filter: WHERE diemTB &lt; threshold
    /// 4. Gateway ⋈ Site 3/4 (student) → add hoten, khoa
    /// 
    /// Cost: 4 site accesses + 2 JOINs + calculation
    /// </remarks>
    [HttpGet("failures")]
    [ProducesResponseType(typeof(ApiResponse<List<FailureReportDto>>), 200)]
    public async Task<IActionResult> GetFailures(
        [FromQuery] decimal threshold = 5.0m,
        [FromQuery] string? khoa = null,
        [FromQuery] string? msmon = null,
        CancellationToken ct = default)
    {
        try
        {
            if (threshold < 0 || threshold > 10)
            {
                return BadRequest(new ApiResponse<object>(
                    false,
                    null,
                    "Threshold phải từ 0 đến 10"));
            }

            var results = await _reportService.GetFailuresReportAsync(threshold, khoa, msmon, ct);

            return Ok(new ApiResponse<List<FailureReportDto>>(
                true,
                results,
                $"Tìm thấy {results.Count} sinh viên có điểm < {threshold}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo báo cáo rớt môn");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Phân bố điểm theo môn (histogram)
    /// </summary>
    /// <remarks>
    /// Tính năng phân tán:
    /// - Multi-site aggregation với histogram
    /// - Distributed GROUP BY: Nhóm theo range điểm (0-4, 4-5, 5-6.5, 6.5-8, 8-10)
    /// - Fan-out + Fan-in: Query 3 sites → merge → aggregate
    /// 
    /// Execution:
    /// 1. Query Site 5 + Site 6/7 → JOIN → calculate diemTB
    /// 2. Gateway aggregate: COUNT theo ranges
    /// 3. Return histogram cho K1 và K2
    /// 
    /// Use case: Phân tích chất lượng đầu ra, độ khó môn học
    /// </remarks>
    [HttpGet("distribution")]
    [ProducesResponseType(typeof(ApiResponse<ScoreDistributionReportDto>), 200)]
    public async Task<IActionResult> GetScoreDistribution(
        [FromQuery] string msmon,
        [FromQuery] string? khoa = null,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(msmon))
            {
                return BadRequest(new ApiResponse<object>(
                    false,
                    null,
                    "Mã môn là bắt buộc"));
            }

            var result = await _reportService.GetScoreDistributionAsync(msmon, khoa, ct);

            return Ok(new ApiResponse<ScoreDistributionReportDto>(
                true,
                result,
                $"Phân bố điểm môn {msmon}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo phân bố điểm");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }
}
