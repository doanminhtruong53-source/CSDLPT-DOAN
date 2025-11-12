using Microsoft.EntityFrameworkCore;
using DistributedDbApi.Data.DbContexts;
using DistributedDbApi.DTOs;
using DistributedDbApi.Models;

namespace DistributedDbApi.Services;

public class StudentService
{
    private readonly SinhVienK1DbContext _svK1Db;
    private readonly SinhVienK2DbContext _svK2Db;
    private readonly LopK1DbContext _lopK1Db;
    private readonly LopK2DbContext _lopK2Db;
    private readonly ILogger<StudentService> _logger;

    public StudentService(
        SinhVienK1DbContext svK1Db,
        SinhVienK2DbContext svK2Db,
        LopK1DbContext lopK1Db,
        LopK2DbContext lopK2Db,
        ILogger<StudentService> logger)
    {
        _svK1Db = svK1Db;
        _svK2Db = svK2Db;
        _lopK1Db = lopK1Db;
        _lopK2Db = lopK2Db;
        _logger = logger;
    }

    /// <summary>
    /// Auto-generate MSSV tiếp theo (toàn cục - check cả 2 khoa)
    /// Format: SVxxx (SV001, SV002, ..., SV100, ...)
    /// </summary>
    private async Task<string> GenerateNextMssvAsync(string khoa, CancellationToken ct = default)
    {
        int maxNumber = 0;

        // Tìm MSSV lớn nhất trong K1
        var studentsK1 = await _svK1Db.SinhVienK1
            .Select(s => s.Mssv)
            .ToListAsync(ct);

        foreach (var mssv in studentsK1)
        {
            if (mssv.StartsWith("SV") && int.TryParse(mssv.Substring(2), out int num))
            {
                if (num > maxNumber) maxNumber = num;
            }
        }

        // Tìm MSSV lớn nhất trong K2
        var studentsK2 = await _svK2Db.SinhVienK2
            .Select(s => s.Mssv)
            .ToListAsync(ct);

        foreach (var mssv in studentsK2)
        {
            if (mssv.StartsWith("SV") && int.TryParse(mssv.Substring(2), out int num))
            {
                if (num > maxNumber) maxNumber = num;
            }
        }

        // Tăng 1 và format
        int nextNumber = maxNumber + 1;
        return $"SV{nextNumber:D3}"; // D3 = 3 digits with leading zeros (001, 002, ...)
    }

    public async Task<StudentWithKhoaDto?> GetKhoaByMssvAsync(string mssv, CancellationToken ct = default)
    {
        try
        {
            // Fan-out: Tìm sinh viên ở cả 2 site song song
            var taskK1 = _svK1Db.SinhVienK1.FirstOrDefaultAsync(s => s.Mssv == mssv, ct);
            var taskK2 = _svK2Db.SinhVienK2.FirstOrDefaultAsync(s => s.Mssv == mssv, ct);

            await Task.WhenAll(taskK1, taskK2);

            var svK1 = taskK1.Result;
            var svK2 = taskK2.Result;

            if (svK1 != null)
            {
                _logger.LogInformation("Sinh viên {Mssv} tìm thấy ở site K1", mssv);
                return new StudentWithKhoaDto(mssv, "K1");
            }

            if (svK2 != null)
            {
                _logger.LogInformation("Sinh viên {Mssv} tìm thấy ở site K2", mssv);
                return new StudentWithKhoaDto(mssv, "K2");
            }

            _logger.LogWarning("Sinh viên {Mssv} không tồn tại", mssv);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tra cứu khoa cho sinh viên {Mssv}", mssv);
            throw;
        }
    }

    public async Task<StudentDto?> GetStudentByMssvAsync(string mssv, CancellationToken ct = default)
    {
        try
        {
            var taskK1 = _svK1Db.SinhVienK1.FirstOrDefaultAsync(s => s.Mssv == mssv, ct);
            var taskK2 = _svK2Db.SinhVienK2.FirstOrDefaultAsync(s => s.Mssv == mssv, ct);

            await Task.WhenAll(taskK1, taskK2);

            var student = taskK1.Result ?? taskK2.Result;
            if (student == null) return null;

            var khoa = taskK1.Result != null ? "K1" : "K2";

            return new StudentDto(
                student.Mssv,
                student.Hoten,
                student.Phai,
                student.Ngaysinh,
                student.Mslop,
                khoa,
                student.Hocbong);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin sinh viên {Mssv}", mssv);
            throw;
        }
    }

