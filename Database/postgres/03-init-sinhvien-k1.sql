-- SINH VIÊN KHOA 1 (K67) - 30 SINH VIÊN
-- Site 3: Sinh viên K1

-- Tạo bảng sinh viên K1
CREATE TABLE IF NOT EXISTS sinhvien_k1 (
    mssv VARCHAR(10) PRIMARY KEY,
    hoten VARCHAR(100) NOT NULL,
    phai VARCHAR(10) NOT NULL,
    ngaysinh DATE NOT NULL,
    mslop VARCHAR(10) NOT NULL,
    hocbong DECIMAL(10, 2) DEFAULT 0
);

INSERT INTO sinhvien_k1 (mssv, hoten, phai, ngaysinh, mslop, hocbong) VALUES
-- Lập trình Web (L01) - 6 sinh viên
('SV001', 'Nguyễn Văn Anh', 'Nam', '2005-01-15', 'L01', 2000000),
('SV002', 'Trần Thị Bình', 'Nữ', '2005-02-20', 'L01', 2500000),
('SV003', 'Lê Minh Cường', 'Nam', '2005-03-10', 'L01', 1500000),
('SV004', 'Phạm Thị Dung', 'Nữ', '2005-04-05', 'L01', 3000000),
('SV005', 'Hoàng Văn Em', 'Nam', '2005-05-12', 'L01', 1800000),
('SV006', 'Vũ Thị Hoa', 'Nữ', '2005-06-18', 'L01', 2200000),

-- Lập trình Mobile (L02) - 6 sinh viên
('SV007', 'Đặng Văn Giang', 'Nam', '2005-07-22', 'L02', 2400000),
('SV008', 'Bùi Thị Hằng', 'Nữ', '2005-08-08', 'L02', 2800000),
('SV009', 'Đinh Minh Khang', 'Nam', '2005-09-14', 'L02', 1600000),
('SV010', 'Ngô Thị Lan', 'Nữ', '2005-10-25', 'L02', 2100000),
('SV011', 'Trương Văn Minh', 'Nam', '2005-11-30', 'L02', 1900000),
('SV012', 'Phan Thị Nga', 'Nữ', '2005-12-05', 'L02', 2600000),

-- Python & AI (L03) - 6 sinh viên
('SV013', 'Lý Văn Oanh', 'Nam', '2005-01-20', 'L03', 2300000),
('SV014', 'Võ Thị Phương', 'Nữ', '2005-02-15', 'L03', 2700000),
('SV015', 'Đỗ Minh Quân', 'Nam', '2005-03-28', 'L03', 1700000),
('SV016', 'Mai Thị Thảo', 'Nữ', '2005-04-10', 'L03', 2400000),
('SV017', 'Chu Văn Tú', 'Nam', '2005-05-22', 'L03', 2000000),
('SV018', 'Hồ Thị Uyên', 'Nữ', '2005-06-30', 'L03', 2500000),

-- Full-stack (L04) - 6 sinh viên
('SV019', 'Dương Văn Vũ', 'Nam', '2005-07-15', 'L04', 2200000),
('SV020', 'Lưu Thị Xuân', 'Nữ', '2005-08-20', 'L04', 2900000),
('SV021', 'Tạ Minh Yên', 'Nam', '2005-09-05', 'L04', 1800000),
('SV022', 'Cao Thị Ánh', 'Nữ', '2005-10-12', 'L04', 2300000),
('SV023', 'Trịnh Văn Bảo', 'Nam', '2005-11-18', 'L04', 2100000),
('SV024', 'Hà Thị Chi', 'Nữ', '2005-12-22', 'L04', 2700000),

-- Game Development (L05) - 6 sinh viên
('SV025', 'Lâm Văn Đức', 'Nam', '2005-01-08', 'L05', 2600000),
('SV026', 'Nguyễn Thị Hương', 'Nữ', '2005-02-14', 'L05', 3000000),
('SV027', 'Trần Văn Khánh', 'Nam', '2005-03-20', 'L05', 1900000),
('SV028', 'Lê Thị Linh', 'Nữ', '2005-04-25', 'L05', 2400000),
('SV029', 'Phạm Văn Nam', 'Nam', '2005-05-30', 'L05', 2200000),
('SV030', 'Hoàng Thị Oanh', 'Nữ', '2005-06-08', 'L05', 2800000);

SELECT 'SINHVIEN_K1: Đã tạo ' || COUNT(*) || ' sinh viên' as message FROM sinhvien_k1;
