using Microsoft.AspNetCore.Mvc;
using DistributedDbApi.Services;
using DistributedDbApi.DTOs;

namespace DistributedDbApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StudentsController : ControllerBase
{
    private readonly StudentService _studentService;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(StudentService studentService, ILogger<StudentsController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    /// <summary>
    /// Tra cứu khoa của sinh viên
    /// </summary>
    [HttpGet("{mssv}/khoa")]
    [ProducesResponseType(typeof(ApiResponse<StudentWithKhoaDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetKhoa(string mssv, CancellationToken ct)
    {
        try
        {
            var result = await _studentService.GetKhoaByMssvAsync(mssv, ct);
            
            if (result == null)
            {
                return NotFound(new ApiResponse<object>(
                    false, 
                    null, 
                    $"Sinh viên {mssv} không tồn tại"));
            }

            return Ok(new ApiResponse<StudentWithKhoaDto>(true, result, "Tra cứu thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tra cứu khoa cho {Mssv}", mssv);
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết sinh viên
    /// </summary>
    [HttpGet("{mssv}")]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetStudent(string mssv, CancellationToken ct)
    {
        try
        {
            var result = await _studentService.GetStudentByMssvAsync(mssv, ct);
            
            if (result == null)
            {
                return NotFound(new ApiResponse<object>(false, null, $"Sinh viên {mssv} không tồn tại"));
            }

            return Ok(new ApiResponse<StudentDto>(true, result, "Thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin sinh viên {Mssv}", mssv);
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Lấy danh sách tất cả sinh viên
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StudentDto>>), 200)]
    public async Task<IActionResult> GetAllStudents(
        [FromQuery] string? khoa,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            var results = await _studentService.SearchStudentsAsync(null, khoa, page, pageSize, ct);
            
            return Ok(new ApiResponse<List<StudentDto>>(true, results, $"Tìm thấy {results.Count} sinh viên"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách sinh viên");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Tìm kiếm sinh viên theo tên/MSSV và khoa
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<StudentDto>>), 200)]
    public async Task<IActionResult> SearchStudents(
        [FromQuery] string? name,
        [FromQuery] string? khoa,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            var results = await _studentService.SearchStudentsAsync(name, khoa, page, pageSize, ct);
            
            return Ok(new ApiResponse<List<StudentDto>>(true, results, $"Tìm thấy {results.Count} sinh viên"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tìm kiếm sinh viên");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    // ==================== WRITE OPERATIONS ====================

    /// <summary>
    /// Tạo mới sinh viên - DISTRIBUTED OPERATION với smart routing
    /// INSERT vào Site 3 (K1) hoặc Site 4 (K2) dựa trên mslop
    /// </summary>
    /// <remarks>
    /// Tính năng phân tán:
    /// - Smart routing: Xác định khoa từ mslop (lookup lop_k1/lop_k2)
    /// - Site localization: Route đến đúng site dựa trên khoa
    /// - Transparency: User không cần biết site nào được sử dụng
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(OperationResultDto), 201)]
    [ProducesResponseType(typeof(OperationResultDto), 400)]
    [ProducesResponseType(typeof(OperationResultDto), 409)]
    public async Task<IActionResult> CreateStudent(
        [FromBody] CreateStudentDto dto,
        CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new OperationResultDto
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Data = ModelState
                });
            }

            var result = await _studentService.CreateStudentAsync(dto, ct);

            if (!result.Success)
            {
                return result.Message.Contains("đã tồn tại") 
                    ? Conflict(result) 
                    : BadRequest(result);
            }

            // Extract generated MSSV from result.Data
            var mssv = result.Data?.GetType().GetProperty("mssv")?.GetValue(result.Data)?.ToString() ?? "";

            return CreatedAtAction(
                nameof(GetStudent), 
                new { mssv }, 
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo sinh viên");
            return StatusCode(500, new OperationResultDto
            {
                Success = false,
                Message = "Lỗi server"
            });
        }
    }

    /// <summary>
    /// Cập nhật thông tin sinh viên - CÓ THỂ CROSS-SITE nếu chuyển khoa
    /// SAGA PATTERN: DELETE từ site cũ + INSERT vào site mới
    /// </summary>
    /// <remarks>
    /// Tính năng phân tán:
    /// - Same-site update: Nếu không đổi khoa → UPDATE tại site hiện tại
    /// - Cross-site transfer (SAGA): Nếu chuyển khoa khác:
    ///   1. Lấy full data từ site cũ
    ///   2. INSERT vào site mới với data cập nhật
    ///   3. DELETE từ site cũ (chỉ khi INSERT thành công)
    ///   4. Compensating transaction: Nếu INSERT fail → giữ nguyên site cũ
    /// - Distributed transaction tracking: TransactionInfo với site operations
    /// </remarks>
    [HttpPatch("{mssv}")]
    [ProducesResponseType(typeof(OperationResultDto), 200)]
    [ProducesResponseType(typeof(OperationResultDto), 404)]
    [ProducesResponseType(typeof(OperationResultDto), 400)]
    public async Task<IActionResult> UpdateStudent(
        string mssv,
        [FromBody] UpdateStudentDto dto,
        CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new OperationResultDto
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Data = ModelState
                });
            }

            var result = await _studentService.UpdateStudentAsync(mssv, dto, ct);

            if (!result.Success)
            {
                return result.Message.Contains("không tồn tại")
                    ? NotFound(result)
                    : BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật sinh viên {Mssv}", mssv);
            return StatusCode(500, new OperationResultDto
            {
                Success = false,
                Message = "Lỗi server"
            });
        }
    }

    /// <summary>
    /// Xóa sinh viên - DELETE từ site tương ứng
    /// </summary>
    /// <remarks>
    /// Tính năng phân tán:
    /// - Fragment lookup: Tìm sinh viên ở site K1 hoặc K2
    /// - Targeted delete: DELETE chỉ từ site chứa sinh viên
    /// - Warning: Nên xóa registrations trước (cascade delete)
    /// 
    /// Production consideration: 
    /// Nên implement SAGA để xóa registrations (sites 5/6/7) trước khi xóa student
    /// </remarks>
    [HttpDelete("{mssv}")]
    [ProducesResponseType(typeof(OperationResultDto), 200)]
    [ProducesResponseType(typeof(OperationResultDto), 404)]
    public async Task<IActionResult> DeleteStudent(
        string mssv,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _studentService.DeleteStudentAsync(mssv, ct);

            if (!result.Success)
            {
                return result.Message.Contains("không tồn tại")
                    ? NotFound(result)
                    : BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa sinh viên {Mssv}", mssv);
            return StatusCode(500, new OperationResultDto
            {
                Success = false,
                Message = "Lỗi server"
            });
        }
    }
}
