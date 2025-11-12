using Microsoft.AspNetCore.Mvc;
using DistributedDbApi.Services;
using DistributedDbApi.DTOs;

namespace DistributedDbApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RegistrationsController : ControllerBase
{
    private readonly RegistrationService _registrationService;
    private readonly ILogger<RegistrationsController> _logger;

    public RegistrationsController(RegistrationService registrationService, ILogger<RegistrationsController> logger)
    {
        _registrationService = registrationService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách tất cả đăng ký (join từ 3 sites)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RegistrationScoreDto>>), 200)]
    public async Task<IActionResult> GetAllRegistrations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            if (pageSize > 200) pageSize = 200;
            if (page < 1) page = 1;

            var results = await _registrationService.GetAllRegistrationsAsync(page, pageSize, ct);
            
            return Ok(new ApiResponse<List<RegistrationScoreDto>>(true, results, $"Tìm thấy {results.Count} đăng ký"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách đăng ký");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Lấy điểm tất cả môn của sinh viên (join phân tán từ 3 sites)
    /// </summary>
    [HttpGet("students/{mssv}/scores")]
    [ProducesResponseType(typeof(ApiResponse<List<RegistrationScoreDto>>), 200)]
    public async Task<IActionResult> GetScoresByStudent(string mssv, CancellationToken ct)
    {
        try
        {
            var results = await _registrationService.GetScoresByMssvAsync(mssv, ct);
            
            if (results.Count == 0)
            {
                return Ok(new ApiResponse<List<RegistrationScoreDto>>(
                    true, 
                    results, 
                    "Sinh viên chưa đăng ký môn nào hoặc không tồn tại"));
            }

            return Ok(new ApiResponse<List<RegistrationScoreDto>>(true, results, $"Tìm thấy {results.Count} môn"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy điểm sinh viên {Mssv}", mssv);
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Lấy danh sách sinh viên học một môn
    /// </summary>
    [HttpGet("subjects/{msmon}/students")]
    [ProducesResponseType(typeof(ApiResponse<List<RegistrationScoreDto>>), 200)]
    public async Task<IActionResult> GetStudentsBySubject(
        string msmon,
        [FromQuery] bool includeScores = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            var results = await _registrationService.GetStudentsBySubjectAsync(msmon, includeScores, page, pageSize, ct);
            
            return Ok(new ApiResponse<List<RegistrationScoreDto>>(true, results, $"Tìm thấy {results.Count} sinh viên"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy sinh viên môn {Msmon}", msmon);
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Tạo đăng ký mới - DISTRIBUTED WRITE với SAGA Pattern
    /// </summary>
    /// <remarks>
    /// **SAGA Pattern Flow:**
    /// 1. Validate student exists and get khoa
    /// 2. INSERT to Site 5 (dangky_diem1) - FIRST OPERATION
    /// 3. INSERT to Site 6 or 7 (dangky_diem23_k1/k2) based on khoa - SECOND OPERATION
    /// 4. If step 3 fails → COMPENSATING TRANSACTION: DELETE from Site 5
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "mssv": "SV001",
    ///   "msmon": "M08",
    ///   "diem1": 7.5,
    ///   "diem2": 8.0,
    ///   "diem3": 8.5
    /// }
    /// ```
    /// 
    /// **Distributed Transaction Info:**
    /// Response includes transaction tracking with operations at each site:
    /// - TransactionId: Unique identifier for this distributed operation
    /// - SiteOperations: List of operations performed (INSERT, DELETE compensating)
    /// - Status: Committed (success) or RolledBack (failure)
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(OperationResultDto), 201)]
    [ProducesResponseType(typeof(OperationResultDto), 400)]
    [ProducesResponseType(typeof(OperationResultDto), 409)]
    public async Task<IActionResult> CreateRegistration(
        [FromBody] CreateRegistrationDto dto,
        CancellationToken ct)
    {
        try
        {
            var result = await _registrationService.CreateRegistrationAsync(dto, ct);

            if (result.Success)
            {
                _logger.LogInformation("✓ Đăng ký created: ({Mssv}, {Msmon}) - TxId: {TxId}", 
                    dto.Mssv, dto.Msmon, result.TransactionInfo?.TransactionId);
                
                return CreatedAtAction(
                    nameof(GetScoresByStudent),
                    new { mssv = dto.Mssv },
                    result);
            }

            _logger.LogWarning("✗ Đăng ký failed: ({Mssv}, {Msmon}) - {Message}", 
                dto.Mssv, dto.Msmon, result.Message);

            // Conflict if already exists
            if (result.Message.Contains("đã tồn tại"))
            {
                return Conflict(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating registration");
            return StatusCode(500, new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi server: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Cập nhật điểm - DISTRIBUTED UPDATE với SAGA Pattern
    /// </summary>
    /// <remarks>
    /// **SAGA Pattern for UPDATE:**
    /// 1. UPDATE Site 5 (diem1) if diem1 provided - FIRST OPERATION
    /// 2. UPDATE Site 6/7 (diem2/diem3) if provided - SECOND OPERATION
    /// 3. If step 2 fails → COMPENSATING: Restore Site 5 to original value
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "diem1": 8.0,
    ///   "diem2": 8.5,
    ///   "diem3": 9.0
    /// }
    /// ```
    /// 
    /// **Partial Updates:**
    /// You can update only specific scores (diem1, diem2, or diem3).
    /// Omitted fields will remain unchanged.
    /// 
    /// **Atomicity:**
    /// If updating multiple scores fails at any site, changes are rolled back
    /// using compensating transactions to maintain consistency.
    /// </remarks>
    [HttpPatch("{mssv}/{msmon}")]
    [ProducesResponseType(typeof(OperationResultDto), 200)]
    [ProducesResponseType(typeof(OperationResultDto), 404)]
    [ProducesResponseType(typeof(OperationResultDto), 400)]
    public async Task<IActionResult> UpdateScores(
        string mssv,
        string msmon,
        [FromBody] UpdateScoreDto dto,
        CancellationToken ct)
    {
        try
        {
            var result = await _registrationService.UpdateScoresAsync(mssv, msmon, dto, ct);

            if (result.Success)
            {
                _logger.LogInformation("✓ Scores updated: ({Mssv}, {Msmon}) - TxId: {TxId}",
                    mssv, msmon, result.TransactionInfo?.TransactionId);
                return Ok(result);
            }

            _logger.LogWarning("✗ Update failed: ({Mssv}, {Msmon}) - {Message}",
                mssv, msmon, result.Message);

            if (result.Message.Contains("không tồn tại"))
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating scores");
            return StatusCode(500, new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi server: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Xóa đăng ký - DISTRIBUTED DELETE với SAGA Pattern
    /// </summary>
    /// <remarks>
    /// **SAGA Pattern for DELETE:**
    /// 1. DELETE from Site 5 (dangky_diem1) - FIRST OPERATION
    /// 2. DELETE from Site 6/7 (dangky_diem23) - SECOND OPERATION
    /// 3. If step 2 fails → COMPENSATING: Re-INSERT to Site 5 with backed-up data
    /// 
    /// **Example Request:**
    /// ```
    /// DELETE /api/registrations/SV001/M08
    /// ```
    /// 
    /// **Atomicity Guarantee:**
    /// Either both deletes succeed (Committed) or both are rolled back (RolledBack).
    /// Uses compensating INSERT to restore Site 5 data if Site 6/7 delete fails.
    /// 
    /// **Transaction Tracking:**
    /// Response includes detailed site-by-site operation log for debugging
    /// distributed transaction behavior.
    /// </remarks>
    [HttpDelete("{mssv}/{msmon}")]
    [ProducesResponseType(typeof(OperationResultDto), 200)]
    [ProducesResponseType(typeof(OperationResultDto), 404)]
    public async Task<IActionResult> DeleteRegistration(
        string mssv,
        string msmon,
        CancellationToken ct)
    {
        try
        {
            var result = await _registrationService.DeleteRegistrationAsync(mssv, msmon, ct);

            if (result.Success)
            {
                _logger.LogInformation("✓ Registration deleted: ({Mssv}, {Msmon}) - TxId: {TxId}",
                    mssv, msmon, result.TransactionInfo?.TransactionId);
                return Ok(result);
            }

            _logger.LogWarning("✗ Delete failed: ({Mssv}, {Msmon}) - {Message}",
                mssv, msmon, result.Message);

            if (result.Message.Contains("không tồn tại"))
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting registration");
            return StatusCode(500, new OperationResultDto
            {
                Success = false,
                Message = $"Lỗi server: {ex.Message}"
            });
        }
    }
}
