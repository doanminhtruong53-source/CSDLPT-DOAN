namespace DistributedDbApi.DTOs;

public record ClassDto(string Mslop, string Tenlop, string Khoa);

public record StudentDto(
    string Mssv,
    string Hoten,
    string? Phai,
    DateTime? Ngaysinh,
    string Mslop,
    string? Khoa,
    decimal? Hocbong);

public record StudentWithKhoaDto(string Mssv, string Khoa);

public record RegistrationScoreDto(
    string Mssv,
    string Msmon,
    decimal? Diem1,
    decimal? Diem2,
    decimal? Diem3,
    string? Hoten = null,
    string? Khoa = null);

public record DepartmentSummaryDto(string Khoa, int TotalClasses, int TotalStudents);

public record OverviewDto(
    int TotalDepartments,
    List<DepartmentSummaryDto> Departments,
    int TotalRegistrations);

public record SiteHealthDto(string Site, string Status, long? LatencyMs, string? Error);

public record ApiResponse<T>(bool Success, T? Data, string? Message, List<string>? Warnings = null);

public record ErrorResponse(string Code, string Message, object? Details = null);

// ==================== REPORT DTOs ====================

/// <summary>
/// Báo cáo học bổng
/// </summary>
public class ScholarshipReportDto
{
    public string Mssv { get; set; } = string.Empty;
    public string Hoten { get; set; } = string.Empty;
    public string Khoa { get; set; } = string.Empty;
    public string Mslop { get; set; } = string.Empty;
    public decimal Hocbong { get; set; }
}

/// <summary>
/// Báo cáo điểm trung bình theo môn
/// DISTRIBUTED AGGREGATION từ 3 sites (5, 6/7)
/// </summary>
public class AverageScoreReportDto
{
    public string Msmon { get; set; } = string.Empty;
    public string Khoa { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public decimal AvgDiem1 { get; set; }
    public decimal AvgDiem2 { get; set; }
    public decimal AvgDiem3 { get; set; }
    public decimal AvgTotal { get; set; }
}

/// <summary>
/// Báo cáo sinh viên rớt môn
/// DISTRIBUTED FILTERING + JOIN 4 sites
/// </summary>
public class FailureReportDto
{
    public string Mssv { get; set; } = string.Empty;
    public string Hoten { get; set; } = string.Empty;
    public string Khoa { get; set; } = string.Empty;
    public string Msmon { get; set; } = string.Empty;
    public decimal Diem1 { get; set; }
    public decimal Diem2 { get; set; }
    public decimal Diem3 { get; set; }
    public decimal DiemTB { get; set; }
}

/// <summary>
/// Phân bố điểm theo môn (histogram)
/// </summary>
public class ScoreDistributionReportDto
{
    public string Msmon { get; set; } = string.Empty;
    
    // K1 distribution
    public int K1_Count { get; set; }
    public decimal K1_Avg { get; set; }
    public int K1_0_4 { get; set; }      // < 4 (Kém)
    public int K1_4_5 { get; set; }      // 4-5 (Yếu)
    public int K1_5_6_5 { get; set; }    // 5-6.5 (Trung bình)
    public int K1_6_5_8 { get; set; }    // 6.5-8 (Khá)
    public int K1_8_10 { get; set; }     // 8-10 (Giỏi)
    
    // K2 distribution
    public int K2_Count { get; set; }
    public decimal K2_Avg { get; set; }
    public int K2_0_4 { get; set; }
    public int K2_4_5 { get; set; }
    public int K2_5_6_5 { get; set; }
    public int K2_6_5_8 { get; set; }
    public int K2_8_10 { get; set; }
}
