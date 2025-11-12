using Microsoft.EntityFrameworkCore;
using DistributedDbApi.Data.DbContexts;
using DistributedDbApi.DTOs;

namespace DistributedDbApi.Services;

public class ClassService
{
    private readonly LopK1DbContext _lopK1Db;
    private readonly LopK2DbContext _lopK2Db;
    private readonly SinhVienK1DbContext _svK1Db;
    private readonly SinhVienK2DbContext _svK2Db;
    private readonly ILogger<ClassService> _logger;

    public ClassService(
        LopK1DbContext lopK1Db,
        LopK2DbContext lopK2Db,
        SinhVienK1DbContext svK1Db,
        SinhVienK2DbContext svK2Db,
        ILogger<ClassService> logger)
    {
        _lopK1Db = lopK1Db;
        _lopK2Db = lopK2Db;
        _svK1Db = svK1Db;
        _svK2Db = svK2Db;
        _logger = logger;
    }

    public async Task<List<DepartmentSummaryDto>> GetDepartmentsAsync(CancellationToken ct = default)
    {
        try
        {
            var taskK1Classes = _lopK1Db.LopK1.CountAsync(ct);
            var taskK2Classes = _lopK2Db.LopK2.CountAsync(ct);
            var taskK1Students = _svK1Db.SinhVienK1.CountAsync(ct);
            var taskK2Students = _svK2Db.SinhVienK2.CountAsync(ct);

            await Task.WhenAll(taskK1Classes, taskK2Classes, taskK1Students, taskK2Students);

            return new List<DepartmentSummaryDto>
            {
                new("K1", taskK1Classes.Result, taskK1Students.Result),
                new("K2", taskK2Classes.Result, taskK2Students.Result)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thống kê khoa");
            throw;
        }
    }

    public async Task<List<ClassDto>> GetClassesAsync(
        string? khoa = null,
        string? q = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var results = new List<ClassDto>();
            var skip = (page - 1) * pageSize;

            if (khoa?.ToUpper() == "K1")
            {
                var query = _lopK1Db.LopK1.AsQueryable();
                if (!string.IsNullOrEmpty(q))
                {
                    query = query.Where(c => EF.Functions.ILike(c.Tenlop, $"%{q}%") || c.Mslop.Contains(q));
                }

                var classes = await query.Skip(skip).Take(pageSize).ToListAsync(ct);
                results.AddRange(classes.Select(c => new ClassDto(c.Mslop, c.Tenlop, c.Khoa)));
            }
            else if (khoa?.ToUpper() == "K2")
            {
                var query = _lopK2Db.LopK2.AsQueryable();
                if (!string.IsNullOrEmpty(q))
                {
                    query = query.Where(c => EF.Functions.ILike(c.Tenlop, $"%{q}%") || c.Mslop.Contains(q));
                }

                var classes = await query.Skip(skip).Take(pageSize).ToListAsync(ct);
                results.AddRange(classes.Select(c => new ClassDto(c.Mslop, c.Tenlop, c.Khoa)));
            }
            else
            {
                // Fan-out cả 2 site
                var queryK1 = _lopK1Db.LopK1.AsQueryable();
                var queryK2 = _lopK2Db.LopK2.AsQueryable();

                if (!string.IsNullOrEmpty(q))
                {
                    queryK1 = queryK1.Where(c => EF.Functions.ILike(c.Tenlop, $"%{q}%") || c.Mslop.Contains(q));
                    queryK2 = queryK2.Where(c => EF.Functions.ILike(c.Tenlop, $"%{q}%") || c.Mslop.Contains(q));
                }

                var taskK1 = queryK1.ToListAsync(ct);
                var taskK2 = queryK2.ToListAsync(ct);

                await Task.WhenAll(taskK1, taskK2);

                var allClasses = taskK1.Result.Select(c => new ClassDto(c.Mslop, c.Tenlop, c.Khoa))
                    .Concat(taskK2.Result.Select(c => new ClassDto(c.Mslop, c.Tenlop, c.Khoa)))
                    .Skip(skip)
                    .Take(pageSize)
                    .ToList();

                results.AddRange(allClasses);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách lớp");
            throw;
        }
    }

    public async Task<ClassDto?> GetClassByMslopAsync(string mslop, CancellationToken ct = default)
    {
        try
        {
            var taskK1 = _lopK1Db.LopK1.FirstOrDefaultAsync(c => c.Mslop == mslop, ct);
            var taskK2 = _lopK2Db.LopK2.FirstOrDefaultAsync(c => c.Mslop == mslop, ct);

            await Task.WhenAll(taskK1, taskK2);

            var classEntity = taskK1.Result ?? taskK2.Result;
            return classEntity != null 
                ? new ClassDto(classEntity.Mslop, classEntity.Tenlop, classEntity.Khoa) 
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin lớp {Mslop}", mslop);
            throw;
        }
    }

    public async Task<List<StudentDto>> GetStudentsByClassAsync(
        string mslop,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var skip = (page - 1) * pageSize;

            // Tìm lớp trước để biết khoa
            var classDto = await GetClassByMslopAsync(mslop, ct);
            if (classDto == null) return new List<StudentDto>();

            var khoa = classDto.Khoa;

            if (khoa == "K1")
            {
                var students = await _svK1Db.SinhVienK1
                    .Where(s => s.Mslop == mslop)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return students.Select(s => new StudentDto(s.Mssv, s.Hoten, s.Phai, s.Ngaysinh, s.Mslop, "K1", s.Hocbong)).ToList();
            }
            else
            {
                var students = await _svK2Db.SinhVienK2
                    .Where(s => s.Mslop == mslop)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return students.Select(s => new StudentDto(s.Mssv, s.Hoten, s.Phai, s.Ngaysinh, s.Mslop, "K2", s.Hocbong)).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy sinh viên lớp {Mslop}", mslop);
            throw;
        }
    }

    /// <summary>
    /// Auto-generate mã lớp tiếp theo GLOBALLY (query cả K1 và K2)
    /// Format: Lxx (L01, L02, ..., L99)
    /// Đảm bảo không bị trùng mã giữa các khoa
    /// </summary>
    private async Task<string> GenerateNextMslopAsync(string khoa, CancellationToken ct = default)
    {
        int maxNumber = 0;

        // Query CẢ 2 SITES để tìm max toàn cục (tránh duplicate key)
        var classesK1Task = _lopK1Db.LopK1.Select(l => l.Mslop).ToListAsync(ct);
        var classesK2Task = _lopK2Db.LopK2.Select(l => l.Mslop).ToListAsync(ct);

        await Task.WhenAll(classesK1Task, classesK2Task);

        var allClasses = classesK1Task.Result.Concat(classesK2Task.Result);

        foreach (var mslop in allClasses)
        {
            if (mslop.StartsWith("L") && int.TryParse(mslop.Substring(1), out int num))
            {
                if (num > maxNumber) maxNumber = num;
            }
        }

        int nextNumber = maxNumber + 1;
        
        _logger.LogInformation("🔢 Generate Mslop: Max={Max}, Next={Next} (Khoa={Khoa})", 
            maxNumber, nextNumber, khoa);
        
        return $"L{nextNumber:D2}"; // L01, L02, ...
    }

    /// <summary>
    /// Tạo lớp mới - DISTRIBUTED WRITE với AUTO-GENERATE Mslop
    /// Ghi vào Site 1 (K1) hoặc Site 2 (K2) tuỳ khoa
    /// </summary>
    public async Task<OperationResultDto> CreateClassAsync(CreateClassDto dto, CancellationToken ct = default)
    {
        var txInfo = new DistributedTransactionInfo
        {
            TransactionId = Guid.NewGuid().ToString(),
            StartTime = DateTime.UtcNow
        };

        try
        {
            var khoa = dto.Khoa.ToUpper();
            var siteId = khoa == "K1" ? 1 : 2;
            var siteName = khoa == "K1" ? "LopK1DB" : "LopK2DB";

            // Auto-generate mã lớp
            var mslop = await GenerateNextMslopAsync(khoa, ct);
            
            _logger.LogInformation("🔄 [TX:{TxId}] Tạo lớp {Mslop} tại Site {SiteId} ({SiteName})", 
                txInfo.TransactionId, mslop, siteId, siteName);

            // SAGA Step 1: Insert to appropriate site
            if (khoa == "K1")
            {
                var entity = new Models.Class
                {
                    Mslop = mslop,
                    Tenlop = dto.Tenlop,
                    Khoa = dto.Khoa
                };
                _lopK1Db.LopK1.Add(entity);
                await _lopK1Db.SaveChangesAsync(ct);
            }
            else
            {
                var entity = new Models.Class
                {
                    Mslop = mslop,
                    Tenlop = dto.Tenlop,
                    Khoa = dto.Khoa
                };
                _lopK2Db.LopK2.Add(entity);
                await _lopK2Db.SaveChangesAsync(ct);
            }

            txInfo.SiteOperations.Add(new SiteOperationInfo
            {
                SiteId = siteId,
                SiteName = siteName,
                Operation = "INSERT",
                Success = true,
                ExecutedAt = DateTime.UtcNow
            });

            txInfo.EndTime = DateTime.UtcNow;
            txInfo.Status = "Committed";

            _logger.LogInformation("✅ [TX:{TxId}] Tạo lớp thành công - Site {SiteId}", 
                txInfo.TransactionId, siteId);

            return new OperationResultDto
            {
                Success = true,
                Message = $"Tạo lớp {mslop} thành công tại {siteName}!",
                Data = new ClassDto(mslop, dto.Tenlop, dto.Khoa),
                TransactionInfo = txInfo
            };
        }
        catch (Exception ex)
        {
            txInfo.Status = "RolledBack";
            txInfo.EndTime = DateTime.UtcNow;
            _logger.LogError(ex, "❌ [TX:{TxId}] Lỗi khi tạo lớp", txInfo.TransactionId);
            
            return new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi khi tạo lớp: {ex.Message}",
                TransactionInfo = txInfo
            };
        }
    }

