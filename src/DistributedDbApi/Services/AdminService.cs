using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using DistributedDbApi.Data.DbContexts;
using DistributedDbApi.DTOs;

namespace DistributedDbApi.Services;

public class AdminService
{
    private readonly LopK1DbContext _lopK1Db;
    private readonly LopK2DbContext _lopK2Db;
    private readonly SinhVienK1DbContext _svK1Db;
    private readonly SinhVienK2DbContext _svK2Db;
    private readonly DangKyDiem1DbContext _diem1Db;
    private readonly DangKyDiem23K1DbContext _diem23K1Db;
    private readonly DangKyDiem23K2DbContext _diem23K2Db;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        LopK1DbContext lopK1Db,
        LopK2DbContext lopK2Db,
        SinhVienK1DbContext svK1Db,
        SinhVienK2DbContext svK2Db,
        DangKyDiem1DbContext diem1Db,
        DangKyDiem23K1DbContext diem23K1Db,
        DangKyDiem23K2DbContext diem23K2Db,
        ILogger<AdminService> logger)
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

    public async Task<List<SiteHealthDto>> GetSitesHealthAsync(CancellationToken ct = default)
    {
        var results = new List<SiteHealthDto>();

        // Test từng site
        results.Add(await CheckSiteHealthAsync("Site 1: LopK1DB (5439)", 
            async () => await _lopK1Db.LopK1.CountAsync(ct), ct));
            
        results.Add(await CheckSiteHealthAsync("Site 2: LopK2DB (5433)", 
            async () => await _lopK2Db.LopK2.CountAsync(ct), ct));
            
        results.Add(await CheckSiteHealthAsync("Site 3: SinhVienK1DB (5434)", 
            async () => await _svK1Db.SinhVienK1.CountAsync(ct), ct));
            
        results.Add(await CheckSiteHealthAsync("Site 4: SinhVienK2DB (5435)", 
            async () => await _svK2Db.SinhVienK2.CountAsync(ct), ct));
            
        results.Add(await CheckSiteHealthAsync("Site 5: DangKyDiem1DB (5436)", 
            async () => await _diem1Db.DangKyDiem1.CountAsync(ct), ct));
            
        results.Add(await CheckSiteHealthAsync("Site 6: DangKyDiem23K1DB (5437)", 
            async () => await _diem23K1Db.DangKyDiem23K1.CountAsync(ct), ct));
            
        results.Add(await CheckSiteHealthAsync("Site 7: DangKyDiem23K2DB (5438)", 
            async () => await _diem23K2Db.DangKyDiem23K2.CountAsync(ct), ct));

        return results;
    }

    private async Task<SiteHealthDto> CheckSiteHealthAsync(string siteName, Func<Task<int>> testQuery, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await testQuery();
            sw.Stop();
            return new SiteHealthDto(siteName, "Healthy", sw.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Health check failed for {Site}", siteName);
            return new SiteHealthDto(siteName, "Unhealthy", sw.ElapsedMilliseconds, ex.Message);
        }
    }

    public async Task<OverviewDto> GetOverviewAsync(CancellationToken ct = default)
    {
        try
        {
            var taskLopK1 = _lopK1Db.LopK1.CountAsync(ct);
            var taskLopK2 = _lopK2Db.LopK2.CountAsync(ct);
            var taskSvK1 = _svK1Db.SinhVienK1.CountAsync(ct);
            var taskSvK2 = _svK2Db.SinhVienK2.CountAsync(ct);
            var taskDiem1 = _diem1Db.DangKyDiem1.CountAsync(ct);

            await Task.WhenAll(taskLopK1, taskLopK2, taskSvK1, taskSvK2, taskDiem1);

            var departments = new List<DepartmentSummaryDto>
            {
                new("K1", taskLopK1.Result, taskSvK1.Result),
                new("K2", taskLopK2.Result, taskSvK2.Result)
            };

            return new OverviewDto(
                TotalDepartments: 2,
                Departments: departments,
                TotalRegistrations: taskDiem1.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy overview");
            throw;
        }
    }

    public Dictionary<string, string> GetConfigMapping()
    {
        return new Dictionary<string, string>
        {
            ["Site 1"] = "LopK1DB - localhost:5439 - lop_k1 (Lớp Khoa K1)",
            ["Site 2"] = "LopK2DB - localhost:5433 - lop_k2 (Lớp Khoa K2)",
            ["Site 3"] = "SinhVienK1DB - localhost:5434 - sinhvien_k1 (Sinh viên K1)",
            ["Site 4"] = "SinhVienK2DB - localhost:5435 - sinhvien_k2 (Sinh viên K2)",
            ["Site 5"] = "DangKyDiem1DB - localhost:5436 - dangky_diem1 (Điểm lần 1 - tất cả SV)",
            ["Site 6"] = "DangKyDiem23K1DB - localhost:5437 - dangky_diem23_k1 (Điểm 2/3 - K1)",
            ["Site 7"] = "DangKyDiem23K2DB - localhost:5438 - dangky_diem23_k2 (Điểm 2/3 - K2)"
        };
    }
}
