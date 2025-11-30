using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TFitnessApp;

namespace TFitnessApp.Windows
{
    public partial class ThemHocVienWindow : Window
    {
        private HocVienRepository _repository;
        public bool IsSuccess { get; private set; } = false;
        private string _selectedImagePath = null;
        private bool _isEditMode = false; // Biến xác định chế độ

        // Constructor: tham số hv mặc định là null (Chế độ Thêm)
        // Nếu hv != null -> Chế độ Sửa
        public ThemHocVienWindow(HocVien hv = null)
        {
            InitializeComponent();
            _repository = new HocVienRepository();

            if (hv != null)
            {
                // CHẾ ĐỘ SỬA
                _isEditMode = true;
                txtHeaderTitle.Text = "CẬP NHẬT THÔNG TIN"; // Cần đặt x:Name="txtHeaderTitle" trong XAML hoặc dùng Title
                txtBtnAction.Text = "Lưu"; // Cần đặt x:Name cho TextBlock trong Button

                // Điền dữ liệu cũ
                txtMaHV.Text = hv.MaHV;
                txtHoTen.Text = hv.HoTen;
                txtEmail.Text = hv.Email;
                txtSDT.Text = hv.SDT;
                dpNgaySinh.SelectedDate = hv.NgaySinh;

                foreach (System.Windows.Controls.ComboBoxItem item in cmbGioiTinh.Items)
                {
                    if (item.Content.ToString() == hv.GioiTinh)
                    {
                        cmbGioiTinh.SelectedItem = item;
                        break;
                    }
                }

                // Load ảnh cũ nếu có
                LoadExistingImage(hv.MaHV);
            }
            else
            {
                // CHẾ ĐỘ THÊM
                _isEditMode = false;
                LoadNextMaHV();
            }
        }

        private void LoadExistingImage(string maHV)
        {
            try
            {
                string[] extensions = { ".jpg", ".png", ".jpeg" };
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HocVienImages");

                foreach (string ext in extensions)
                {
                    string filePath = Path.Combine(folderPath, $"{maHV}{ext}");
                    if (File.Exists(filePath))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(filePath);
                        bitmap.EndInit();
                        imgAvatar.Source = bitmap;

                        if (this.FindName("iconDefaultAvatar") is FrameworkElement icon)
                            icon.Visibility = Visibility.Collapsed;
                        break;
                    }
                }
            }
            catch { }
        }

        private void LoadNextMaHV()
        {
            string newCode = _repository.GenerateNewMaHV();
            txtMaHV.Text = newCode;
        }

        private void BtnChonAnh_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                _selectedImagePath = openFileDialog.FileName;
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_selectedImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgAvatar.Source = bitmap;
                    if (this.FindName("iconDefaultAvatar") is FrameworkElement icon) icon.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex) { MessageBox.Show("Lỗi tải ảnh: " + ex.Message); }
            }
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            string maHV = txtMaHV.Text.Trim();
            string hoTen = txtHoTen.Text.Trim();

            if (string.IsNullOrEmpty(maHV) || string.IsNullOrEmpty(hoTen))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Nếu là THÊM MỚI thì mới kiểm tra trùng mã
            if (!_isEditMode && _repository.CheckMaHVExists(maHV))
            {
                MessageBox.Show($"Mã học viên {maHV} đã tồn tại!", "Trùng mã", MessageBoxButton.OK, MessageBoxImage.Warning);
                LoadNextMaHV();
                return;
            }

            HocVien item = new HocVien
            {
                MaHV = maHV,
                HoTen = hoTen,
                NgaySinh = dpNgaySinh.SelectedDate,
                GioiTinh = (cmbGioiTinh.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Nam",
                Email = txtEmail.Text.Trim(),
                SDT = txtSDT.Text.Trim(),
                DiaChi = ""
            };

            bool result = false;
            if (_isEditMode)
                result = _repository.UpdateHocVien(item);
            else
                result = _repository.AddHocVien(item);

            if (result)
            {
                // Lưu ảnh (nếu có chọn ảnh mới)
                if (!string.IsNullOrEmpty(_selectedImagePath))
                {
                    try
                    {
                        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HocVienImages");
                        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                        // Xóa ảnh cũ nếu có (để tránh rác) - tùy chọn

                        string destFileName = $"{maHV}{Path.GetExtension(_selectedImagePath)}";
                        string destPath = Path.Combine(folderPath, destFileName);
                        File.Copy(_selectedImagePath, destPath, true);
                    }
                    catch { }
                }

                MessageBox.Show(_isEditMode ? "Cập nhật thành công!" : "Thêm thành công!", "Thông báo");
                IsSuccess = true;
                this.Close();
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) { this.Close(); }
    }
}

