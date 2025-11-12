-- SINH VIÊN KHOA 2 (K68) - 30 SINH VIÊN
-- Site 4: Sinh viên K2

-- Tạo bảng sinh viên K2
CREATE TABLE IF NOT EXISTS sinhvien_k2 (
    mssv VARCHAR(10) PRIMARY KEY,
    hoten VARCHAR(100) NOT NULL,
    phai VARCHAR(10) NOT NULL,
    ngaysinh DATE NOT NULL,
    mslop VARCHAR(10) NOT NULL,
    hocbong DECIMAL(10, 2) DEFAULT 0
);

INSERT INTO sinhvien_k2 (mssv, hoten, phai, ngaysinh, mslop, hocbong) VALUES
-- Data Science (L11) - 6 sinh viên
('SV101', 'Nguyễn Thị Mai', 'Nữ', '2006-01-12', 'L11', 2800000),
('SV102', 'Trần Văn Nam', 'Nam', '2006-02-18', 'L11', 2400000),
('SV103', 'Lê Thị Oanh', 'Nữ', '2006-03-25', 'L11', 3000000),
('SV104', 'Phạm Văn Phúc', 'Nam', '2006-04-08', 'L11', 2100000),
('SV105', 'Hoàng Thị Quỳnh', 'Nữ', '2006-05-15', 'L11', 2600000),
('SV106', 'Vũ Văn Sơn', 'Nam', '2006-06-22', 'L11', 2200000),

-- Machine Learning (L12) - 6 sinh viên
('SV107', 'Đặng Thị Tâm', 'Nữ', '2006-07-10', 'L12', 2900000),
('SV108', 'Bùi Văn Uy', 'Nam', '2006-08-16', 'L12', 2500000),
('SV109', 'Đinh Thị Vân', 'Nữ', '2006-09-20', 'L12', 3100000),
('SV110', 'Ngô Văn Xuân', 'Nam', '2006-10-05', 'L12', 2300000),
('SV111', 'Trương Thị Yến', 'Nữ', '2006-11-12', 'L12', 2700000),
('SV112', 'Phan Văn Anh', 'Nam', '2006-12-18', 'L12', 2400000),

-- Deep Learning (L13) - 6 sinh viên
('SV113', 'Lý Thị Bích', 'Nữ', '2006-01-25', 'L13', 3200000),
('SV114', 'Võ Văn Cường', 'Nam', '2006-02-08', 'L13', 2600000),
('SV115', 'Đỗ Thị Duyên', 'Nữ', '2006-03-15', 'L13', 2900000),
('SV116', 'Mai Văn Đạt', 'Nam', '2006-04-22', 'L13', 2500000),
('SV117', 'Chu Thị Hà', 'Nữ', '2006-05-08', 'L13', 2800000),
('SV118', 'Hồ Văn Hùng', 'Nam', '2006-06-14', 'L13', 2400000),

-- Computer Vision (L14) - 6 sinh viên
('SV119', 'Dương Thị Linh', 'Nữ', '2006-07-20', 'L14', 3000000),
('SV120', 'Lưu Văn Minh', 'Nam', '2006-08-25', 'L14', 2700000),
('SV121', 'Tạ Thị Ngọc', 'Nữ', '2006-09-30', 'L14', 2900000),
('SV122', 'Cao Văn Phong', 'Nam', '2006-10-15', 'L14', 2500000),
('SV123', 'Trịnh Thị Quế', 'Nữ', '2006-11-20', 'L14', 2800000),
('SV124', 'Hà Văn Sáng', 'Nam', '2006-12-28', 'L14', 2600000),

-- NLP (L15) - 6 sinh viên
('SV125', 'Lâm Thị Thủy', 'Nữ', '2006-01-05', 'L15', 3100000),
('SV126', 'Nguyễn Văn Tuấn', 'Nam', '2006-02-10', 'L15', 2800000),
('SV127', 'Trần Thị Uyên', 'Nữ', '2006-03-18', 'L15', 3000000),
('SV128', 'Lê Văn Việt', 'Nam', '2006-04-24', 'L15', 2600000),
('SV129', 'Phạm Thị Xuân', 'Nữ', '2006-05-30', 'L15', 2900000),
('SV130', 'Hoàng Văn Yên', 'Nam', '2006-06-12', 'L15', 2700000);

SELECT 'SINHVIEN_K2: Đã tạo ' || COUNT(*) || ' sinh viên' as message FROM sinhvien_k2;
