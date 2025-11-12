using Microsoft.AspNetCore.Mvc;
using DistributedDbApi.Services;
using DistributedDbApi.DTOs;

namespace DistributedDbApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly ClassService _classService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AdminService adminService, ClassService classService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _classService = classService;
        _logger = logger;
    }

    /// <summary>
    /// Kiểm tra health của 7 sites
    /// </summary>
    [HttpGet("sites/health")]
    [ProducesResponseType(typeof(ApiResponse<List<SiteHealthDto>>), 200)]
    public async Task<IActionResult> GetSitesHealth(CancellationToken ct)
    {
        try
        {
            var results = await _adminService.GetSitesHealthAsync(ct);
            var healthyCount = results.Count(r => r.Status == "Healthy");
            
            return Ok(new ApiResponse<List<SiteHealthDto>>(
                true, 
                results, 
                $"{healthyCount}/{results.Count} sites healthy"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi kiểm tra health");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Lấy tổng quan hệ thống
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<OverviewDto>), 200)]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        try
        {
            var result = await _adminService.GetOverviewAsync(ct);
            return Ok(new ApiResponse<OverviewDto>(true, result, "Thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy overview");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Lấy danh sách khoa và thống kê
    /// </summary>
    [HttpGet("departments")]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentSummaryDto>>), 200)]
    public async Task<IActionResult> GetDepartments(CancellationToken ct)
    {
        try
        {
            var results = await _classService.GetDepartmentsAsync(ct);
            return Ok(new ApiResponse<List<DepartmentSummaryDto>>(true, results, "Thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách khoa");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Lấy cấu hình mapping site/port/fragment
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, string>>), 200)]
    public IActionResult GetConfig()
    {
        try
        {
            var config = _adminService.GetConfigMapping();
            return Ok(new ApiResponse<Dictionary<string, string>>(true, config, "Thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy config");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }
}
