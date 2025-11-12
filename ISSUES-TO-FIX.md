# 🔧 DANH SÁCH CÁC VẤN ĐỀ CẦN FIX

## ❌ API ENDPOINTS BỊ THIẾU/LỖI

### 1. GET /api/students - THIẾU
**Hiện tại**: KHÔNG CÓ endpoint
**Mong đợi**: Lấy danh sách tất cả sinh viên với phân trang
**Frontend đang dùng**: `searchStudents()` API
**Solution**: 
- Option A: Thêm endpoint `GET /api/students` trong StudentsController
- Option B: Frontend gọi `/api/students/search` không params

**Trạng thái**: ⚠️ CẦN FIX - Frontend không hiển thị được danh sách sinh viên

---

### 2. GET /api/registrations - THIẾU
**Hiện tại**: Chỉ có `GET /api/registrations/students/{mssv}/scores`
**Mong đợi**: Lấy danh sách TẤT CẢ đăng ký
**Solution**: Thêm endpoint `GET /api/registrations` trong RegistrationsController

**Trạng thái**: ⚠️ CẦN FIX - Trang /registrations không load được dữ liệu

---

### 3. GET /api/students/search?name=XXX - LỖI 400
**Hiện tại**: Backend expect param `q` nhưng frontend gửi `name`
**Code Backend**: `[FromQuery] string? q`
**Code Frontend**: `queryParams.append('name', params.name)`
**Solution**: 
- Option A: Sửa backend từ `q` → `name`
- Option B: Sửa frontend từ `name` → `q`

**Trạng thái**: ⚠️ CẦN FIX - Tìm kiếm sinh viên không hoạt động

---

## ✅ ĐÃ HOẠT ĐỘNG ĐÚNG

1. ✓ GET /api/admin/overview - Dashboard thống kê
2. ✓ GET /api/admin/sites/health - Health check 7 sites
3. ✓ GET /api/classes - Danh sách lớp (20 lớp)
4. ✓ GET /api/classes?khoa=K1 - Filter theo khoa
5. ✓ GET /api/classes/L01 - Chi tiết lớp
6. ✓ GET /api/students/SV001 - Chi tiết sinh viên
7. ✓ Tên lớp trực quan (Lập trình Web, Data Science...)

---

## 📝 DỮ LIỆU DATABASE

- ✓ 20 lớp học (L01-L10: K1, L11-L20: K2)
- ✓ 60 sinh viên (30 K1 + 30 K2)
- ✓ 180 đăng ký với điểm
- ✓ Tên lớp trực quan trong database

---

## 🔄 CÁC BƯỚC FIX

### Bước 1: Fix API Backend (Priority: HIGH)

#### a) Thêm endpoint GET /api/students
File: `src/DistributedDbApi/Controllers/StudentsController.cs`

```csharp
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
```

**Thứ tự route**: Đặt TRƯỚC `[HttpGet("search")]` để tránh conflict

---

#### b) Thêm endpoint GET /api/registrations
File: `src/DistributedDbApi/Controllers/RegistrationsController.cs`

```csharp
/// <summary>
/// Lấy danh sách tất cả đăng ký
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
        // TODO: Implement trong RegistrationService
        // Query từ Site 5 (diem1) JOIN với Sites 6/7 (diem2, diem3)
        
        return Ok(new ApiResponse<List<RegistrationScoreDto>>(
            true, 
            new List<RegistrationScoreDto>(), 
            "Success"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Lỗi khi lấy danh sách đăng ký");
        return StatusCode(500, new ApiResponse<object>(false, null, "Lỗi server"));
    }
}
```

---

#### c) Fix search param name
File: `src/DistributedDbApi/Controllers/StudentsController.cs`

Đổi từ:
```csharp
[FromQuery] string? q
```

Thành:
```csharp
[FromQuery] string? name
```

Và update service call:
```csharp
var results = await _studentService.SearchStudentsAsync(name, khoa, page, pageSize, ct);
```

---

### Bước 2: Restart Backend
```bash
cd src/DistributedDbApi
dotnet build
dotnet run
```

---

### Bước 3: Test lại
```bash
./test-system.sh
```

---

## 📊 EXPECTED RESULTS SAU KHI FIX

```
✓ GET /api/students → 200 (60 sinh viên)
✓ GET /api/students/search?name=Nguyễn → 200
✓ GET /api/registrations → 200 (180 đăng ký)
```

---

## 🎯 CHECKLIST SAU KHI FIX

- [ ] Backend build thành công
- [ ] GET /api/students returns 200
- [ ] GET /api/registrations returns 200
- [ ] Search sinh viên hoạt động
- [ ] Frontend /students page hiển thị danh sách
- [ ] Frontend /registrations page hiển thị danh sách
- [ ] SAGA tracking vẫn hoạt động
- [ ] Tất cả test pass

---

**File này sẽ được update khi fix xong!**
