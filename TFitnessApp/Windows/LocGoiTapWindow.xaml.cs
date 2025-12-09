using System;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace TFitnessApp.Windows
{
    public class FilterGoiTapData
    {
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public string PTOption { get; set; }
        public int? Months { get; set; }
        public string SpecialService { get; set; }
    }

    public partial class LocGoiTapWindow : Window
    {
        public FilterGoiTapData FilterData { get; private set; }
        public bool IsApply { get; private set; } = false;

        public LocGoiTapWindow()
        {
            InitializeComponent();
        }

        // kiểm tra số thực dương (IsValidNumber -> KiemTraSoHopLe)
        private bool KiemTraSoHopLe(string text)
        {
            // Cho phép số nguyên hoặc số thập phân, không âm
            return Regex.IsMatch(text, @"^\d+(\.\d+)?$");
        }

        private void BtnApDung_Click(object sender, RoutedEventArgs e)
        {
            FilterData = new FilterGoiTapData();
            // 1. Validate Giá
            string minPriceText = txtGiaTu.Text.Trim();
            string maxPriceText = txtGiaDen.Text.Trim();
            if (!string.IsNullOrEmpty(minPriceText))
            {
                if (!KiemTraSoHopLe(minPriceText))
                {
                    MessageBox.Show("Giá thấp nhất phải là số hợp lệ!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtGiaTu.Focus();
                    return;
                }
                FilterData.MinPrice = double.Parse(minPriceText);
            }
            if (!string.IsNullOrEmpty(maxPriceText))
            {
                if (!KiemTraSoHopLe(maxPriceText))
                {
                    MessageBox.Show("Giá cao nhất phải là số hợp lệ!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtGiaDen.Focus();
                    return;
                }
                FilterData.MaxPrice = double.Parse(maxPriceText);
            }
            // Kiểm tra logic: Giá thấp nhất không được lớn hơn giá cao nhất
            if (FilterData.MinPrice.HasValue && FilterData.MaxPrice.HasValue && FilterData.MinPrice > FilterData.MaxPrice)
            {
                MessageBox.Show("Khoảng giá không hợp lệ (Thấp nhất > Cao nhất)!", "Lỗi logic", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 2. PT
            if (rbPTCo.IsChecked == true) FilterData.PTOption = "Có PT";
            else if (rbPTKhong.IsChecked == true) FilterData.PTOption = "Không PT";
            else FilterData.PTOption = "Tất cả";
            // 3. Thời hạn
            if (cmbThoiHan.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                FilterData.Months = int.Parse(item.Tag.ToString());
            }
            // 4. Dịch vụ
            if (rbDVCo.IsChecked == true) FilterData.SpecialService = "Có";
            else if (rbDVKhong.IsChecked == true) FilterData.SpecialService = "Không";
            else FilterData.SpecialService = "Tất cả";
            IsApply = true;
            this.Close();
        }

        private void BtnDatLai_Click(object sender, RoutedEventArgs e)
        {
            txtGiaTu.Clear();
            txtGiaDen.Clear();
            rbPTAll.IsChecked = true;
            cmbThoiHan.SelectedIndex = 0;
            rbDVAll.IsChecked = true;
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}