    public async Task<List<StudentDto>> SearchStudentsAsync(
        string? q = null,
        string? khoa = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var results = new List<StudentDto>();
            var skip = (page - 1) * pageSize;

            // Nếu chỉ định khoa, chỉ query 1 site
            if (khoa?.ToUpper() == "K1")
            {
                var query = _svK1Db.SinhVienK1.AsQueryable();
                if (!string.IsNullOrEmpty(q))
                {
                    query = query.Where(s => EF.Functions.ILike(s.Hoten, $"%{q}%") || s.Mssv.Contains(q));
                }

                var students = await query.Skip(skip).Take(pageSize).ToListAsync(ct);
                results.AddRange(students.Select(s => new StudentDto(s.Mssv, s.Hoten, s.Phai, s.Ngaysinh, s.Mslop, "K1", s.Hocbong)));
            }
            else if (khoa?.ToUpper() == "K2")
            {
                var query = _svK2Db.SinhVienK2.AsQueryable();
                if (!string.IsNullOrEmpty(q))
                {
                    query = query.Where(s => EF.Functions.ILike(s.Hoten, $"%{q}%") || s.Mssv.Contains(q));
                }

                var students = await query.Skip(skip).Take(pageSize).ToListAsync(ct);
                results.AddRange(students.Select(s => new StudentDto(s.Mssv, s.Hoten, s.Phai, s.Ngaysinh, s.Mslop, "K2", s.Hocbong)));
            }
            else
            {
                // Fan-out cả 2 site
                var queryK1 = _svK1Db.SinhVienK1.AsQueryable();
                var queryK2 = _svK2Db.SinhVienK2.AsQueryable();

                if (!string.IsNullOrEmpty(q))
                {
                    queryK1 = queryK1.Where(s => EF.Functions.ILike(s.Hoten, $"%{q}%") || s.Mssv.Contains(q));
                    queryK2 = queryK2.Where(s => EF.Functions.ILike(s.Hoten, $"%{q}%") || s.Mssv.Contains(q));
                }

                var taskK1 = queryK1.ToListAsync(ct);
                var taskK2 = queryK2.ToListAsync(ct);

                await Task.WhenAll(taskK1, taskK2);

                var allStudents = taskK1.Result.Select(s => new StudentDto(s.Mssv, s.Hoten, s.Phai, s.Ngaysinh, s.Mslop, "K1", s.Hocbong))
                    .Concat(taskK2.Result.Select(s => new StudentDto(s.Mssv, s.Hoten, s.Phai, s.Ngaysinh, s.Mslop, "K2", s.Hocbong)))
                    .Skip(skip)
                    .Take(pageSize)
                    .ToList();

                results.AddRange(allStudents);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tìm kiếm sinh viên");
            throw;
        }
    }

    // ==================== WRITE OPERATIONS ====================

    /// <summary>
    /// Xác định khoa từ mslop (lookup từ lop_k1 hoặc lop_k2)
    /// </summary>
    private async Task<string?> GetKhoaByMslopAsync(string mslop, CancellationToken ct = default)
    {
        var lopK1 = await _lopK1Db.LopK1.FirstOrDefaultAsync(l => l.Mslop == mslop, ct);
        if (lopK1 != null) return "K1";

        var lopK2 = await _lopK2Db.LopK2.FirstOrDefaultAsync(l => l.Mslop == mslop, ct);
        if (lopK2 != null) return "K2";

        return null;
    }

