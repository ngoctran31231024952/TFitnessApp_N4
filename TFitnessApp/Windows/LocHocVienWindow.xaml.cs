using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TFitnessApp;

namespace TFitnessApp.Windows
{
    /// <summary>
    /// Cửa sổ Lọc Nâng cao dành cho Học viên
    /// </summary>
    public partial class LocHocVienWindow : Window
    {
        #region Biến Nội bộ
        private HocVienRepository _repository;
        public HocVienFilterData FilterData { get; private set; } // Dữ liệu trả về sau khi lọc
        public bool IsApply { get; private set; } = false;        // Cờ xác nhận người dùng nhấn Áp dụng
        // Danh sách tất cả PT để hỗ trợ tìm kiếm trong ComboBox
        private List<ComboBoxItemData> _allPTs;
        #endregion

        #region Khởi tạo
        public LocHocVienWindow()
        {
            InitializeComponent();
            _repository = new HocVienRepository();
            TaiDuLieuLoc(); // Load dữ liệu vào các ComboBox
        }
        // Load dữ liệu từ DB vào các ComboBox (Gói tập, Chi nhánh, PT)
        private void TaiDuLieuLoc()
        {
            // 1. Load Gói tập
            var goiTaps = _repository.LayDanhSachComboBox("GoiTap", "MaGoi", "TenGoi");
            goiTaps.Insert(0, new ComboBoxItemData { ID = "Tất cả", Name = "-- Tất cả --" });
            cmbGoiTap.ItemsSource = goiTaps;
            cmbGoiTap.SelectedIndex = 0;

            // 2. Load Chi nhánh
            var chiNhanhs = _repository.LayDanhSachComboBox("ChiNhanh", "MaCN", "TenCN");
            chiNhanhs.Insert(0, new ComboBoxItemData { ID = "Tất cả", Name = "-- Tất cả --" });
            cmbChiNhanh.ItemsSource = chiNhanhs;
            cmbChiNhanh.SelectedIndex = 0;

            // 3. Load Danh sách PT (Huấn luyện viên)
            _allPTs = _repository.LayDanhSachComboBox("PT", "MaPT", "HoTen");
            _allPTs.Insert(0, new ComboBoxItemData { ID = "Tất cả", Name = "-- Tất cả --" });
            cmbPT.ItemsSource = _allPTs;
        }
        #endregion

        #region Xử lý Sự kiện (Event Handlers)

        // Tìm kiếm trong ComboBox PT khi gõ phím
        private void cmbPT_KeyUp(object sender, KeyEventArgs e)
        {
            var combo = sender as ComboBox;
            // Bỏ qua các phím điều hướng để không làm phiền người dùng chọn
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter) return;

            string searchText = combo.Text.ToLower();

            // Lọc danh sách PT trong bộ nhớ (_allPTs) theo tên
            var filtered = _allPTs.Where(pt => pt.Name.ToLower().Contains(searchText)).ToList();

            combo.ItemsSource = filtered;
            combo.IsDropDownOpen = true; // Tự động mở danh sách gợi ý
        }

        // Nút Áp dụng Lọc
        private void BtnApDung_Click(object sender, RoutedEventArgs e)
        {
            FilterData = new HocVienFilterData();

            // 1. Lấy điều kiện Giới tính
            if (rbNam.IsChecked == true) FilterData.GioiTinh = "Nam";
            else if (rbNu.IsChecked == true) FilterData.GioiTinh = "Nữ";
            else FilterData.GioiTinh = "Tất cả";

            // 2. Lấy điều kiện Ngày tháng (Ngày sinh & Ngày tham gia)
            FilterData.NamSinhTu = dpNamSinhTu.SelectedDate;
            FilterData.NamSinhDen = dpNamSinhDen.SelectedDate;
            FilterData.NgayThamGiaTu = dpThamGiaTu.SelectedDate;
            FilterData.NgayThamGiaDen = dpThamGiaDen.SelectedDate;

            // 3. Lấy giá trị từ các ComboBox
            FilterData.MaGoi = cmbGoiTap.SelectedValue?.ToString();
            FilterData.MaCN = cmbChiNhanh.SelectedValue?.ToString();

            // Xử lý riêng cho ComboBox PT (vì có chức năng tìm kiếm text)
            FilterData.MaPT = cmbPT.SelectedValue?.ToString();

            // Nếu người dùng gõ tên PT nhưng chưa chọn item nào -> Tự tìm item khớp tên
            if (string.IsNullOrEmpty(FilterData.MaPT) && !string.IsNullOrEmpty(cmbPT.Text) && cmbPT.Text != "-- Tất cả --")
            {
                var match = _allPTs.FirstOrDefault(p => p.Name.Equals(cmbPT.Text, StringComparison.OrdinalIgnoreCase));
                if (match != null) FilterData.MaPT = match.ID;
            }

            IsApply = true; // Đánh dấu đã áp dụng thành công
            this.Close();
        }

        // Nút Đặt lại (Reset Form)
        private void BtnDatLai_Click(object sender, RoutedEventArgs e)
        {
            rbTatCa.IsChecked = true;

            dpNamSinhTu.SelectedDate = null;
            dpNamSinhDen.SelectedDate = null;
            dpThamGiaTu.SelectedDate = null;
            dpThamGiaDen.SelectedDate = null;

            cmbGoiTap.SelectedIndex = 0;
            cmbChiNhanh.SelectedIndex = 0;

            // Reset ComboBox PT về danh sách đầy đủ
            cmbPT.Text = "";
            cmbPT.ItemsSource = _allPTs;
            cmbPT.SelectedIndex = -1;
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) { this.Close(); }
        #endregion
    }
}