    /// <summary>
    /// Cập nhật lớp - DISTRIBUTED UPDATE
    /// Update Site 1 (K1) hoặc Site 2 (K2)
    /// </summary>
    public async Task<OperationResultDto> UpdateClassAsync(string mslop, UpdateClassDto dto, CancellationToken ct = default)
    {
        var txInfo = new DistributedTransactionInfo
        {
            TransactionId = Guid.NewGuid().ToString(),
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Find existing class
            var taskK1 = _lopK1Db.LopK1.FirstOrDefaultAsync(c => c.Mslop == mslop, ct);
            var taskK2 = _lopK2Db.LopK2.FirstOrDefaultAsync(c => c.Mslop == mslop, ct);
            await Task.WhenAll(taskK1, taskK2);

            var lopK1 = taskK1.Result;
            var lopK2 = taskK2.Result;

            if (lopK1 == null && lopK2 == null)
            {
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Không tìm thấy lớp {mslop}!",
                    TransactionInfo = txInfo
                };
            }

            // SAGA Step 1: Update appropriate site
            if (lopK1 != null)
            {
                _logger.LogInformation("🔄 [TX:{TxId}] Cập nhật lớp {Mslop} tại Site 1 (LopK1DB)", 
                    txInfo.TransactionId, mslop);

                lopK1.Tenlop = dto.Tenlop;
                await _lopK1Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 1,
                    SiteName = "LopK1DB",
                    Operation = "UPDATE",
                    Success = true,
                    ExecutedAt = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogInformation("🔄 [TX:{TxId}] Cập nhật lớp {Mslop} tại Site 2 (LopK2DB)", 
                    txInfo.TransactionId, mslop);

                lopK2!.Tenlop = dto.Tenlop;
                await _lopK2Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 2,
                    SiteName = "LopK2DB",
                    Operation = "UPDATE",
                    Success = true,
                    ExecutedAt = DateTime.UtcNow
                });
            }

            txInfo.EndTime = DateTime.UtcNow;
            txInfo.Status = "Committed";

            _logger.LogInformation("✅ [TX:{TxId}] Cập nhật lớp thành công", txInfo.TransactionId);

            return new OperationResultDto
            {
                Success = true,
                Message = $"Cập nhật lớp {mslop} thành công!",
                TransactionInfo = txInfo
            };
        }
        catch (Exception ex)
        {
            txInfo.Status = "RolledBack";
            txInfo.EndTime = DateTime.UtcNow;
            _logger.LogError(ex, "❌ [TX:{TxId}] Lỗi khi cập nhật lớp", txInfo.TransactionId);
            
            return new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi khi cập nhật lớp: {ex.Message}",
                TransactionInfo = txInfo
            };
        }
    }

    /// <summary>
    /// Xoá lớp - DISTRIBUTED DELETE với SAGA pattern
    /// Bước 1: Kiểm tra sinh viên (Site 3/4)
    /// Bước 2: Xoá lớp (Site 1/2)
    /// </summary>
    public async Task<OperationResultDto> DeleteClassAsync(string mslop, CancellationToken ct = default)
    {
        var txInfo = new DistributedTransactionInfo
        {
            TransactionId = Guid.NewGuid().ToString(),
            StartTime = DateTime.UtcNow
        };

        try
        {
            // SAGA Step 1: Check if class has students
            var students = await GetStudentsByClassAsync(mslop, 1, 1, ct);
            if (students.Any())
            {
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Không thể xoá lớp {mslop} vì còn {students.Count} sinh viên! Hãy chuyển hoặc xoá sinh viên trước.",
                    TransactionInfo = txInfo
                };
            }

            // Find class to delete
            var taskK1 = _lopK1Db.LopK1.FirstOrDefaultAsync(c => c.Mslop == mslop, ct);
            var taskK2 = _lopK2Db.LopK2.FirstOrDefaultAsync(c => c.Mslop == mslop, ct);
            await Task.WhenAll(taskK1, taskK2);

            var lopK1 = taskK1.Result;
            var lopK2 = taskK2.Result;

            if (lopK1 == null && lopK2 == null)
            {
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Không tìm thấy lớp {mslop}!",
                    TransactionInfo = txInfo
                };
            }

            // SAGA Step 2: Delete from appropriate site
            if (lopK1 != null)
            {
                _logger.LogInformation("🔄 [TX:{TxId}] Xoá lớp {Mslop} từ Site 1 (LopK1DB)", 
                    txInfo.TransactionId, mslop);

                _lopK1Db.LopK1.Remove(lopK1);
                await _lopK1Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 1,
                    SiteName = "LopK1DB",
                    Operation = "DELETE",
                    Success = true,
                    ExecutedAt = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogInformation("🔄 [TX:{TxId}] Xoá lớp {Mslop} từ Site 2 (LopK2DB)", 
                    txInfo.TransactionId, mslop);

                _lopK2Db.LopK2.Remove(lopK2!);
                await _lopK2Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 2,
                    SiteName = "LopK2DB",
                    Operation = "DELETE",
                    Success = true,
                    ExecutedAt = DateTime.UtcNow
                });
            }

            txInfo.EndTime = DateTime.UtcNow;
            txInfo.Status = "Committed";

            _logger.LogInformation("✅ [TX:{TxId}] Xoá lớp thành công", txInfo.TransactionId);

            return new OperationResultDto
            {
                Success = true,
                Message = $"Xoá lớp {mslop} thành công!",
                TransactionInfo = txInfo
            };
        }
        catch (Exception ex)
        {
            txInfo.Status = "RolledBack";
            txInfo.EndTime = DateTime.UtcNow;
            _logger.LogError(ex, "❌ [TX:{TxId}] Lỗi khi xoá lớp", txInfo.TransactionId);
            
            return new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi khi xoá lớp: {ex.Message}",
                TransactionInfo = txInfo
            };
        }
    }
}