    /// <summary>
    /// POST /api/students - Thêm sinh viên mới
    /// DISTRIBUTED OPERATION: Smart routing dựa trên mslop
    /// AUTO-GENERATE MSSV
    /// </summary>
    public async Task<OperationResultDto> CreateStudentAsync(CreateStudentDto dto, CancellationToken ct = default)
    {
        var txInfo = new DistributedTransactionInfo();
        
        try
        {
            _logger.LogInformation("Bắt đầu tạo sinh viên mới");

            // Step 1: Xác định khoa từ mslop (lookup lop_k1/lop_k2)
            var khoa = await GetKhoaByMslopAsync(dto.Mslop, ct);
            if (khoa == null)
            {
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Lớp {dto.Mslop} không tồn tại"
                };
            }

            // Step 2: Auto-generate MSSV dựa trên khoa
            var mssv = await GenerateNextMssvAsync(khoa, ct);
            _logger.LogInformation("Auto-generated MSSV: {Mssv} cho khoa {Khoa}", mssv, khoa);

            // Step 3: INSERT vào đúng site dựa trên khoa
            // Fix DateTime to UTC for PostgreSQL
            var ngaysinhUtc = DateTime.SpecifyKind(dto.Ngaysinh, DateTimeKind.Utc);
            
            if (khoa == "K1")
            {
                var newStudent = new Student
                {
                    Mssv = mssv,
                    Hoten = dto.Hoten,
                    Phai = dto.Phai,
                    Ngaysinh = ngaysinhUtc,
                    Mslop = dto.Mslop,
                    Hocbong = dto.Hocbong
                };

                _svK1Db.SinhVienK1.Add(newStudent);
                await _svK1Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 3,
                    SiteName = "SinhVienK1DB",
                    Operation = "INSERT",
                    Success = true
                });

