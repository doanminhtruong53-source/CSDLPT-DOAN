# ✅ CHECKLIST KIỂM TRA HỆ THỐNG

## 📋 DANH SÁCH KIỂM TRA

### 🏠 1. TRANG CHỦ (Dashboard)
- [ ] Hiển thị tổng số lớp học
- [ ] Hiển thị tổng số sinh viên  
- [ ] Hiển thị tổng số đăng ký
- [ ] Hiển thị health status 7 sites
- [ ] Animation load mượt mà
- [ ] Card statistics hiển thị đúng

### 📚 2. QUẢN LÝ LỚP HỌC (/classes)
- [ ] **Danh sách lớp:**
  - [ ] Hiển thị tên lớp TRỰC QUAN (Lập trình Web, Data Science...)
  - [ ] Hiển thị mã lớp (L01, L11...)
  - [ ] Hiển thị khoa (K1, K2)
  - [ ] Hiển thị số lượng sinh viên trong lớp
  - [ ] Phân trang hoạt động
  - [ ] Search theo tên lớp
  - [ ] Animation card mượt

- [ ] **Xem chi tiết lớp:**
  - [ ] Thông tin lớp đầy đủ
  - [ ] Danh sách sinh viên trong lớp
  - [ ] Nút chuyển đến profile sinh viên

- [ ] **Tạo lớp mới:**
  - [ ] Form validation đầy đủ
  - [ ] Chọn khoa (K1/K2)
  - [ ] Tạo thành công với SAGA tracking
  - [ ] Hiển thị message thành công/lỗi

- [ ] **Sửa lớp:**
  - [ ] Load dữ liệu cũ vào form
  - [ ] Update thành công
  - [ ] Validation khi sửa

- [ ] **Xóa lớp:**
  - [ ] Confirm dialog hiển thị
  - [ ] Xóa thành công với SAGA
  - [ ] Không xóa được nếu có sinh viên

### 👨‍🎓 3. QUẢN LÝ SINH VIÊN (/students)
- [ ] **Danh sách sinh viên:**
  - [ ] Hiển thị MSSV
  - [ ] Hiển thị họ tên
  - [ ] Hiển thị giới tính
  - [ ] Hiển thị ngày sinh (format dd/mm/yyyy)
  - [ ] Hiển thị TÊN LỚP TRỰC QUAN (không chỉ mã)
  - [ ] Hiển thị học bổng (format tiền VNĐ)
  - [ ] Search theo tên
  - [ ] Filter theo lớp
  - [ ] Phân trang

- [ ] **Xem chi tiết sinh viên:**
  - [ ] Thông tin cá nhân đầy đủ
  - [ ] Danh sách môn đã đăng ký
  - [ ] Hiển thị TÊN MÔN HỌC (Toán cao cấp, không chỉ M01)
  - [ ] Hiển thị điểm TX, GK, CK
  - [ ] Tính điểm trung bình
  - [ ] Phân loại học lực (Xuất sắc/Giỏi/Khá/TB/Yếu)

- [ ] **Tạo sinh viên mới:**
  - [ ] MSSV tự động generate (KHÔNG nhập tay)
  - [ ] Hiển thị MSSV vừa tạo
  - [ ] Form đầy đủ: Họ tên, Giới tính, Ngày sinh
  - [ ] Chọn lớp (dropdown với tên trực quan)
  - [ ] Nhập học bổng
  - [ ] Validation đầy đủ
  - [ ] SAGA tracking hiển thị
  - [ ] Message thành công

- [ ] **Sửa sinh viên:**
  - [ ] KHÔNG sửa được MSSV (readonly)
  - [ ] Load dữ liệu cũ
  - [ ] Update thông tin thành công
  - [ ] Validation

- [ ] **Xóa sinh viên:**
  - [ ] Confirm dialog
  - [ ] Xóa cascade: sinh viên + đăng ký trên 3 sites
  - [ ] SAGA tracking

### 📝 4. ĐĂNG KÝ & ĐIỂM SỐ (/registrations)
- [ ] **Danh sách đăng ký:**
  - [ ] Hiển thị MSSV + Tên sinh viên
  - [ ] Hiển thị TÊN MÔN HỌC (không chỉ mã)
  - [ ] Hiển thị điểm TX, GK, CK
  - [ ] Hiển thị điểm trung bình
  - [ ] Search theo MSSV hoặc tên
  - [ ] Filter theo môn học
  - [ ] Phân trang

- [ ] **Tạo đăng ký mới:**
  - [ ] Chọn sinh viên (dropdown với tên)
  - [ ] Chọn môn học (dropdown với TÊN TRỰC QUAN)
  - [ ] Nhập điểm TX (0-10, optional)
  - [ ] Nhập điểm GK (0-10, optional)
  - [ ] Nhập điểm CK (0-10, optional)
  - [ ] Validation: điểm hợp lệ
  - [ ] SAGA tracking: Site 5 (TX) + Site 6/7 (GK, CK)
  - [ ] Kiểm tra trùng (MSSV + Môn)

- [ ] **Cập nhật điểm:**
  - [ ] Load điểm hiện tại
  - [ ] Cho phép sửa TX, GK, CK
  - [ ] PATCH request đúng
  - [ ] Update thành công trên đúng site
  - [ ] Validation

