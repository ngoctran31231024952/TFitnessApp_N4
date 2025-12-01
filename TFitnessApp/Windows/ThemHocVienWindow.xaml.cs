using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TFitnessApp;
using System.Text.RegularExpressions; 

namespace TFitnessApp.Windows
{
    public partial class ThemHocVienWindow : Window
    {
        private HocVienRepository _repository;
        public bool IsSuccess { get; private set; } = false;
        private string _selectedImagePath = null;
        private bool _isEditMode = false;

        public ThemHocVienWindow(HocVien hv = null)
        {
            InitializeComponent();
            _repository = new HocVienRepository();

            if (hv != null)
            {
                _isEditMode = true;
                txtHeaderTitle.Text = "CẬP NHẬT THÔNG TIN";
                txtBtnAction.Text = "Lưu";

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

                LoadExistingImage(hv.MaHV);
            }
            else
            {
                _isEditMode = false;
                LoadNextMaHV();
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private bool IsNumber(string text)
        {
            return Regex.IsMatch(text, @"^\d+$");
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

        private void LoadNextMaHV() { txtMaHV.Text = _repository.GenerateNewMaHV(); }
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
            string email = txtEmail.Text.Trim();
            string sdt = txtSDT.Text.Trim();

            if (string.IsNullOrEmpty(maHV) || string.IsNullOrEmpty(hoTen))
            {
                MessageBox.Show("Vui lòng nhập Mã HV và Họ tên!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
            {
                MessageBox.Show("Định dạng Email không hợp lệ! Vui lòng kiểm tra lại.\nVí dụ: user@example.com", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            if (!string.IsNullOrEmpty(sdt))
            {
                if (!IsNumber(sdt))
                {
                    MessageBox.Show("Số điện thoại chỉ được chứa các chữ số!", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtSDT.Focus();
                    return;
                }
                if (sdt.Length < 9 || sdt.Length > 11)
                {
                    MessageBox.Show("Số điện thoại phải có từ 9 đến 11 chữ số!", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtSDT.Focus();
                    return;
                }
            }

            if (!_isEditMode && _repository.CheckMaHVExists(maHV))
            {
                MessageBox.Show($"Mã học viên {maHV} đã tồn tại! Đang tải lại mã mới...", "Trùng mã", MessageBoxButton.OK, MessageBoxImage.Warning);
                LoadNextMaHV();
                return;
            }

            HocVien item = new HocVien
            {
                MaHV = maHV,
                HoTen = hoTen,
                NgaySinh = dpNgaySinh.SelectedDate,
                GioiTinh = (cmbGioiTinh.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Nam",
                Email = email,
                SDT = sdt,
                DiaChi = ""
            };

            bool result = false;
            if (_isEditMode)
                result = _repository.UpdateHocVien(item);
            else
                result = _repository.AddHocVien(item);

            if (result)
            {
                if (!string.IsNullOrEmpty(_selectedImagePath))
                {
                    try
                    {
                        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HocVienImages");
                        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
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