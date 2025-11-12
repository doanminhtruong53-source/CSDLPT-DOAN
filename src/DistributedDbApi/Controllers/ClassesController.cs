using Microsoft.AspNetCore.Mvc;
using DistributedDbApi.Services;
using DistributedDbApi.DTOs;

namespace DistributedDbApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClassesController : ControllerBase
{
    private readonly ClassService _classService;
    private readonly ILogger<ClassesController> _logger;

    public ClassesController(ClassService classService, ILogger<ClassesController> logger)
    {
        _classService = classService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách lớp, có thể lọc theo khoa và tìm kiếm
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ClassDto>>), 200)]
    public async Task<IActionResult> GetClasses(
        [FromQuery] string? khoa,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            var results = await _classService.GetClassesAsync(khoa, q, page, pageSize, ct);
            
            return Ok(new ApiResponse<List<ClassDto>>(true, results, $"Tìm thấy {results.Count} lớp"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách lớp");
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết một lớp
    /// </summary>
    [HttpGet("{mslop}")]
    [ProducesResponseType(typeof(ApiResponse<ClassDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetClass(string mslop, CancellationToken ct)
    {
        try
        {
            var result = await _classService.GetClassByMslopAsync(mslop, ct);
            
            if (result == null)
            {
                return NotFound(new ApiResponse<object>(false, null, $"Lớp {mslop} không tồn tại"));
            }

            return Ok(new ApiResponse<ClassDto>(true, result, "Thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin lớp {Mslop}", mslop);
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    /// <summary>
    /// Lấy danh sách sinh viên của một lớp
    /// </summary>
    [HttpGet("{mslop}/students")]
    [ProducesResponseType(typeof(ApiResponse<List<StudentDto>>), 200)]
    public async Task<IActionResult> GetStudentsByClass(
        string mslop,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            var results = await _classService.GetStudentsByClassAsync(mslop, page, pageSize, ct);
            
            return Ok(new ApiResponse<List<StudentDto>>(true, results, $"Tìm thấy {results.Count} sinh viên"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy sinh viên lớp {Mslop}", mslop);
            return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
        }
    }

    // ==================== WRITE OPERATIONS ====================

    /// <summary>
    /// POST /api/classes - Tạo lớp mới
    /// DISTRIBUTED WRITE: Ghi vào Site 1 (K1) hoặc Site 2 (K2)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassDto dto, CancellationToken ct)
    {
        try
        {
            if (dto == null)
            {
                _logger.LogWarning("⚠️ Received null dto");
                return BadRequest(new ApiResponse<object>(false, null, "Dữ liệu không được để trống"));
            }

            _logger.LogInformation("📥 POST /api/classes - Received: Tenlop='{Tenlop}', Khoa='{Khoa}'", 
                dto.Tenlop, dto.Khoa);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                var errorMsg = string.Join(", ", errors);
                
                _logger.LogWarning("⚠️ Validation failed: {Errors}", errorMsg);
                return BadRequest(new ApiResponse<object>(false, null, $"Dữ liệu không hợp lệ: {errorMsg}"));
            }

            var result = await _classService.CreateClassAsync(dto, ct);
            
            if (result.Success)
            {
                // Get mslop from result data
                var classDto = result.Data as ClassDto;
                var mslop = classDto?.Mslop ?? "unknown";
                
                _logger.LogInformation("✅ Tạo lớp {Mslop} thành công", mslop);
                return CreatedAtAction(nameof(GetClass), new { mslop }, 
                    new ApiResponse<OperationResultDto>(true, result, result.Message));
            }
            else
            {
                return BadRequest(new ApiResponse<OperationResultDto>(false, result, result.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi tạo lớp");
            return StatusCode(500, new ApiResponse<object>(false, null, $"Lỗi server: {ex.Message}"));
        }
    }

    /// <summary>
    /// PUT /api/classes/{mslop} - Cập nhật lớp
    /// DISTRIBUTED UPDATE: Update Site 1 (K1) hoặc Site 2 (K2)
    /// </summary>
    [HttpPut("{mslop}")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateClass(string mslop, [FromBody] UpdateClassDto dto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>(false, null, "Dữ liệu không hợp lệ"));
            }

            var result = await _classService.UpdateClassAsync(mslop, dto, ct);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ Cập nhật lớp {Mslop} thành công", mslop);
                return Ok(new ApiResponse<OperationResultDto>(true, result, result.Message));
            }
            else
            {
                return NotFound(new ApiResponse<OperationResultDto>(false, result, result.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi cập nhật lớp {Mslop}", mslop);
            return StatusCode(500, new ApiResponse<object>(false, null, $"Lỗi server: {ex.Message}"));
        }
    }

    /// <summary>
    /// DELETE /api/classes/{mslop} - Xoá lớp
    /// DISTRIBUTED DELETE với SAGA: Kiểm tra sinh viên trước khi xoá
    /// </summary>
    [HttpDelete("{mslop}")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteClass(string mslop, CancellationToken ct)
    {
        try
        {
            var result = await _classService.DeleteClassAsync(mslop, ct);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ Xoá lớp {Mslop} thành công", mslop);
                return Ok(new ApiResponse<OperationResultDto>(true, result, result.Message));
            }
            else
            {
                // Có thể là không tìm thấy hoặc còn sinh viên
                return BadRequest(new ApiResponse<OperationResultDto>(false, result, result.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi xoá lớp {Mslop}", mslop);
            return StatusCode(500, new ApiResponse<object>(false, null, $"Lỗi server: {ex.Message}"));
        }
    }
}