- [ ] **Xóa đăng ký:**
  - [ ] Confirm dialog
  - [ ] Xóa trên đúng sites (5, 6 hoặc 7)
  - [ ] SAGA tracking

### 📊 5. BÁO CÁO (/reports)
- [ ] **Báo cáo tổng quan:**
  - [ ] Số lượng lớp mỗi khoa
  - [ ] Số lượng sinh viên mỗi khoa
  - [ ] Số lượng đăng ký mỗi môn
  - [ ] Chart hiển thị đẹp

- [ ] **Top sinh viên:**
  - [ ] Xếp hạng theo điểm TB
  - [ ] Hiển thị tên sinh viên
  - [ ] Hiển thị điểm TB
  - [ ] Hiển thị xếp hạng

- [ ] **Thống kê môn học:**
  - [ ] Số lượng đăng ký mỗi môn
  - [ ] Điểm TB mỗi môn
  - [ ] TÊN MÔN TRỰC QUAN

### 🏥 6. HEALTH CHECK (/admin)
- [ ] **Site Status:**
  - [ ] Hiển thị 7 sites
  - [ ] Status: Healthy/Unhealthy
  - [ ] Response time
  - [ ] Tên site rõ ràng
  - [ ] Icon trạng thái

- [ ] **SAGA Transactions:**
  - [ ] List các transaction gần đây
  - [ ] Status: Completed/Failed/Pending
  - [ ] Chi tiết từng step
  - [ ] Timestamp

## 🔧 7. API ENDPOINTS

### Classes API
- [ ] `GET /api/classes` - Lấy danh sách lớp
- [ ] `GET /api/classes/{mslop}` - Chi tiết lớp
- [ ] `POST /api/classes` - Tạo lớp mới
- [ ] `PUT /api/classes/{mslop}` - Cập nhật lớp
- [ ] `DELETE /api/classes/{mslop}` - Xóa lớp

### Students API
- [ ] `GET /api/students` - Danh sách sinh viên
- [ ] `GET /api/students/{mssv}` - Chi tiết sinh viên
- [ ] `GET /api/students/search?name=` - Tìm kiếm
- [ ] `POST /api/students` - Tạo sinh viên (MSSV auto)
- [ ] `PUT /api/students/{mssv}` - Cập nhật
- [ ] `DELETE /api/students/{mssv}` - Xóa (cascade)

### Registrations API
- [ ] `GET /api/registrations` - Danh sách đăng ký
- [ ] `GET /api/registrations/{mssv}` - Đăng ký của sinh viên
- [ ] `POST /api/registrations` - Tạo đăng ký (TX+GK+CK)
- [ ] `PATCH /api/registrations/{mssv}/{msmon}` - Update điểm
- [ ] `DELETE /api/registrations/{mssv}/{msmon}` - Xóa

### Admin API
- [ ] `GET /api/admin/overview` - Thống kê tổng quan
- [ ] `GET /api/admin/sites/health` - Health check 7 sites
- [ ] `GET /api/admin/saga-transactions` - SAGA history

## 🎨 8. UI/UX
- [ ] **Layout:**
  - [ ] Sidebar navigation hoạt động
  - [ ] Active link highlight
  - [ ] Responsive trên mobile
  - [ ] Dark mode (nếu có)

- [ ] **Loading States:**
  - [ ] Skeleton loading khi fetch data
  - [ ] Spinner khi submit form
  - [ ] Disable button khi đang xử lý

- [ ] **Error Handling:**
  - [ ] Toast notification hiển thị lỗi
  - [ ] Error message rõ ràng
  - [ ] Retry khi API fail

- [ ] **Animation:**
  - [ ] Framer Motion mượt mà
  - [ ] Card hover effect
  - [ ] Page transition

## 🔍 9. KIỂM TRA ĐẶC BIỆT

### Dữ liệu trực quan
- [ ] **Tên lớp:** "Lập trình Web (ReactJS & Node.js)" thay vì "L01"
- [ ] **Tên môn:** "Toán cao cấp" thay vì "M01"
- [ ] **Điểm:** "TX/GK/CK" thay vì "Điểm 1/2/3"
- [ ] **Khoa:** "Khoa CNTT 1" thay vì "K1"

### SAGA Pattern
- [ ] **Tạo sinh viên:** Transaction tracking hiển thị
- [ ] **Xóa sinh viên:** Cascade delete trên 3 sites
- [ ] **Tạo đăng ký:** Insert vào Site 5 + (6 hoặc 7)
- [ ] **Rollback:** Khi có lỗi, rollback tất cả

### Fragmentation
- [ ] **Vertical:** Điểm 1 (Site 5) tách Điểm 2&3 (Sites 6/7)
- [ ] **Horizontal:** K1 (Site 6) tách K2 (Site 7)
- [ ] **Query đúng site:** API gọi đúng site theo logic

## 📝 GHI CHÚ
- Backend: http://localhost:5020
- Frontend: http://localhost:3000
- Swagger: http://localhost:5020/swagger

---

**Cách sử dụng:**
1. Tick ✅ vào mỗi item sau khi test
2. Ghi chú lỗi cần fix
3. Test lại sau khi fix