                _logger.LogInformation("Tạo sinh viên {Mssv} thành công tại Site 3 (K1)", mssv);
            }
            else // K2
            {
                var newStudent = new Student
                {
                    Mssv = mssv,
                    Hoten = dto.Hoten,
                    Phai = dto.Phai,
                    Ngaysinh = ngaysinhUtc,
                    Mslop = dto.Mslop,
                    Hocbong = dto.Hocbong
                };

                _svK2Db.SinhVienK2.Add(newStudent);
                await _svK2Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 4,
                    SiteName = "SinhVienK2DB",
                    Operation = "INSERT",
                    Success = true
                });

                _logger.LogInformation("Tạo sinh viên {Mssv} thành công tại Site 4 (K2)", mssv);
            }

            txInfo.Status = "Committed";
            txInfo.EndTime = DateTime.UtcNow;

            return new OperationResultDto
            {
                Success = true,
                Message = $"Tạo sinh viên {mssv} thành công tại khoa {khoa}",
                Data = new { mssv, khoa },
                TransactionInfo = txInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo sinh viên mới: {Error}", ex.Message);
            txInfo.Status = "RolledBack";
            txInfo.EndTime = DateTime.UtcNow;
            
            return new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi: {ex.Message}",
                TransactionInfo = txInfo
            };
        }
    }

    /// <summary>
    /// PATCH /api/students/{mssv} - Cập nhật sinh viên
    /// DISTRIBUTED SAGA: Nếu chuyển lớp khác khoa → DELETE từ site cũ + INSERT vào site mới
    /// </summary>
    public async Task<OperationResultDto> UpdateStudentAsync(string mssv, UpdateStudentDto dto, CancellationToken ct = default)
    {
        var txInfo = new DistributedTransactionInfo();
        
        try
        {
            _logger.LogInformation("Bắt đầu cập nhật sinh viên {Mssv}", mssv);

            // Step 1: Tìm sinh viên hiện tại
            var currentKhoa = await GetKhoaByMssvAsync(mssv, ct);
            if (currentKhoa == null)
            {
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Sinh viên {mssv} không tồn tại"
                };
            }

            // Step 2: Nếu chuyển lớp, kiểm tra khoa mới
            string? newKhoa = null;
            if (!string.IsNullOrEmpty(dto.Mslop))
            {
                newKhoa = await GetKhoaByMslopAsync(dto.Mslop, ct);
                if (newKhoa == null)
                {
                    return new OperationResultDto
                    {
                        Success = false,
                        Message = $"Lớp {dto.Mslop} không tồn tại"
                    };
                }
            }

            // Step 3: Xử lý CROSS-SITE nếu chuyển khoa
            if (newKhoa != null && newKhoa != currentKhoa.Khoa)
            {
                return await TransferStudentCrossSiteAsync(mssv, currentKhoa.Khoa, newKhoa, dto, txInfo, ct);
            }

            // Step 4: UPDATE trong cùng site (không đổi khoa)
            if (currentKhoa.Khoa == "K1")
            {
                var student = await _svK1Db.SinhVienK1.FirstOrDefaultAsync(s => s.Mssv == mssv, ct);
                if (student == null) return new OperationResultDto { Success = false, Message = "Không tìm thấy sinh viên" };

                if (!string.IsNullOrEmpty(dto.Hoten)) student.Hoten = dto.Hoten;
                if (!string.IsNullOrEmpty(dto.Phai)) student.Phai = dto.Phai;
                if (dto.Ngaysinh.HasValue) student.Ngaysinh = dto.Ngaysinh.Value;
                if (!string.IsNullOrEmpty(dto.Mslop)) student.Mslop = dto.Mslop;
                if (dto.Hocbong.HasValue) student.Hocbong = dto.Hocbong.Value;

                await _svK1Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 3,
                    SiteName = "SinhVienK1DB",
                    Operation = "UPDATE",
                    Success = true
                });
            }
            else // K2
            {
                var student = await _svK2Db.SinhVienK2.FirstOrDefaultAsync(s => s.Mssv == mssv, ct);
                if (student == null) return new OperationResultDto { Success = false, Message = "Không tìm thấy sinh viên" };

                if (!string.IsNullOrEmpty(dto.Hoten)) student.Hoten = dto.Hoten;
                if (!string.IsNullOrEmpty(dto.Phai)) student.Phai = dto.Phai;
                if (dto.Ngaysinh.HasValue) student.Ngaysinh = dto.Ngaysinh.Value;
                if (!string.IsNullOrEmpty(dto.Mslop)) student.Mslop = dto.Mslop;
                if (dto.Hocbong.HasValue) student.Hocbong = dto.Hocbong.Value;

                await _svK2Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 4,
                    SiteName = "SinhVienK2DB",
                    Operation = "UPDATE",
                    Success = true
                });
            }

            txInfo.Status = "Committed";
            txInfo.EndTime = DateTime.UtcNow;

            return new OperationResultDto
            {
                Success = true,
                Message = $"Cập nhật sinh viên {mssv} thành công",
                TransactionInfo = txInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật sinh viên {Mssv}", mssv);
            txInfo.Status = "RolledBack";
            
            return new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi: {ex.Message}",
                TransactionInfo = txInfo
            };
        }
    }

    /// <summary>
    /// SAGA Pattern: Chuyển sinh viên từ site này sang site khác (cross-khoa)
    /// Step 1: Lấy full data từ site cũ
    /// Step 2: INSERT vào site mới
    /// Step 3: DELETE từ site cũ
    /// Rollback: Nếu step 2 fail → không xóa site cũ
    /// </summary>
    private async Task<OperationResultDto> TransferStudentCrossSiteAsync(
        string mssv, string oldKhoa, string newKhoa, UpdateStudentDto dto, 
        DistributedTransactionInfo txInfo, CancellationToken ct)
    {
        _logger.LogWarning("CROSS-SITE TRANSFER: {Mssv} từ {OldKhoa} sang {NewKhoa}", mssv, oldKhoa, newKhoa);

        try
        {
            // Step 1: Lấy data từ site cũ
            Student? oldStudent = null;

            if (oldKhoa == "K1")
            {
                oldStudent = await _svK1Db.SinhVienK1.FirstOrDefaultAsync(s => s.Mssv == mssv, ct);
            }
            else
            {
                oldStudent = await _svK2Db.SinhVienK2.FirstOrDefaultAsync(s => s.Mssv == mssv, ct);
            }

            if (oldStudent == null)
                throw new Exception($"Không tìm thấy sinh viên tại site cũ");

            // Step 2: INSERT vào site mới với data cập nhật
            var newStudent = new Student
            {
                Mssv = mssv,
                Hoten = dto.Hoten ?? oldStudent.Hoten,
                Phai = dto.Phai ?? oldStudent.Phai,
                Ngaysinh = dto.Ngaysinh ?? oldStudent.Ngaysinh,
                Mslop = dto.Mslop!, // Mới có lớp mới
                Hocbong = dto.Hocbong ?? oldStudent.Hocbong
            };

            if (newKhoa == "K1")
            {
                _svK1Db.SinhVienK1.Add(newStudent);
                await _svK1Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 3,
                    SiteName = "SinhVienK1DB",
                    Operation = "INSERT (transfer in)",
                    Success = true
                });
            }
            else // K2
            {
                _svK2Db.SinhVienK2.Add(newStudent);
                await _svK2Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 4,
                    SiteName = "SinhVienK2DB",
                    Operation = "INSERT (transfer in)",
                    Success = true
                });
            }

            // Step 3: DELETE từ site cũ (chỉ khi INSERT thành công)
            if (oldKhoa == "K1")
            {
                _svK1Db.SinhVienK1.Remove(oldStudent);
                await _svK1Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 3,
                    SiteName = "SinhVienK1DB",
                    Operation = "DELETE (transfer out)",
                    Success = true
                });
            }
            else
            {
                _svK2Db.SinhVienK2.Remove(oldStudent);
                await _svK2Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 4,
                    SiteName = "SinhVienK2DB",
                    Operation = "DELETE (transfer out)",
                    Success = true
                });
            }

            txInfo.Status = "Committed";
            txInfo.EndTime = DateTime.UtcNow;

            return new OperationResultDto
            {
                Success = true,
                Message = $"Chuyển sinh viên {mssv} từ {oldKhoa} sang {newKhoa} thành công (SAGA)",
                Data = new { oldKhoa, newKhoa },
                TransactionInfo = txInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAGA FAILED: Lỗi chuyển sinh viên {Mssv}", mssv);
            txInfo.Status = "RolledBack";
            txInfo.EndTime = DateTime.UtcNow;

            return new OperationResultDto
            {
                Success = false,
                Message = $"SAGA failed: {ex.Message}. Data vẫn ở site cũ.",
                TransactionInfo = txInfo
            };
        }
    }

    /// <summary>
    /// DELETE /api/students/{mssv} - Xóa sinh viên
    /// NOTE: Trong production nên xóa registrations trước (cascade)
    /// </summary>
    public async Task<OperationResultDto> DeleteStudentAsync(string mssv, CancellationToken ct = default)
    {
        var txInfo = new DistributedTransactionInfo();
        
        try
        {
            _logger.LogInformation("Bắt đầu xóa sinh viên {Mssv}", mssv);

            // Step 1: Tìm sinh viên
            var khoa = await GetKhoaByMssvAsync(mssv, ct);
            if (khoa == null)
            {
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Sinh viên {mssv} không tồn tại"
                };
            }

            // Step 2: DELETE từ site tương ứng
            if (khoa.Khoa == "K1")
            {
                var student = await _svK1Db.SinhVienK1.FirstOrDefaultAsync(s => s.Mssv == mssv, ct);
                if (student != null)
                {
                    _svK1Db.SinhVienK1.Remove(student);
                    await _svK1Db.SaveChangesAsync(ct);

                    txInfo.SiteOperations.Add(new SiteOperationInfo
                    {
                        SiteId = 3,
                        SiteName = "SinhVienK1DB",
                        Operation = "DELETE",
                        Success = true
                    });
                }
            }
            else
            {
                var student = await _svK2Db.SinhVienK2.FirstOrDefaultAsync(s => s.Mssv == mssv, ct);
                if (student != null)
                {
                    _svK2Db.SinhVienK2.Remove(student);
                    await _svK2Db.SaveChangesAsync(ct);

                    txInfo.SiteOperations.Add(new SiteOperationInfo
                    {
                        SiteId = 4,
                        SiteName = "SinhVienK2DB",
                        Operation = "DELETE",
                        Success = true
                    });
                }
            }

            txInfo.Status = "Committed";
            txInfo.EndTime = DateTime.UtcNow;

            return new OperationResultDto
            {
                Success = true,
                Message = $"Xóa sinh viên {mssv} thành công",
                Warnings = new List<string> { "Lưu ý: Cần xóa registrations của sinh viên này ở các sites 5/6/7" },
                TransactionInfo = txInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa sinh viên {Mssv}", mssv);
            txInfo.Status = "RolledBack";
            
            return new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi: {ex.Message}",
                TransactionInfo = txInfo
            };
        }
    }
}
