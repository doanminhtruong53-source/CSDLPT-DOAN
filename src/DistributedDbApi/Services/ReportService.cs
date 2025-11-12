using Microsoft.EntityFrameworkCore;
using DistributedDbApi.Data.DbContexts;
using DistributedDbApi.DTOs;

namespace DistributedDbApi.Services;

/// <summary>
/// ReportService - Các tính năng báo cáo/phân tích trên CSDL phân tán
/// Thể hiện: Aggregation, Fan-out, Fan-in, Cross-site JOIN
/// </summary>
public class ReportService
{
    private readonly LopK1DbContext _lopK1Db;
    private readonly LopK2DbContext _lopK2Db;
    private readonly SinhVienK1DbContext _svK1Db;
    private readonly SinhVienK2DbContext _svK2Db;
    private readonly DangKyDiem1DbContext _diem1Db;
    private readonly DangKyDiem23K1DbContext _diem23K1Db;
    private readonly DangKyDiem23K2DbContext _diem23K2Db;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        LopK1DbContext lopK1Db,
        LopK2DbContext lopK2Db,
        SinhVienK1DbContext svK1Db,
        SinhVienK2DbContext svK2Db,
        DangKyDiem1DbContext diem1Db,
        DangKyDiem23K1DbContext diem23K1Db,
        DangKyDiem23K2DbContext diem23K2Db,
        ILogger<ReportService> logger)
    {
        _lopK1Db = lopK1Db;
        _lopK2Db = lopK2Db;
        _svK1Db = svK1Db;
        _svK2Db = svK2Db;
        _diem1Db = diem1Db;
        _diem23K1Db = diem23K1Db;
        _diem23K2Db = diem23K2Db;
        _logger = logger;
    }

    /// <summary>
    /// Báo cáo học bổng theo khoa
    /// DISTRIBUTED AGGREGATION: Query 2 sites, filter, aggregate
    /// </summary>
    public async Task<List<ScholarshipReportDto>> GetScholarshipsReportAsync(
        string? khoa = null,
        decimal minAmount = 0,
        int top = 20,
        CancellationToken ct = default)
    {
        try
        {
            var results = new List<ScholarshipReportDto>();

            if (string.IsNullOrEmpty(khoa) || khoa.ToUpper() == "K1")
            {
                // Query Site 3 (SinhVienK1)
                var k1Students = await _svK1Db.SinhVienK1
                    .Where(s => s.Hocbong >= minAmount)
                    .OrderByDescending(s => s.Hocbong)
                    .Take(top)
                    .ToListAsync(ct);

                results.AddRange(k1Students.Select(s => new ScholarshipReportDto
                {
                    Mssv = s.Mssv,
                    Hoten = s.Hoten,
                    Khoa = "K1",
                    Mslop = s.Mslop,
                    Hocbong = s.Hocbong ?? 0
                }));
            }

            if (string.IsNullOrEmpty(khoa) || khoa.ToUpper() == "K2")
            {
                // Query Site 4 (SinhVienK2)
                var k2Students = await _svK2Db.SinhVienK2
                    .Where(s => s.Hocbong >= minAmount)
                    .OrderByDescending(s => s.Hocbong)
                    .Take(top)
                    .ToListAsync(ct);

                results.AddRange(k2Students.Select(s => new ScholarshipReportDto
                {
                    Mssv = s.Mssv,
                    Hoten = s.Hoten,
                    Khoa = "K2",
                    Mslop = s.Mslop,
                    Hocbong = s.Hocbong ?? 0
                }));
            }

            // Fan-in: Sort và lấy top
            return results
                .OrderByDescending(s => s.Hocbong)
                .Take(top)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy báo cáo học bổng");
            throw;
        }
    }

    /// <summary>
    /// Báo cáo điểm trung bình theo môn và khoa
    /// DISTRIBUTED JOIN + AGGREGATION: 3-site join với AVG calculation
    /// </summary>
    public async Task<List<AverageScoreReportDto>> GetAverageScoresAsync(
        string? khoa = null,
        string? msmon = null,
        CancellationToken ct = default)
    {
        try
        {
            var results = new List<AverageScoreReportDto>();

            // Query Site 5 (Diem1) - Base data
            var diem1Query = _diem1Db.DangKyDiem1.AsQueryable();
            if (!string.IsNullOrEmpty(msmon))
            {
                diem1Query = diem1Query.Where(d => d.Msmon == msmon);
            }
            var diem1List = await diem1Query.ToListAsync(ct);

            // Group by môn học
            var subjects = diem1List.Select(d => d.Msmon).Distinct();

            foreach (var subject in subjects)
            {
                // Lấy điểm của môn này
                var subjectDiem1 = diem1List.Where(d => d.Msmon == subject).ToList();
                
                // Query diem2/3 từ Site 6 và 7
                var diem23K1List = await _diem23K1Db.DangKyDiem23K1
                    .Where(d => d.Msmon == subject)
                    .ToListAsync(ct);
                
                var diem23K2List = await _diem23K2Db.DangKyDiem23K2
                    .Where(d => d.Msmon == subject)
                    .ToListAsync(ct);

                // JOIN và tính TB cho K1
                if (string.IsNullOrEmpty(khoa) || khoa.ToUpper() == "K1")
                {
                    var k1Scores = subjectDiem1
                        .Join(diem23K1List,
                            d1 => new { d1.Mssv, d1.Msmon },
                            d23 => new { d23.Mssv, d23.Msmon },
                            (d1, d23) => new
                            {
                                d1.Diem1,
                                d23.Diem2,
                                d23.Diem3,
                                DiemTB = ((d1.Diem1 ?? 0) + (d23.Diem2 ?? 0) + (d23.Diem3 ?? 0)) / 3m
                            })
                        .ToList();

                    if (k1Scores.Any())
                    {
                        results.Add(new AverageScoreReportDto
                        {
                            Msmon = subject,
                            Khoa = "K1",
                            TotalStudents = k1Scores.Count,
                            AvgDiem1 = k1Scores.Average(s => s.Diem1 ?? 0),
                            AvgDiem2 = k1Scores.Average(s => s.Diem2 ?? 0),
                            AvgDiem3 = k1Scores.Average(s => s.Diem3 ?? 0),
                            AvgTotal = k1Scores.Average(s => s.DiemTB)
                        });
                    }
                }

                // JOIN và tính TB cho K2
                if (string.IsNullOrEmpty(khoa) || khoa.ToUpper() == "K2")
                {
                    var k2Scores = subjectDiem1
                        .Join(diem23K2List,
                            d1 => new { d1.Mssv, d1.Msmon },
                            d23 => new { d23.Mssv, d23.Msmon },
                            (d1, d23) => new
                            {
                                d1.Diem1,
                                d23.Diem2,
                                d23.Diem3,
                                DiemTB = ((d1.Diem1 ?? 0) + (d23.Diem2 ?? 0) + (d23.Diem3 ?? 0)) / 3m
                            })
                        .ToList();

                    if (k2Scores.Any())
                    {
                        results.Add(new AverageScoreReportDto
                        {
                            Msmon = subject,
                            Khoa = "K2",
                            TotalStudents = k2Scores.Count,
                            AvgDiem1 = k2Scores.Average(s => s.Diem1 ?? 0),
                            AvgDiem2 = k2Scores.Average(s => s.Diem2 ?? 0),
                            AvgDiem3 = k2Scores.Average(s => s.Diem3 ?? 0),
                            AvgTotal = k2Scores.Average(s => s.DiemTB)
                        });
                    }
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tính điểm trung bình");
            throw;
        }
    }

    /// <summary>
    /// Báo cáo sinh viên rớt môn (điểm TB < threshold)
    /// DISTRIBUTED FILTERING: Filter trên nhiều sites
    /// </summary>
    public async Task<List<FailureReportDto>> GetFailuresReportAsync(
        decimal threshold = 5.0m,
        string? khoa = null,
        string? msmon = null,
        CancellationToken ct = default)
    {
        try
        {
            var results = new List<FailureReportDto>();

            // Query diem1 từ Site 5
            var diem1Query = _diem1Db.DangKyDiem1.AsQueryable();
            if (!string.IsNullOrEmpty(msmon))
            {
                diem1Query = diem1Query.Where(d => d.Msmon == msmon);
            }
            var diem1List = await diem1Query.ToListAsync(ct);

            // Query K1
            if (string.IsNullOrEmpty(khoa) || khoa.ToUpper() == "K1")
            {
                var diem23K1List = await _diem23K1Db.DangKyDiem23K1.ToListAsync(ct);
                if (!string.IsNullOrEmpty(msmon))
                {
                    diem23K1List = diem23K1List.Where(d => d.Msmon == msmon).ToList();
                }

                var k1Students = await _svK1Db.SinhVienK1.ToListAsync(ct);

                var k1Failures = diem1List
                    .Join(diem23K1List,
                        d1 => new { d1.Mssv, d1.Msmon },
                        d23 => new { d23.Mssv, d23.Msmon },
                        (d1, d23) => new
                        {
                            d1.Mssv,
                            d1.Msmon,
                            DiemTB = ((d1.Diem1 ?? 0) + (d23.Diem2 ?? 0) + (d23.Diem3 ?? 0)) / 3m,
                            d1.Diem1,
                            d23.Diem2,
                            d23.Diem3
                        })
                    .Where(r => r.DiemTB < threshold)
                    .Join(k1Students,
                        r => r.Mssv,
                        s => s.Mssv,
                        (r, s) => new FailureReportDto
                        {
                            Mssv = r.Mssv,
                            Hoten = s.Hoten,
                            Khoa = "K1",
                            Msmon = r.Msmon,
                            Diem1 = r.Diem1 ?? 0,
                            Diem2 = r.Diem2 ?? 0,
                            Diem3 = r.Diem3 ?? 0,
                            DiemTB = r.DiemTB
                        })
                    .ToList();

                results.AddRange(k1Failures);
            }

            // Query K2
            if (string.IsNullOrEmpty(khoa) || khoa.ToUpper() == "K2")
            {
                var diem23K2List = await _diem23K2Db.DangKyDiem23K2.ToListAsync(ct);
                if (!string.IsNullOrEmpty(msmon))
                {
                    diem23K2List = diem23K2List.Where(d => d.Msmon == msmon).ToList();
                }

                var k2Students = await _svK2Db.SinhVienK2.ToListAsync(ct);

                var k2Failures = diem1List
                    .Join(diem23K2List,
                        d1 => new { d1.Mssv, d1.Msmon },
                        d23 => new { d23.Mssv, d23.Msmon },
                        (d1, d23) => new
                        {
                            d1.Mssv,
                            d1.Msmon,
                            DiemTB = ((d1.Diem1 ?? 0) + (d23.Diem2 ?? 0) + (d23.Diem3 ?? 0)) / 3m,
                            d1.Diem1,
                            d23.Diem2,
                            d23.Diem3
                        })
                    .Where(r => r.DiemTB < threshold)
                    .Join(k2Students,
                        r => r.Mssv,
                        s => s.Mssv,
                        (r, s) => new FailureReportDto
                        {
                            Mssv = r.Mssv,
                            Hoten = s.Hoten,
                            Khoa = "K2",
                            Msmon = r.Msmon,
                            Diem1 = r.Diem1 ?? 0,
                            Diem2 = r.Diem2 ?? 0,
                            Diem3 = r.Diem3 ?? 0,
                            DiemTB = r.DiemTB
                        })
                    .ToList();

                results.AddRange(k2Failures);
            }

            return results.OrderBy(r => r.DiemTB).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo báo cáo rớt môn");
            throw;
        }
    }

    /// <summary>
    /// Thống kê phân bố điểm theo môn
    /// DISTRIBUTED AGGREGATION với histogram
    /// </summary>
    public async Task<ScoreDistributionReportDto> GetScoreDistributionAsync(
        string msmon,
        string? khoa = null,
        CancellationToken ct = default)
    {
        try
        {
            var report = new ScoreDistributionReportDto
            {
                Msmon = msmon
            };

            // Query diem1
            var diem1List = await _diem1Db.DangKyDiem1
                .Where(d => d.Msmon == msmon)
                .ToListAsync(ct);

            if (string.IsNullOrEmpty(khoa) || khoa.ToUpper() == "K1")
            {
                var diem23List = await _diem23K1Db.DangKyDiem23K1
                    .Where(d => d.Msmon == msmon)
                    .ToListAsync(ct);

                var k1Scores = diem1List
                    .Join(diem23List,
                        d1 => new { d1.Mssv, d1.Msmon },
                        d23 => new { d23.Mssv, d23.Msmon },
                        (d1, d23) => ((d1.Diem1 ?? 0) + (d23.Diem2 ?? 0) + (d23.Diem3 ?? 0)) / 3m)
                    .ToList();

                report.K1_Count = k1Scores.Count;
                report.K1_Avg = k1Scores.Any() ? k1Scores.Average() : 0;
                report.K1_0_4 = k1Scores.Count(s => s < 4);
                report.K1_4_5 = k1Scores.Count(s => s >= 4 && s < 5);
                report.K1_5_6_5 = k1Scores.Count(s => s >= 5 && s < 6.5m);
                report.K1_6_5_8 = k1Scores.Count(s => s >= 6.5m && s < 8);
                report.K1_8_10 = k1Scores.Count(s => s >= 8);
            }

            if (string.IsNullOrEmpty(khoa) || khoa.ToUpper() == "K2")
            {
                var diem23List = await _diem23K2Db.DangKyDiem23K2
                    .Where(d => d.Msmon == msmon)
                    .ToListAsync(ct);

                var k2Scores = diem1List
                    .Join(diem23List,
                        d1 => new { d1.Mssv, d1.Msmon },
                        d23 => new { d23.Mssv, d23.Msmon },
                        (d1, d23) => ((d1.Diem1 ?? 0) + (d23.Diem2 ?? 0) + (d23.Diem3 ?? 0)) / 3m)
                    .ToList();

                report.K2_Count = k2Scores.Count;
                report.K2_Avg = k2Scores.Any() ? k2Scores.Average() : 0;
                report.K2_0_4 = k2Scores.Count(s => s < 4);
                report.K2_4_5 = k2Scores.Count(s => s >= 4 && s < 5);
                report.K2_5_6_5 = k2Scores.Count(s => s >= 5 && s < 6.5m);
                report.K2_6_5_8 = k2Scores.Count(s => s >= 6.5m && s < 8);
                report.K2_8_10 = k2Scores.Count(s => s >= 8);
            }

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo phân bố điểm");
            throw;
        }
    }
}
