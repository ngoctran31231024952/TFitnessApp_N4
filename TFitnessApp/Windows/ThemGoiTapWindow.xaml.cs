using System;
using System.Windows;
using TFitnessApp;
using System.Text.RegularExpressions;

namespace TFitnessApp.Windows
{
    #region Khởi tạo
    public partial class ThemGoiTapWindow : Window
    {
        private GoiTapRepository _repository;
        public bool IsSuccess { get; private set; } = false;
        private bool _isEditMode = false;

        public ThemGoiTapWindow(GoiTap gt = null)
        {
            InitializeComponent();
            _repository = new GoiTapRepository();

            if (gt != null)
            {
                _isEditMode = true;
                txtHeaderTitle.Text = "SỬA GÓI TẬP";
                txtBtnAction.Text = "Lưu";
                txtMaGoi.Text = gt.MaGoi;
                txtMaGoi.IsReadOnly = true;
                txtMaGoi.Background = System.Windows.Media.Brushes.LightGray;

                txtTenGoi.Text = gt.TenGoi;
                txtThoiHan.Text = gt.ThoiHan.ToString();
                txtGia.Text = gt.GiaNiemYet.ToString();
                txtSoBuoiPT.Text = gt.SoBuoiPT.ToString();

                cmbDichVu.Text = gt.DichVuDacBiet;
                cmbTrangThai.Text = gt.TrangThai;
            }
        }
        #endregion

        #region Các phương thức hỗ trợ
        // Helper kiểm tra số
        private bool LaSoNguyen(string text) { return Regex.IsMatch(text, @"^\d+$"); }
        // IsDecimal -> LaSoThuc
        private bool LaSoThuc(string text) { return double.TryParse(text, out _); }
        #endregion

        #region Xử lý sự kiện Lưu
        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            string maGoi = txtMaGoi.Text.Trim();
            string tenGoi = txtTenGoi.Text.Trim();
            string strThoiHan = txtThoiHan.Text.Trim();
            string strGia = txtGia.Text.Trim();
            string strSoBuoi = txtSoBuoiPT.Text.Trim();

            // --- VALIDATION ---
            if (string.IsNullOrEmpty(maGoi))
            {
                MessageBox.Show("Vui lòng nhập Mã gói!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMaGoi.Focus(); return;
            }
            if (string.IsNullOrEmpty(tenGoi))
            {
                MessageBox.Show("Vui lòng nhập Tên gói!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTenGoi.Focus(); return;
            }

            // Kiểm tra Thời hạn (phải là số nguyên > 0)
            if (!LaSoNguyen(strThoiHan) || int.Parse(strThoiHan) <= 0)
            {
                MessageBox.Show("Thời hạn phải là số nguyên dương (tháng)!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtThoiHan.Focus(); return;
            }

            // Kiểm tra Giá (phải là số thực >= 0)
            if (!LaSoThuc(strGia) || double.Parse(strGia) < 0)
            {
                MessageBox.Show("Giá niêm yết phải là số hợp lệ và không âm!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtGia.Focus(); return;
            }

            // Kiểm tra Số buổi PT (phải là số nguyên >= 0)
            if (!LaSoNguyen(strSoBuoi) || int.Parse(strSoBuoi) < 0)
            {
                MessageBox.Show("Số buổi PT phải là số nguyên không âm!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSoBuoiPT.Focus(); return;
            }

            // Kiểm tra trùng mã (Gọi hàm KiemTraMaGoiTonTai thay cho CheckMaGoiExists)
            if (!_isEditMode && _repository.KiemTraMaGoiTonTai(maGoi))
            {
                MessageBox.Show($"Mã gói {maGoi} đã tồn tại!", "Trùng mã", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // ------------------

            GoiTap gt = new GoiTap
            {
                MaGoi = maGoi,
                TenGoi = tenGoi,
                ThoiHan = int.Parse(strThoiHan),
                GiaNiemYet = double.Parse(strGia),
                SoBuoiPT = int.Parse(strSoBuoi),
                DichVuDacBiet = cmbDichVu.Text,
                TrangThai = cmbTrangThai.Text
            };

            // Gọi CapNhatGoiTap hoặc ThemGoiTap
            bool kq = _isEditMode ? _repository.CapNhatGoiTap(gt) : _repository.ThemGoiTap(gt);

            if (kq)
            {
                MessageBox.Show(_isEditMode ? "Cập nhật thành công!" : "Thêm gói tập thành công!", "Thông báo");
                IsSuccess = true;
                this.Close();
            }
        }
        #endregion
        private void BtnHuy_Click(object sender, RoutedEventArgs e) { this.Close(); }
    }
}