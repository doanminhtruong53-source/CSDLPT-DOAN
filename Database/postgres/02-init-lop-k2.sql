-- DỮ LIỆU LỚP HỌC K68 - TÊN TRỰC QUAN
-- Site 2: Lớp Khoa 2 (K68)

CREATE TABLE IF NOT EXISTS lop_k2 (
    mslop VARCHAR(10) PRIMARY KEY,
    tenlop VARCHAR(100) NOT NULL,
    khoa VARCHAR(10) NOT NULL
);

-- 10 LỚP CHÍNH với tên MÔN HỌC thực tế
INSERT INTO lop_k2 (mslop, tenlop, khoa) VALUES
-- Data Science & AI
('L11', 'Khoa học dữ liệu (Data Science)', 'K2'),
('L12', 'Machine Learning cơ bản', 'K2'),
('L13', 'Deep Learning & Neural Networks', 'K2'),
('L14', 'Computer Vision (OpenCV)', 'K2'),
('L15', 'Natural Language Processing', 'K2'),

-- Công nghệ mới
('L16', 'Blockchain & Cryptocurrency', 'K2'),
('L17', 'Internet of Things (IoT)', 'K2'),
('L18', 'UI/UX Design & Figma', 'K2'),
('L19', 'Digital Marketing & SEO', 'K2'),
('L20', 'Quản lý dự án Agile/Scrum', 'K2');

SELECT 'LOP_K2: Đã tạo ' || COUNT(*) || ' lớp học' as message FROM lop_k2;
