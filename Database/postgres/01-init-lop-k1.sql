-- DỮ LIỆU LỚP HỌC K67 - TÊN TRỰC QUAN
-- Site 1: Lớp Khoa 1 (K67)

CREATE TABLE IF NOT EXISTS lop_k1 (
    mslop VARCHAR(10) PRIMARY KEY,
    tenlop VARCHAR(100) NOT NULL,
    khoa VARCHAR(10) NOT NULL
);

-- 10 LỚP CHÍNH với tên MÔN HỌC thực tế
INSERT INTO lop_k1 (mslop, tenlop, khoa) VALUES
-- Lập trình & Phát triển phần mềm
('L01', 'Lập trình Web (ReactJS & Node.js)', 'K1'),
('L02', 'Lập trình Mobile (React Native)', 'K1'),
('L03', 'Lập trình Python & AI cơ bản', 'K1'),
('L04', 'Phát triển Full-stack (MERN)', 'K1'),
('L05', 'Lập trình Game (Unity & C#)', 'K1'),

-- Cơ sở dữ liệu & Hệ thống
('L06', 'Cơ sở dữ liệu phân tán', 'K1'),
('L07', 'Quản trị hệ thống Linux', 'K1'),
('L08', 'An ninh mạng & Bảo mật', 'K1'),
('L09', 'DevOps & CI/CD', 'K1'),
('L10', 'Cloud Computing (AWS)', 'K1');

SELECT 'LOP_K1: Đã tạo ' || COUNT(*) || ' lớp học' as message FROM lop_k1;
