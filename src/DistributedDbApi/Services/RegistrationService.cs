using Microsoft.EntityFrameworkCore;
using DistributedDbApi.Data.DbContexts;
using DistributedDbApi.DTOs;
using DistributedDbApi.Models;

namespace DistributedDbApi.Services;

public class RegistrationService
{
    private readonly DangKyDiem1DbContext _diem1Db;
    private readonly DangKyDiem23K1DbContext _diem23K1Db;
    private readonly DangKyDiem23K2DbContext _diem23K2Db;
    private readonly SinhVienK1DbContext _svK1Db;
    private readonly SinhVienK2DbContext _svK2Db;
    private readonly StudentService _studentService;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        DangKyDiem1DbContext diem1Db,
        DangKyDiem23K1DbContext diem23K1Db,
        DangKyDiem23K2DbContext diem23K2Db,
        SinhVienK1DbContext svK1Db,
        SinhVienK2DbContext svK2Db,
        StudentService studentService,
        ILogger<RegistrationService> logger)
    {
        _diem1Db = diem1Db;
        _diem23K1Db = diem23K1Db;
        _diem23K2Db = diem23K2Db;
        _svK1Db = svK1Db;
        _svK2Db = svK2Db;
        _studentService = studentService;
        _logger = logger;
    }

    public async Task<List<RegistrationScoreDto>> GetAllRegistrationsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        try
        {
            var skip = (page - 1) * pageSize;

            // Lấy tất cả đăng ký từ site 5 (điểm 1)
            var diem1List = await _diem1Db.DangKyDiem1
                .OrderBy(d => d.Mssv)
                .ThenBy(d => d.Msmon)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(ct);

            if (!diem1List.Any())
            {
                return new List<RegistrationScoreDto>();
            }

            // Lấy danh sách MSSV để query điểm 2,3 và thông tin sinh viên
            var mssvList = diem1List.Select(d => d.Mssv).Distinct().ToList();

            // Query thông tin sinh viên từ cả 2 site (K1 và K2)
            var studentsK1 = await _svK1Db.SinhVienK1
                .Where(s => mssvList.Contains(s.Mssv))
                .Select(s => new { s.Mssv, s.Hoten, Khoa = "K1" })
                .ToListAsync(ct);

            var studentsK2 = await _svK2Db.SinhVienK2
                .Where(s => mssvList.Contains(s.Mssv))
                .Select(s => new { s.Mssv, s.Hoten, Khoa = "K2" })
                .ToListAsync(ct);

            var allStudents = studentsK1.Concat(studentsK2).ToList();

            // Query điểm 2,3 từ cả 2 site (K1 và K2)
            var diem23K1List = await _diem23K1Db.DangKyDiem23K1
                .Where(d => mssvList.Contains(d.Mssv))
                .ToListAsync(ct);

            var diem23K2List = await _diem23K2Db.DangKyDiem23K2
                .Where(d => mssvList.Contains(d.Mssv))
                .ToListAsync(ct);

            // Merge điểm 2,3 từ cả 2 site
            var allDiem23 = diem23K1List
                .Select(d => new { d.Mssv, d.Msmon, d.Diem2, d.Diem3 })
                .Concat(diem23K2List.Select(d => new { d.Mssv, d.Msmon, d.Diem2, d.Diem3 }))
                .ToList();

            // JOIN điểm 1 với điểm 2,3 và thông tin sinh viên
            var results = from d1 in diem1List
                          join d23 in allDiem23 on new { d1.Mssv, d1.Msmon } equals new { d23.Mssv, d23.Msmon } into joined
                          from d23 in joined.DefaultIfEmpty()
                          join student in allStudents on d1.Mssv equals student.Mssv into studentJoined
                          from student in studentJoined.DefaultIfEmpty()
                          select new RegistrationScoreDto(
                              d1.Mssv,
                              d1.Msmon,
                              d1.Diem1,
                              d23?.Diem2,
                              d23?.Diem3,
                              student?.Hoten,
                              student?.Khoa);

            _logger.LogInformation("Lấy {Count} đăng ký (page {Page})", results.Count(), page);
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách đăng ký");
            throw;
        }
    }

    public async Task<List<RegistrationScoreDto>> GetScoresByMssvAsync(string mssv, CancellationToken ct = default)
    {
        try
        {
            // Bước 1: Xác định khoa của sinh viên
            var khoaInfo = await _studentService.GetKhoaByMssvAsync(mssv, ct);
            if (khoaInfo == null)
            {
                _logger.LogWarning("Sinh viên {Mssv} không tồn tại", mssv);
                return new List<RegistrationScoreDto>();
            }

            // Bước 2: Lấy điểm lần 1 từ site 5
            var diem1List = await _diem1Db.DangKyDiem1
                .Where(d => d.Mssv == mssv)
                .ToListAsync(ct);

            // Bước 3: Lấy điểm lần 2,3 từ site phù hợp (6 hoặc 7)
            List<RegistrationScoreDto> diem23List;
            if (khoaInfo.Khoa == "K1")
            {
                var diem23 = await _diem23K1Db.DangKyDiem23K1
                    .Where(d => d.Mssv == mssv)
                    .ToListAsync(ct);

                diem23List = diem23.Select(d => new RegistrationScoreDto(d.Mssv, d.Msmon, null, d.Diem2, d.Diem3)).ToList();
            }
            else
            {
                var diem23 = await _diem23K2Db.DangKyDiem23K2
                    .Where(d => d.Mssv == mssv)
                    .ToListAsync(ct);

                diem23List = diem23.Select(d => new RegistrationScoreDto(d.Mssv, d.Msmon, null, d.Diem2, d.Diem3)).ToList();
            }

            // Bước 4: JOIN tại API Gateway (fan-in)
            var results = from d1 in diem1List
                          join d23 in diem23List on d1.Msmon equals d23.Msmon into joined
                          from d23 in joined.DefaultIfEmpty()
                          select new RegistrationScoreDto(
                              d1.Mssv,
                              d1.Msmon,
                              d1.Diem1,
                              d23?.Diem2,
                              d23?.Diem3);

            _logger.LogInformation("Lấy điểm thành công cho sinh viên {Mssv}: {Count} môn", mssv, results.Count());
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy điểm sinh viên {Mssv}", mssv);
            throw;
        }
    }

    public async Task<List<RegistrationScoreDto>> GetStudentsBySubjectAsync(
        string msmon,
        bool includeScores = true,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var skip = (page - 1) * pageSize;

            // Lấy danh sách sinh viên học môn từ site 5
            var registrations = await _diem1Db.DangKyDiem1
                .Where(d => d.Msmon == msmon)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(ct);

            if (!includeScores)
            {
                return registrations.Select(r => new RegistrationScoreDto(r.Mssv, r.Msmon, r.Diem1, null, null)).ToList();
            }

            // Nếu cần điểm đầy đủ, fan-out lấy điểm 2/3
            var results = new List<RegistrationScoreDto>();

            foreach (var reg in registrations)
            {
                var khoaInfo = await _studentService.GetKhoaByMssvAsync(reg.Mssv, ct);
                if (khoaInfo == null) continue;

                if (khoaInfo.Khoa == "K1")
                {
                    var diem23 = await _diem23K1Db.DangKyDiem23K1
                        .FirstOrDefaultAsync(d => d.Mssv == reg.Mssv && d.Msmon == msmon, ct);

                    results.Add(new RegistrationScoreDto(reg.Mssv, reg.Msmon, reg.Diem1, diem23?.Diem2, diem23?.Diem3));
                }
                else
                {
                    var diem23 = await _diem23K2Db.DangKyDiem23K2
                        .FirstOrDefaultAsync(d => d.Mssv == reg.Mssv && d.Msmon == msmon, ct);

                    results.Add(new RegistrationScoreDto(reg.Mssv, reg.Msmon, reg.Diem1, diem23?.Diem2, diem23?.Diem3));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy sinh viên môn {Msmon}", msmon);
            throw;
        }
    }

    /// <summary>
    /// Tạo đăng ký mới sử dụng SAGA Pattern - Distributed Transaction
    /// SAGA Flow:
    /// 1. INSERT to Site 5 (dangky_diem1) - FIRST OPERATION
    /// 2. INSERT to Site 6/7 (dangky_diem23_k1/k2) - SECOND OPERATION
    /// 3. If Step 2 fails → COMPENSATING TRANSACTION: DELETE from Site 5
    /// </summary>
    public async Task<OperationResultDto> CreateRegistrationAsync(
        CreateRegistrationDto dto,
        CancellationToken ct = default)
    {
        var transactionId = Guid.NewGuid().ToString("N");
        var txInfo = new DistributedTransactionInfo
        {
            TransactionId = transactionId,
            StartTime = DateTime.UtcNow,
            Status = "Initiated"
        };

        _logger.LogInformation("Starting distributed registration creation: {Mssv} -> {Msmon}", dto.Mssv, dto.Msmon);

        try
        {
            // Step 1: Validate student exists and get khoa
            var khoaInfo = await _studentService.GetKhoaByMssvAsync(dto.Mssv, ct);
            if (khoaInfo == null)
            {
                txInfo.Status = "Failed";
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Sinh viên {dto.Mssv} không tồn tại trong hệ thống",
                    TransactionInfo = txInfo
                };
            }

            // Check if registration already exists
            var exists = await _diem1Db.DangKyDiem1
                .AnyAsync(d => d.Mssv == dto.Mssv && d.Msmon == dto.Msmon, ct);

            if (exists)
            {
                txInfo.Status = "Failed";
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Đăng ký ({dto.Mssv}, {dto.Msmon}) đã tồn tại",
                    TransactionInfo = txInfo
                };
            }

            // Step 2: INSERT to Site 5 (diem1) - FIRST OPERATION
            try
            {
                var diem1 = new RegistrationDiem1
                {
                    Mssv = dto.Mssv,
                    Msmon = dto.Msmon,
                    Diem1 = dto.Diem1
                };

                _diem1Db.DangKyDiem1.Add(diem1);
                await _diem1Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 5,
                    SiteName = "dangky_diem1",
                    Operation = "INSERT",
                    Success = true,
                    ExecutedAt = DateTime.UtcNow
                });

                _logger.LogInformation("✓ Site 5 INSERT success: ({Mssv}, {Msmon})", dto.Mssv, dto.Msmon);
            }
            catch (Exception ex)
            {
                txInfo.Status = "RolledBack";
                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 5,
                    SiteName = "dangky_diem1",
                    Operation = "INSERT",
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutedAt = DateTime.UtcNow
                });

                _logger.LogError(ex, "✗ Site 5 INSERT failed - Transaction aborted");
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Lỗi khi thêm vào Site 5: {ex.Message}",
                    TransactionInfo = txInfo
                };
            }

            // Step 3: INSERT to Site 6/7 (diem23) - SECOND OPERATION
            try
            {
                var diem23 = new RegistrationDiem23
                {
                    Mssv = dto.Mssv,
                    Msmon = dto.Msmon,
                    Diem2 = dto.Diem2,
                    Diem3 = dto.Diem3
                };

                int targetSite;
                string targetSiteName;

                if (khoaInfo.Khoa == "K1")
                {
                    _diem23K1Db.DangKyDiem23K1.Add(diem23);
                    await _diem23K1Db.SaveChangesAsync(ct);
                    targetSite = 6;
                    targetSiteName = "dangky_diem23_k1";
                }
                else
                {
                    _diem23K2Db.DangKyDiem23K2.Add(diem23);
                    await _diem23K2Db.SaveChangesAsync(ct);
                    targetSite = 7;
                    targetSiteName = "dangky_diem23_k2";
                }

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = targetSite,
                    SiteName = targetSiteName,
                    Operation = "INSERT",
                    Success = true,
                    ExecutedAt = DateTime.UtcNow
                });

                _logger.LogInformation("✓ Site {Site} INSERT success - Transaction COMMITTED", targetSite);

                txInfo.Status = "Committed";
                txInfo.EndTime = DateTime.UtcNow;

                return new OperationResultDto
                {
                    Success = true,
                    Message = $"Đăng ký tạo thành công trên 2 sites (Site 5 + Site {targetSite})",
                    Data = new { dto.Mssv, dto.Msmon, Khoa = khoaInfo.Khoa },
                    TransactionInfo = txInfo
                };
            }
            catch (Exception ex)
            {
                // COMPENSATING TRANSACTION: Rollback Site 5
                _logger.LogError(ex, "✗ Site 6/7 INSERT failed - Executing compensating transaction");

                var targetSite = khoaInfo.Khoa == "K1" ? 6 : 7;
                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = targetSite,
                    SiteName = khoaInfo.Khoa == "K1" ? "dangky_diem23_k1" : "dangky_diem23_k2",
                    Operation = "INSERT",
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutedAt = DateTime.UtcNow
                });

                try
                {
                    var toRemove = await _diem1Db.DangKyDiem1
                        .FirstOrDefaultAsync(d => d.Mssv == dto.Mssv && d.Msmon == dto.Msmon, ct);

                    if (toRemove != null)
                    {
                        _diem1Db.DangKyDiem1.Remove(toRemove);
                        await _diem1Db.SaveChangesAsync(ct);

                        txInfo.SiteOperations.Add(new SiteOperationInfo
                        {
                            SiteId = 5,
                            SiteName = "dangky_diem1",
                            Operation = "DELETE (Compensating)",
                            Success = true,
                            ExecutedAt = DateTime.UtcNow
                        });

                        _logger.LogInformation("✓ Compensating DELETE from Site 5 success - Rollback complete");
                    }
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogCritical(rollbackEx, "✗✗✗ CRITICAL: Compensating transaction failed - Data inconsistency!");
                    txInfo.SiteOperations.Add(new SiteOperationInfo
                    {
                        SiteId = 5,
                        SiteName = "dangky_diem1",
                        Operation = "DELETE (Compensating)",
                        Success = false,
                        ErrorMessage = rollbackEx.Message,
                        ExecutedAt = DateTime.UtcNow
                    });
                }

                txInfo.Status = "RolledBack";
                txInfo.EndTime = DateTime.UtcNow;

                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Lỗi khi thêm vào Site {targetSite}, đã rollback Site 5: {ex.Message}",
                    TransactionInfo = txInfo
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration creation");
            txInfo.Status = "Failed";
            return new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi không mong đợi: {ex.Message}",
                TransactionInfo = txInfo
            };
        }
    }

    /// <summary>
    /// Cập nhật điểm sử dụng SAGA Pattern
    /// Update across 2 sites: Site 5 (diem1) and Site 6/7 (diem23)
    /// If one fails, both should remain in original state
    /// </summary>
    public async Task<OperationResultDto> UpdateScoresAsync(
        string mssv,
        string msmon,
        UpdateScoreDto dto,
        CancellationToken ct = default)
    {
        var transactionId = Guid.NewGuid().ToString("N");
        var txInfo = new DistributedTransactionInfo
        {
            TransactionId = transactionId,
            StartTime = DateTime.UtcNow,
            Status = "Initiated"
        };

        _logger.LogInformation("Starting distributed score update: ({Mssv}, {Msmon})", mssv, msmon);

        try
        {
            // Step 1: Get student's khoa
            var khoaInfo = await _studentService.GetKhoaByMssvAsync(mssv, ct);
            if (khoaInfo == null)
            {
                txInfo.Status = "Failed";
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Sinh viên {mssv} không tồn tại",
                    TransactionInfo = txInfo
                };
            }

            // Step 2: Check if registration exists
            var existsInSite5 = await _diem1Db.DangKyDiem1
                .AnyAsync(d => d.Mssv == mssv && d.Msmon == msmon, ct);

            if (!existsInSite5)
            {
                txInfo.Status = "Failed";
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Đăng ký ({mssv}, {msmon}) không tồn tại",
                    TransactionInfo = txInfo
                };
            }

            // Store original values for rollback
            decimal? originalDiem1 = null;
            decimal? originalDiem2 = null;
            decimal? originalDiem3 = null;

            // Step 3: Update Site 5 (diem1) if provided
            var site5Updated = false;
            if (dto.Diem1.HasValue)
            {
                try
                {
                    var diem1Record = await _diem1Db.DangKyDiem1
                        .FirstOrDefaultAsync(d => d.Mssv == mssv && d.Msmon == msmon, ct);

                    if (diem1Record != null)
                    {
                        originalDiem1 = diem1Record.Diem1;
                        diem1Record.Diem1 = dto.Diem1.Value;
                        await _diem1Db.SaveChangesAsync(ct);
                        site5Updated = true;

                        txInfo.SiteOperations.Add(new SiteOperationInfo
                        {
                            SiteId = 5,
                            SiteName = "dangky_diem1",
                            Operation = "UPDATE",
                            Success = true,
                            ExecutedAt = DateTime.UtcNow
                        });

                        _logger.LogInformation("✓ Site 5 UPDATE success: diem1 = {Score}", dto.Diem1.Value);
                    }
                }
                catch (Exception ex)
                {
                    txInfo.Status = "Failed";
                    txInfo.SiteOperations.Add(new SiteOperationInfo
                    {
                        SiteId = 5,
                        SiteName = "dangky_diem1",
                        Operation = "UPDATE",
                        Success = false,
                        ErrorMessage = ex.Message,
                        ExecutedAt = DateTime.UtcNow
                    });

                    _logger.LogError(ex, "✗ Site 5 UPDATE failed");
                    return new OperationResultDto
                    {
                        Success = false,
                        Message = $"Lỗi khi cập nhật Site 5: {ex.Message}",
                        TransactionInfo = txInfo
                    };
                }
            }

            // Step 4: Update Site 6/7 (diem23) if provided
            if (dto.Diem2.HasValue || dto.Diem3.HasValue)
            {
                try
                {
                    int targetSite = khoaInfo.Khoa == "K1" ? 6 : 7;
                    string targetSiteName = khoaInfo.Khoa == "K1" ? "dangky_diem23_k1" : "dangky_diem23_k2";

                    RegistrationDiem23? diem23Record = null;

                    if (khoaInfo.Khoa == "K1")
                    {
                        diem23Record = await _diem23K1Db.DangKyDiem23K1
                            .FirstOrDefaultAsync(d => d.Mssv == mssv && d.Msmon == msmon, ct);

                        if (diem23Record != null)
                        {
                            originalDiem2 = diem23Record.Diem2;
                            originalDiem3 = diem23Record.Diem3;

                            if (dto.Diem2.HasValue) diem23Record.Diem2 = dto.Diem2.Value;
                            if (dto.Diem3.HasValue) diem23Record.Diem3 = dto.Diem3.Value;

                            await _diem23K1Db.SaveChangesAsync(ct);
                        }
                    }
                    else
                    {
                        diem23Record = await _diem23K2Db.DangKyDiem23K2
                            .FirstOrDefaultAsync(d => d.Mssv == mssv && d.Msmon == msmon, ct);

                        if (diem23Record != null)
                        {
                            originalDiem2 = diem23Record.Diem2;
                            originalDiem3 = diem23Record.Diem3;

                            if (dto.Diem2.HasValue) diem23Record.Diem2 = dto.Diem2.Value;
                            if (dto.Diem3.HasValue) diem23Record.Diem3 = dto.Diem3.Value;

                            await _diem23K2Db.SaveChangesAsync(ct);
                        }
                    }

                    txInfo.SiteOperations.Add(new SiteOperationInfo
                    {
                        SiteId = targetSite,
                        SiteName = targetSiteName,
                        Operation = "UPDATE",
                        Success = true,
                        ExecutedAt = DateTime.UtcNow
                    });

                    _logger.LogInformation("✓ Site {Site} UPDATE success - Transaction COMMITTED", targetSite);
                }
                catch (Exception ex)
                {
                    // COMPENSATING TRANSACTION: Rollback Site 5 if it was updated
                    _logger.LogError(ex, "✗ Site 6/7 UPDATE failed - Executing compensating transaction");

                    var targetSite = khoaInfo.Khoa == "K1" ? 6 : 7;
                    txInfo.SiteOperations.Add(new SiteOperationInfo
                    {
                        SiteId = targetSite,
                        SiteName = khoaInfo.Khoa == "K1" ? "dangky_diem23_k1" : "dangky_diem23_k2",
                        Operation = "UPDATE",
                        Success = false,
                        ErrorMessage = ex.Message,
                        ExecutedAt = DateTime.UtcNow
                    });

                    if (site5Updated && originalDiem1.HasValue)
                    {
                        try
                        {
                            var diem1Record = await _diem1Db.DangKyDiem1
                                .FirstOrDefaultAsync(d => d.Mssv == mssv && d.Msmon == msmon, ct);

                            if (diem1Record != null)
                            {
                                diem1Record.Diem1 = originalDiem1.Value;
                                await _diem1Db.SaveChangesAsync(ct);

                                txInfo.SiteOperations.Add(new SiteOperationInfo
                                {
                                    SiteId = 5,
                                    SiteName = "dangky_diem1",
                                    Operation = "UPDATE (Compensating)",
                                    Success = true,
                                    ExecutedAt = DateTime.UtcNow
                                });

                                _logger.LogInformation("✓ Compensating UPDATE to Site 5 success - Rollback complete");
                            }
                        }
                        catch (Exception rollbackEx)
                        {
                            _logger.LogCritical(rollbackEx, "✗✗✗ CRITICAL: Compensating transaction failed!");
                            txInfo.SiteOperations.Add(new SiteOperationInfo
                            {
                                SiteId = 5,
                                SiteName = "dangky_diem1",
                                Operation = "UPDATE (Compensating)",
                                Success = false,
                                ErrorMessage = rollbackEx.Message,
                                ExecutedAt = DateTime.UtcNow
                            });
                        }
                    }

                    txInfo.Status = "RolledBack";
                    txInfo.EndTime = DateTime.UtcNow;

                    return new OperationResultDto
                    {
                        Success = false,
                        Message = $"Lỗi khi cập nhật Site {targetSite}, đã rollback thay đổi: {ex.Message}",
                        TransactionInfo = txInfo
                    };
                }
            }

            txInfo.Status = "Committed";
            txInfo.EndTime = DateTime.UtcNow;

            return new OperationResultDto
            {
                Success = true,
                Message = "Cập nhật điểm thành công trên các sites",
                Data = new { Mssv = mssv, Msmon = msmon, UpdatedFields = new { dto.Diem1, dto.Diem2, dto.Diem3 } },
                TransactionInfo = txInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during score update");
            txInfo.Status = "Failed";
            return new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi không mong đợi: {ex.Message}",
                TransactionInfo = txInfo
            };
        }
    }

    /// <summary>
    /// Xóa đăng ký sử dụng SAGA Pattern
    /// Delete from 2 sites atomically: Site 5 and Site 6/7
    /// If second delete fails, re-insert to first site (compensating)
    /// </summary>
    public async Task<OperationResultDto> DeleteRegistrationAsync(
        string mssv,
        string msmon,
        CancellationToken ct = default)
    {
        var transactionId = Guid.NewGuid().ToString("N");
        var txInfo = new DistributedTransactionInfo
        {
            TransactionId = transactionId,
            StartTime = DateTime.UtcNow,
            Status = "Initiated"
        };

        _logger.LogInformation("Starting distributed registration deletion: ({Mssv}, {Msmon})", mssv, msmon);

        try
        {
            // Step 1: Get student's khoa
            var khoaInfo = await _studentService.GetKhoaByMssvAsync(mssv, ct);
            if (khoaInfo == null)
            {
                txInfo.Status = "Failed";
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Sinh viên {mssv} không tồn tại",
                    TransactionInfo = txInfo
                };
            }

            // Step 2: Check if registration exists and backup data for rollback
            var diem1Backup = await _diem1Db.DangKyDiem1
                .FirstOrDefaultAsync(d => d.Mssv == mssv && d.Msmon == msmon, ct);

            if (diem1Backup == null)
            {
                txInfo.Status = "Failed";
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Đăng ký ({mssv}, {msmon}) không tồn tại",
                    TransactionInfo = txInfo
                };
            }

            RegistrationDiem23? diem23Backup = null;
            if (khoaInfo.Khoa == "K1")
            {
                diem23Backup = await _diem23K1Db.DangKyDiem23K1
                    .FirstOrDefaultAsync(d => d.Mssv == mssv && d.Msmon == msmon, ct);
            }
            else
            {
                diem23Backup = await _diem23K2Db.DangKyDiem23K2
                    .FirstOrDefaultAsync(d => d.Mssv == mssv && d.Msmon == msmon, ct);
            }

            // Step 3: DELETE from Site 5 (diem1) - FIRST OPERATION
            try
            {
                _diem1Db.DangKyDiem1.Remove(diem1Backup);
                await _diem1Db.SaveChangesAsync(ct);

                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 5,
                    SiteName = "dangky_diem1",
                    Operation = "DELETE",
                    Success = true,
                    ExecutedAt = DateTime.UtcNow
                });

                _logger.LogInformation("✓ Site 5 DELETE success: ({Mssv}, {Msmon})", mssv, msmon);
            }
            catch (Exception ex)
            {
                txInfo.Status = "Failed";
                txInfo.SiteOperations.Add(new SiteOperationInfo
                {
                    SiteId = 5,
                    SiteName = "dangky_diem1",
                    Operation = "DELETE",
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutedAt = DateTime.UtcNow
                });

                _logger.LogError(ex, "✗ Site 5 DELETE failed - Transaction aborted");
                return new OperationResultDto
                {
                    Success = false,
                    Message = $"Lỗi khi xóa từ Site 5: {ex.Message}",
                    TransactionInfo = txInfo
                };
            }

            // Step 4: DELETE from Site 6/7 (diem23) - SECOND OPERATION
            if (diem23Backup != null)
            {
                try
                {
                    int targetSite = khoaInfo.Khoa == "K1" ? 6 : 7;
                    string targetSiteName = khoaInfo.Khoa == "K1" ? "dangky_diem23_k1" : "dangky_diem23_k2";

                    if (khoaInfo.Khoa == "K1")
                    {
                        _diem23K1Db.DangKyDiem23K1.Remove(diem23Backup);
                        await _diem23K1Db.SaveChangesAsync(ct);
                    }
                    else
                    {
                        _diem23K2Db.DangKyDiem23K2.Remove(diem23Backup);
                        await _diem23K2Db.SaveChangesAsync(ct);
                    }

                    txInfo.SiteOperations.Add(new SiteOperationInfo
                    {
                        SiteId = targetSite,
                        SiteName = targetSiteName,
                        Operation = "DELETE",
                        Success = true,
                        ExecutedAt = DateTime.UtcNow
                    });

                    _logger.LogInformation("✓ Site {Site} DELETE success - Transaction COMMITTED", targetSite);
                }
                catch (Exception ex)
                {
                    // COMPENSATING TRANSACTION: Re-INSERT to Site 5
                    _logger.LogError(ex, "✗ Site 6/7 DELETE failed - Executing compensating transaction");

                    var targetSite = khoaInfo.Khoa == "K1" ? 6 : 7;
                    txInfo.SiteOperations.Add(new SiteOperationInfo
                    {
                        SiteId = targetSite,
                        SiteName = khoaInfo.Khoa == "K1" ? "dangky_diem23_k1" : "dangky_diem23_k2",
                        Operation = "DELETE",
                        Success = false,
                        ErrorMessage = ex.Message,
                        ExecutedAt = DateTime.UtcNow
                    });

                    try
                    {
                        var restoreRecord = new RegistrationDiem1
                        {
                            Mssv = diem1Backup.Mssv,
                            Msmon = diem1Backup.Msmon,
                            Diem1 = diem1Backup.Diem1
                        };

                        _diem1Db.DangKyDiem1.Add(restoreRecord);
                        await _diem1Db.SaveChangesAsync(ct);

                        txInfo.SiteOperations.Add(new SiteOperationInfo
                        {
                            SiteId = 5,
                            SiteName = "dangky_diem1",
                            Operation = "INSERT (Compensating)",
                            Success = true,
                            ExecutedAt = DateTime.UtcNow
                        });

                        _logger.LogInformation("✓ Compensating INSERT to Site 5 success - Rollback complete");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogCritical(rollbackEx, "✗✗✗ CRITICAL: Compensating transaction failed - Data inconsistency!");
                        txInfo.SiteOperations.Add(new SiteOperationInfo
                        {
                            SiteId = 5,
                            SiteName = "dangky_diem1",
                            Operation = "INSERT (Compensating)",
                            Success = false,
                            ErrorMessage = rollbackEx.Message,
                            ExecutedAt = DateTime.UtcNow
                        });
                    }

                    txInfo.Status = "RolledBack";
                    txInfo.EndTime = DateTime.UtcNow;

                    return new OperationResultDto
                    {
                        Success = false,
                        Message = $"Lỗi khi xóa từ Site {targetSite}, đã rollback Site 5: {ex.Message}",
                        TransactionInfo = txInfo
                    };
                }
            }

            txInfo.Status = "Committed";
            txInfo.EndTime = DateTime.UtcNow;

            return new OperationResultDto
            {
                Success = true,
                Message = $"Xóa đăng ký thành công từ 2 sites",
                Data = new { Mssv = mssv, Msmon = msmon },
                TransactionInfo = txInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration deletion");
            txInfo.Status = "Failed";
            return new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi không mong đợi: {ex.Message}",
                TransactionInfo = txInfo
            };
        }
    }
}
