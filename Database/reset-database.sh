#!/bin/bash
# ═══════════════════════════════════════════════════════
# SCRIPT RESET & LOAD DỮ LIỆU DATABASE
# Xóa toàn bộ dữ liệu cũ và load dữ liệu mới
# ═══════════════════════════════════════════════════════

cd "$(dirname "$0")"

echo "════════════════════════════════════════════════════════════"
echo "  🗑️  ĐANG XÓA DỮ LIỆU CŨ..."
echo "════════════════════════════════════════════════════════════"

# Xóa dữ liệu trong tất cả các site (giữ nguyên container)
docker exec postgres-lop-khoa-k1 psql -U admin -d LopK1DB -c "TRUNCATE TABLE lop_k1 CASCADE;" 2>/dev/null
docker exec postgres-lop-khoa-k2 psql -U admin -d LopK2DB -c "TRUNCATE TABLE lop_k2 CASCADE;" 2>/dev/null
docker exec postgres-sinhvien-khoa-k1 psql -U admin -d SinhVienK1DB -c "TRUNCATE TABLE sinhvien_k1 CASCADE;" 2>/dev/null
docker exec postgres-sinhvien-khoa-k2 psql -U admin -d SinhVienK2DB -c "TRUNCATE TABLE sinhvien_k2 CASCADE;" 2>/dev/null
docker exec postgres-dangky-diem1 psql -U admin -d DangKyDiem1DB -c "TRUNCATE TABLE dangky_diem1 CASCADE;" 2>/dev/null
docker exec postgres-dangky-diem23-khoa-k1 psql -U admin -d DangKyDiem23K1DB -c "TRUNCATE TABLE dangky_diem23_k1 CASCADE;" 2>/dev/null
docker exec postgres-dangky-diem23-khoa-k2 psql -U admin -d DangKyDiem23K2DB -c "TRUNCATE TABLE dangky_diem23_k2 CASCADE;" 2>/dev/null

echo "✅ Đã xóa toàn bộ dữ liệu cũ"
echo ""
echo "════════════════════════════════════════════════════════════"
echo "  📝 ĐANG LOAD DỮ LIỆU MỚI..."
echo "════════════════════════════════════════════════════════════"

# Load dữ liệu mới từ các file SQL
cat postgres/01-init-lop-k1.sql | docker exec -i postgres-lop-khoa-k1 psql -U admin -d LopK1DB 2>/dev/null
echo "✓ Site 1: Lớp K1 (Lập trình Web, Mobile, Python...)"

cat postgres/02-init-lop-k2.sql | docker exec -i postgres-lop-khoa-k2 psql -U admin -d LopK2DB 2>/dev/null
echo "✓ Site 2: Lớp K2 (Data Science, ML, AI...)"

cat postgres/03-init-sinhvien-k1.sql | docker exec -i postgres-sinhvien-khoa-k1 psql -U admin -d SinhVienK1DB 2>/dev/null
echo "✓ Site 3: Sinh viên K1 (30 sinh viên)"

cat postgres/04-init-sinhvien-k2.sql | docker exec -i postgres-sinhvien-khoa-k2 psql -U admin -d SinhVienK2DB 2>/dev/null
echo "✓ Site 4: Sinh viên K2 (30 sinh viên)"

cat postgres/05-init-dangky-diem1.sql | docker exec -i postgres-dangky-diem1 psql -U admin -d DangKyDiem1DB 2>/dev/null
echo "✓ Site 5: Đăng ký - Điểm TX (180 đăng ký)"

cat postgres/06-init-dangky-diem23-k1.sql | docker exec -i postgres-dangky-diem23-khoa-k1 psql -U admin -d DangKyDiem23K1DB 2>/dev/null
echo "✓ Site 6: Đăng ký - Điểm GK & CK K1 (90 đăng ký)"

cat postgres/07-init-dangky-diem23-k2.sql | docker exec -i postgres-dangky-diem23-khoa-k2 psql -U admin -d DangKyDiem23K2DB 2>/dev/null
echo "✓ Site 7: Đăng ký - Điểm GK & CK K2 (90 đăng ký)"

echo ""
echo "════════════════════════════════════════════════════════════"
echo "  ✅ HOÀN THÀNH!"
echo "════════════════════════════════════════════════════════════"
echo ""
echo "📊 THỐNG KÊ DỮ LIỆU:"
echo "────────────────────────────────────────────────────────────"
echo "  Lớp K1: $(docker exec postgres-lop-khoa-k1 psql -U admin -d LopK1DB -t -c 'SELECT COUNT(*) FROM lop_k1;' 2>/dev/null | xargs) lớp"
echo "  Lớp K2: $(docker exec postgres-lop-khoa-k2 psql -U admin -d LopK2DB -t -c 'SELECT COUNT(*) FROM lop_k2;' 2>/dev/null | xargs) lớp"
echo "  Sinh viên K1: $(docker exec postgres-sinhvien-khoa-k1 psql -U admin -d SinhVienK1DB -t -c 'SELECT COUNT(*) FROM sinhvien_k1;' 2>/dev/null | xargs) sv"
echo "  Sinh viên K2: $(docker exec postgres-sinhvien-khoa-k2 psql -U admin -d SinhVienK2DB -t -c 'SELECT COUNT(*) FROM sinhvien_k2;' 2>/dev/null | xargs) sv"
echo "  Tổng đăng ký: $(docker exec postgres-dangky-diem1 psql -U admin -d DangKyDiem1DB -t -c 'SELECT COUNT(*) FROM dangky_diem1;' 2>/dev/null | xargs)"
echo ""
echo "🎯 Truy cập: http://localhost:3000"
echo "════════════════════════════════════════════════════════════"
