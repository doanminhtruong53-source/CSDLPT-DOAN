using System.ComponentModel.DataAnnotations;

namespace DistributedDbApi.DTOs;

/// <summary>
/// DTO cho tạo mới sinh viên - POST /api/students
/// MSSV sẽ được tự động generate bởi server
/// </summary>
public class CreateStudentDto
{
    // MSSV sẽ được auto-generate, không cần gửi từ client
    
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2-100 ký tự")]
    public string Hoten { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phái là bắt buộc")]
    [RegularExpression(@"^(Nam|Nữ|Nu)$", ErrorMessage = "Phái phải là 'Nam' hoặc 'Nữ'")]
    public string Phai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
    public DateTime Ngaysinh { get; set; }

    [Required(ErrorMessage = "Mã lớp là bắt buộc")]
    [RegularExpression(@"^L\d+$", ErrorMessage = "Mã lớp phải có định dạng Lxx (ví dụ: L1)")]
    public string Mslop { get; set; } = string.Empty;

    [Range(0, 10000000, ErrorMessage = "Học bổng phải từ 0 đến 10,000,000")]
    public decimal Hocbong { get; set; } = 0;
}

/// <summary>
/// DTO cho cập nhật sinh viên - PATCH /api/students/{mssv}
/// </summary>
public class UpdateStudentDto
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2-100 ký tự")]
    public string? Hoten { get; set; }

    [RegularExpression(@"^(Nam|Nữ|Nu)$", ErrorMessage = "Phái phải là 'Nam' hoặc 'Nữ'")]
    public string? Phai { get; set; }

    public DateTime? Ngaysinh { get; set; }

    /// <summary>
    /// Chuyển lớp - CÓ THỂ CROSS-SITE nếu lớp mới thuộc khoa khác
    /// </summary>
    [RegularExpression(@"^L\d+$", ErrorMessage = "Mã lớp phải có định dạng Lxx (ví dụ: L1)")]
    public string? Mslop { get; set; }

    [Range(0, 10000000, ErrorMessage = "Học bổng phải từ 0 đến 10,000,000")]
    public decimal? Hocbong { get; set; }
}

/// <summary>
/// DTO cho đăng ký môn học - POST /api/registrations
/// DISTRIBUTED WRITE: Ghi vào 2 sites (Site 5 + Site 6/7)
/// </summary>
public class CreateRegistrationDto
{
    [Required(ErrorMessage = "MSSV là bắt buộc")]
    [RegularExpression(@"^SV\d{3,}$", ErrorMessage = "MSSV không hợp lệ")]
    public string Mssv { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã môn là bắt buộc")]
    [RegularExpression(@"^M\d{2,}$", ErrorMessage = "Mã môn phải có định dạng Mxx (ví dụ: M01)")]
    public string Msmon { get; set; } = string.Empty;

    /// <summary>
    /// Điểm lần 1 (optional, có thể nhập sau)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Điểm phải từ 0 đến 10")]
    public decimal? Diem1 { get; set; }

    /// <summary>
    /// Điểm lần 2 (optional, có thể nhập sau)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Điểm 2 phải từ 0 đến 10")]
    public decimal? Diem2 { get; set; }

    /// <summary>
    /// Điểm lần 3 (optional, có thể nhập sau)
    /// </summary>
    [Range(0, 10, ErrorMessage = "Điểm 3 phải từ 0 đến 10")]
    public decimal? Diem3 { get; set; }
}

/// <summary>
/// DTO cho cập nhật điểm - PATCH /api/registrations/{mssv}/{msmon}
/// DISTRIBUTED UPDATE: Update Site 5 và/hoặc Site 6/7
/// </summary>
public class UpdateScoreDto
{
    [Range(0, 10, ErrorMessage = "Điểm 1 phải từ 0 đến 10")]
    public decimal? Diem1 { get; set; }

    [Range(0, 10, ErrorMessage = "Điểm 2 phải từ 0 đến 10")]
    public decimal? Diem2 { get; set; }

    [Range(0, 10, ErrorMessage = "Điểm 3 phải từ 0 đến 10")]
    public decimal? Diem3 { get; set; }
}

/// <summary>
/// DTO cho tạo lớp học mới - POST /api/classes
/// DISTRIBUTED: Ghi vào Site 1 (K1) hoặc Site 2 (K2)
/// Mslop sẽ được auto-generate bởi server
/// </summary>
public class CreateClassDto
{
    // Mslop sẽ được auto-generate, không cần gửi từ client

    [Required(ErrorMessage = "Tên lớp là bắt buộc")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Tên lớp phải từ 5-200 ký tự")]
    public string Tenlop { get; set; } = string.Empty;

    [Required(ErrorMessage = "Khoa là bắt buộc")]
    [RegularExpression(@"^K[12]$", ErrorMessage = "Khoa phải là K1 hoặc K2")]
    public string Khoa { get; set; } = string.Empty;
}

/// <summary>
/// DTO cho cập nhật lớp học - PUT /api/classes/{mslop}
/// DISTRIBUTED: Update Site 1 (K1) hoặc Site 2 (K2)
/// </summary>
public class UpdateClassDto
{
    [Required(ErrorMessage = "Tên lớp là bắt buộc")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Tên lớp phải từ 5-200 ký tự")]
    public string Tenlop { get; set; } = string.Empty;

    // Không cho phép đổi khoa vì sẽ phức tạp (phải move sang site khác + update tất cả sinh viên)
    // Nếu muốn đổi khoa, phải xóa và tạo mới
}

/// <summary>
/// Response cho operation thành công
/// </summary>
public class OperationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public List<string>? Warnings { get; set; }
    public DistributedTransactionInfo? TransactionInfo { get; set; }
}

/// <summary>
/// Thông tin về distributed transaction (cho monitoring/debugging)
/// </summary>
public class DistributedTransactionInfo
{
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public List<SiteOperationInfo> SiteOperations { get; set; } = new();
    public string Status { get; set; } = "Pending"; // Pending, Committed, RolledBack, PartialSuccess
}

/// <summary>
/// Thông tin về operation tại từng site
/// </summary>
public class SiteOperationInfo
{
    public int SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty; // INSERT, UPDATE, DELETE
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
