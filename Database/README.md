# 📁 Database - Hệ thống CSDL Phân tán

## 📂 Cấu trúc thư mục

```
Database/
├── postgres/                          # Các file SQL khởi tạo dữ liệu
│   ├── 01-init-lop-k1.sql            # Lớp học K67 (10 lớp)
│   ├── 02-init-lop-k2.sql            # Lớp học K68 (10 lớp)
│   ├── 03-init-sinhvien-k1.sql       # Sinh viên K1 (30 sv)
│   ├── 04-init-sinhvien-k2.sql       # Sinh viên K2 (30 sv)
│   ├── 05-init-dangky-diem1.sql      # Đăng ký + Điểm TX (180)
│   ├── 06-init-dangky-diem23-k1.sql  # Đăng ký + Điểm GK&CK K1 (90)
│   └── 07-init-dangky-diem23-k2.sql  # Đăng ký + Điểm GK&CK K2 (90)
└── reset-database.sh                  # Script reset & load dữ liệu
```

## 🗄️ Dữ liệu hiện tại

### **Lớp học K1** (10 lớp - Khóa K67):
- 🌐 Lập trình Web (ReactJS & Node.js)
- 📱 Lập trình Mobile (React Native)
- 🐍 Lập trình Python & AI cơ bản
- 💻 Phát triển Full-stack (MERN)
- 🎮 Lập trình Game (Unity & C#)
- 🗄️ Cơ sở dữ liệu phân tán
- 🐧 Quản trị hệ thống Linux
- 🔐 An ninh mạng & Bảo mật
- ⚙️ DevOps & CI/CD
- ☁️ Cloud Computing (AWS)

### **Lớp học K2** (10 lớp - Khóa K68):
- 📊 Khoa học dữ liệu (Data Science)
- 🧠 Machine Learning cơ bản
- 🔥 Deep Learning & Neural Networks
- 👁️ Computer Vision (OpenCV)
- 💬 Natural Language Processing
- ⛓️ Blockchain & Cryptocurrency
- 🌐 Internet of Things (IoT)
- 🎨 UI/UX Design & Figma
- 📈 Digital Marketing & SEO
- 📋 Quản lý dự án Agile/Scrum

### **Thống kê**:
- **60 sinh viên** (30 K1 + 30 K2)
- **180 đăng ký** môn học
- **7 sites** phân tán (PostgreSQL Docker)

## 🚀 Cách sử dụng

### Reset toàn bộ database:
```bash
cd Database
./reset-database.sh
```

Script này sẽ:
1. ✅ Xóa toàn bộ dữ liệu cũ (giữ nguyên container)
2. ✅ Load dữ liệu mới từ 7 file SQL
3. ✅ Hiển thị thống kê kết quả

### Kiểm tra dữ liệu thủ công:
```bash
# Site 1: Lớp K1
docker exec postgres-lop-khoa-k1 psql -U admin -d LopK1DB -c "SELECT * FROM lop_k1;"

# Site 3: Sinh viên K1
docker exec postgres-sinhvien-khoa-k1 psql -U admin -d SinhVienK1DB -c "SELECT * FROM sinhvien_k1;"

# Site 5: Đăng ký & Điểm 1
docker exec postgres-dangky-diem1 psql -U admin -d DangKyDiem1DB -c "SELECT * FROM dangky_diem1;"
```

## 📝 Chỉnh sửa dữ liệu

Để thay đổi dữ liệu:
1. Sửa file SQL tương ứng trong thư mục `postgres/`
2. Chạy `./reset-database.sh` để load lại

## 🔗 Links hữu ích

- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5020
- **API Docs**: http://localhost:5020/swagger

## 📌 Lưu ý

- Container Docker phải đang chạy
- Username: `admin` (không phải `postgres`)
- Database names: **Case-sensitive** (LopK1DB, không phải lopk1db)
