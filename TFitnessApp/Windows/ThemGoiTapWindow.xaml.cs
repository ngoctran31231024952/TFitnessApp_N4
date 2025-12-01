using System;
using System.Windows;
using TFitnessApp;

namespace TFitnessApp.Windows
{
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
                txtMaGoi.IsReadOnly = true; // Không cho sửa mã
                txtMaGoi.Background = System.Windows.Media.Brushes.LightGray;

                txtTenGoi.Text = gt.TenGoi;
                txtThoiHan.Text = gt.ThoiHan.ToString();
                txtGia.Text = gt.GiaNiemYet.ToString();
                txtSoBuoiPT.Text = gt.SoBuoiPT.ToString();

                // Set Combobox values...
                cmbDichVu.Text = gt.DichVuDacBiet;
                cmbTrangThai.Text = gt.TrangThai;
            }
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            string maGoi = txtMaGoi.Text.Trim();
            string tenGoi = txtTenGoi.Text.Trim();

            if (string.IsNullOrEmpty(maGoi) || string.IsNullOrEmpty(tenGoi))
            {
                MessageBox.Show("Vui lòng nhập Mã gói và Tên gói!"); return;
            }

            if (!_isEditMode && _repository.CheckMaGoiExists(maGoi))
            {
                MessageBox.Show("Mã gói đã tồn tại!"); return;
            }

            // TryParse an toàn
            int.TryParse(txtThoiHan.Text, out int thoiHan);
            double.TryParse(txtGia.Text, out double gia);
            int.TryParse(txtSoBuoiPT.Text, out int soBuoi);

            GoiTap gt = new GoiTap
            {
                MaGoi = maGoi,
                TenGoi = tenGoi,
                ThoiHan = thoiHan,
                GiaNiemYet = gia,
                SoBuoiPT = soBuoi,
                DichVuDacBiet = cmbDichVu.Text,
                TrangThai = cmbTrangThai.Text
            };

            bool kq = _isEditMode ? _repository.UpdateGoiTap(gt) : _repository.AddGoiTap(gt);

            if (kq)
            {
                MessageBox.Show("Thành công!");
                IsSuccess = true;
                this.Close();
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) { this.Close(); }
    }
}