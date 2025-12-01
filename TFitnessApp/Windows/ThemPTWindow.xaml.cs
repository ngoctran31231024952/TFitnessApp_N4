using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TFitnessApp;

namespace TFitnessApp.Windows
{
    public partial class ThemPTWindow : Window
    {
        private PTRepository _repository;
        public bool IsSuccess { get; private set; } = false;
        private string _selectedImagePath = null;
        private bool _isEditMode = false;

        public ThemPTWindow(PT pt = null)
        {
            InitializeComponent();
            _repository = new PTRepository();

            // Load danh sách chi nhánh vào ComboBox
            cmbChiNhanh.ItemsSource = _repository.GetAllChiNhanh();
            if (cmbChiNhanh.Items.Count > 0) cmbChiNhanh.SelectedIndex = 0;

            if (pt != null)
            {
                _isEditMode = true;
                txtHeaderTitle.Text = "SỬA THÔNG TIN PT";
                txtBtnAction.Text = "Lưu";

                txtMaPT.Text = pt.MaPT;
                txtHoTen.Text = pt.HoTen;
                txtEmail.Text = pt.Email;
                txtSDT.Text = pt.SDT;

                // Chọn giới tính
                foreach (System.Windows.Controls.ComboBoxItem item in cmbGioiTinh.Items)
                {
                    if (item.Content.ToString() == pt.GioiTinh)
                    {
                        cmbGioiTinh.SelectedItem = item;
                        break;
                    }
                }

                // Chọn chi nhánh
                cmbChiNhanh.SelectedValue = pt.MaCN;

                LoadExistingImage(pt.MaPT);
            }
            else
            {
                txtMaPT.Text = _repository.GenerateNewMaPT();
            }
        }

        private void LoadExistingImage(string maPT)
        {
            try
            {
                string[] extensions = { ".jpg", ".png", ".jpeg" };
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PTImages");
                foreach (string ext in extensions)
                {
                    string filePath = Path.Combine(folderPath, $"{maPT}{ext}");
                    if (File.Exists(filePath))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(filePath);
                        bitmap.EndInit();
                        imgAvatar.Source = bitmap;
                        iconDefaultAvatar.Visibility = Visibility.Collapsed;
                        break;
                    }
                }
            }
            catch { }
        }

        private void BtnChonAnh_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image files|*.jpg;*.png;*.jpeg";
            if (dlg.ShowDialog() == true)
            {
                _selectedImagePath = dlg.FileName;
                imgAvatar.Source = new BitmapImage(new Uri(_selectedImagePath));
                iconDefaultAvatar.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            string maPT = txtMaPT.Text;
            string hoTen = txtHoTen.Text.Trim();
            if (string.IsNullOrEmpty(hoTen)) { MessageBox.Show("Vui lòng nhập tên PT"); return; }

            if (!_isEditMode && _repository.CheckMaPTExists(maPT))
            {
                MessageBox.Show("Mã PT đã tồn tại!"); return;
            }

            PT pt = new PT
            {
                MaPT = maPT,
                HoTen = hoTen,
                Email = txtEmail.Text.Trim(),
                SDT = txtSDT.Text.Trim(),
                GioiTinh = (cmbGioiTinh.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString(),
                MaCN = cmbChiNhanh.SelectedValue?.ToString()
            };

            bool result = _isEditMode ? _repository.UpdatePT(pt) : _repository.AddPT(pt);

            if (result)
            {
                if (!string.IsNullOrEmpty(_selectedImagePath))
                {
                    try
                    {
                        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PTImages");
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                        string dest = Path.Combine(folder, $"{maPT}{Path.GetExtension(_selectedImagePath)}");
                        File.Copy(_selectedImagePath, dest, true);
                    }
                    catch { }
                }
                MessageBox.Show("Thành công!");
                IsSuccess = true;
                this.Close();
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) { this.Close(); }
    }
}