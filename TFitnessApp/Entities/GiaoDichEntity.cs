using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFitnessApp.Entities
{
    /// <summary>
    /// Lớp Entity đại diện cho một bản ghi Giao Dịch (Transaction) trong CSDL SQLite.
    /// </summary>
    public class GiaoDich
    {
        // Mã Giao Dịch (MaGD) - Khóa chính
        public string MaGD { get; set; }

        // Tổng Tiền (REAL trong SQLite)
        public decimal TongTien { get; set; }

        // Đã Thanh Toán (REAL trong SQLite)
        public decimal DaThanhToan { get; set; }

        // Số Tiền Nợ (REAL trong SQLite)
        public decimal SoTienNo { get; set; }

        // Phương Thức Thanh Toán (Mới)
        public string PhuongThuc { get; set; }

        // Ngày Giao Dịch (NgayGD TEXT trong SQLite)
        public DateTime NgayGD { get; set; }

        // Trạng Thái (TEXT)
        public string TrangThai { get; set; }

        // --- Khóa Ngoại ---

        // Mã Học Viên (MaHV) - Khóa ngoại tới HocVien
        public string MaHV { get; set; }

        // Mã Gói (MaGoi) - Khóa ngoại tới GoiTap
        public string MaGoi { get; set; }

        // Mã Tài Khoản (MaTK) - Khóa ngoại tới TaiKhoan (Mới)
        public string MaTK { get; set; }
    }
}
