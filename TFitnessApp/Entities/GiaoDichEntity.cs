using System;

namespace TFitnessApp.Entities
{
    // Cần phải có lớp này để DataGrid và Repository hoạt động.
    public class GiaoDich
    {
        // Các cột Mã (string)
        public string MaGD { get; set; }
        public string MaHV { get; set; }
        public string MaGoi { get; set; }
        public string MaTK { get; set; }

        // Các cột số (decimal)
        public decimal TongTien { get; set; }
        public decimal DaThanhToan { get; set; }
        public decimal SoTienNo { get; set; }

        // Ngày giao dịch (DateTime)
        public DateTime NgayGD { get; set; }

        // Trạng thái (string)
        public string TrangThai { get; set; }

        // Thuộc tính cần thiết cho CheckBox trong DataGrid
        public bool IsSelected { get; set; }
    }
}