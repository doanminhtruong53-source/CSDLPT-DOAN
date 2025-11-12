#!/bin/bash
# ═══════════════════════════════════════════════════════
# SCRIPT TEST API & FRONTEND
# Kiểm tra tất cả endpoints và logic nghiệp vụ
# ═══════════════════════════════════════════════════════

API_URL="http://localhost:5020/api"
FRONTEND_URL="http://localhost:3000"

echo "════════════════════════════════════════════════════════════"
echo "  🧪 BẮT ĐẦU TEST HỆ THỐNG"
echo "════════════════════════════════════════════════════════════"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
TOTAL=0
PASSED=0
FAILED=0

test_endpoint() {
    local method=$1
    local endpoint=$2
    local description=$3
    local data=$4
    
    TOTAL=$((TOTAL + 1))
    echo -n "[$TOTAL] Testing: $description... "
    
    if [ -z "$data" ]; then
        response=$(curl -s -w "\n%{http_code}" -X $method "$API_URL$endpoint")
    else
        response=$(curl -s -w "\n%{http_code}" -X $method "$API_URL$endpoint" \
            -H "Content-Type: application/json" \
            -d "$data")
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n-1)
    
    if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
        echo -e "${GREEN}✓ PASS${NC} (HTTP $http_code)"
        PASSED=$((PASSED + 1))
        return 0
    else
        echo -e "${RED}✗ FAIL${NC} (HTTP $http_code)"
        echo "   Response: $body"
        FAILED=$((FAILED + 1))
        return 1
    fi
}

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  1. ADMIN & HEALTH CHECK"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
test_endpoint "GET" "/admin/overview" "Tổng quan hệ thống"
test_endpoint "GET" "/admin/sites/health" "Health check 7 sites"
echo ""

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  2. QUẢN LÝ LỚP HỌC"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
test_endpoint "GET" "/classes" "Lấy danh sách lớp"
test_endpoint "GET" "/classes?khoa=K1" "Filter lớp theo khoa K1"
test_endpoint "GET" "/classes/L01" "Chi tiết lớp L01"
test_endpoint "GET" "/classes/L11" "Chi tiết lớp L11"
echo ""

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  3. QUẢN LÝ SINH VIÊN"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
test_endpoint "GET" "/students" "Lấy danh sách sinh viên"
test_endpoint "GET" "/students/SV001" "Chi tiết sinh viên SV001"
test_endpoint "GET" "/students/SV101" "Chi tiết sinh viên SV101"
test_endpoint "GET" "/students/search?name=Nguyễn" "Tìm kiếm theo tên"
echo ""

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  4. ĐĂNG KÝ & ĐIỂM SỐ"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
test_endpoint "GET" "/registrations" "Lấy danh sách đăng ký"
test_endpoint "GET" "/registrations/SV001" "Đăng ký của SV001"
test_endpoint "GET" "/registrations/SV101" "Đăng ký của SV101"
echo ""

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  5. KIỂM TRA DỮ LIỆU"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Lấy dữ liệu từ API
echo "Kiểm tra tên lớp trực quan..."
classes_response=$(curl -s "$API_URL/classes")
echo "$classes_response" | grep -q "Lập trình Web" && echo -e "${GREEN}✓${NC} Tên lớp trực quan: OK" || echo -e "${RED}✗${NC} Tên lớp chưa cập nhật"

echo "Kiểm tra số lượng lớp..."
class_count=$(echo "$classes_response" | grep -o '"mslop"' | wc -l | xargs)
echo "   Tổng số lớp: $class_count (expect: 20)"

echo "Kiểm tra sinh viên..."
students_response=$(curl -s "$API_URL/students")
student_count=$(echo "$students_response" | grep -o '"mssv"' | wc -l | xargs)
echo "   Tổng số sinh viên: $student_count (expect: 60)"

echo "Kiểm tra đăng ký..."
reg_response=$(curl -s "$API_URL/registrations")
reg_count=$(echo "$reg_response" | grep -o '"mssv"' | wc -l | xargs)
echo "   Tổng số đăng ký: $reg_count (expect: 180)"

echo ""
echo "════════════════════════════════════════════════════════════"
echo "  📊 KẾT QUẢ TEST"
echo "════════════════════════════════════════════════════════════"
echo -e "Tổng số test:     $TOTAL"
echo -e "${GREEN}✓ Passed:        $PASSED${NC}"
echo -e "${RED}✗ Failed:        $FAILED${NC}"

if [ $FAILED -eq 0 ]; then
    echo ""
    echo -e "${GREEN}🎉 TẤT CẢ TEST ĐỀU PASS!${NC}"
    echo ""
    echo "Tiếp theo kiểm tra Frontend:"
    echo "1. Mở $FRONTEND_URL"
    echo "2. Kiểm tra các trang:"
    echo "   - Dashboard: Thống kê tổng quan"
    echo "   - Classes: Tên lớp trực quan"
    echo "   - Students: Danh sách sinh viên"
    echo "   - Registrations: Đăng ký môn học"
    echo "   - Reports: Báo cáo thống kê"
else
    echo ""
    echo -e "${RED}⚠️  CÓ $FAILED TEST FAILED!${NC}"
    echo "Vui lòng kiểm tra log bên trên"
fi

echo "════════════════════════════════════════════════════════════"
