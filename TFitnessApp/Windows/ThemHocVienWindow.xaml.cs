using System;
using System.Windows;
using TFitnessApp;

namespace TFitnessApp.Windows
{
    public partial class ThemHocVienWindow : Window
    {
        private HocVienRepository _repository;

        // Property để báo cho cửa sổ cha biết là đã thêm thành công
        public bool IsSuccess { get; private set; } = false;

        public ThemHocVienWindow()
        {
            InitializeComponent();
            _repository = new HocVienRepository();
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lấy dữ liệu từ giao diện
            string maHV = txtMaHV.Text.Trim();
            string hoTen = txtHoTen.Text.Trim();

            // 2. Validate (Kiểm tra dữ liệu)
            if (string.IsNullOrEmpty(maHV) || string.IsNullOrEmpty(hoTen))
            {
                MessageBox.Show("Vui lòng nhập Mã HV và Họ tên!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Kiểm tra trùng mã (Logic này nằm trong Repository)
            if (_repository.CheckMaHVExists(maHV))
            {
                MessageBox.Show($"Mã học viên {maHV} đã tồn tại!", "Trùng mã", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 4. Tạo đối tượng HocVien mới
            HocVien newItem = new HocVien
            {
                MaHV = maHV,
                HoTen = hoTen,
                NgaySinh = dpNgaySinh.SelectedDate,
                // Lấy giá trị từ ComboBox an toàn hơn
                GioiTinh = (cmbGioiTinh.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Nam",
                Email = txtEmail.Text.Trim(),
                SDT = txtSDT.Text.Trim(),
                DiaChi = ""
            };

            // 5. Gọi Repository để lưu vào CSDL
            if (_repository.AddHocVien(newItem))
            {
                MessageBox.Show("Thêm học viên thành công!", "Thông báo");
                IsSuccess = true; // Đánh dấu thành công
                this.Close();     // Đóng cửa sổ
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}