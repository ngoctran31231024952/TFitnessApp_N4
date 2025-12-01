using System;
using System.Windows;
using System.Windows.Controls;

namespace TFitnessApp.Windows
{
    // Class chứa dữ liệu lọc để trả về
    public class FilterGoiTapData
    {
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public string PTOption { get; set; } // "Tất cả", "Có PT", "Không PT"
        public int? Months { get; set; }
        public string SpecialService { get; set; } // "Tất cả", "Có", "Không"
    }

    public partial class LocGoiTapWindow : Window
    {
        public FilterGoiTapData FilterData { get; private set; }
        public bool IsApply { get; private set; } = false;

        public LocGoiTapWindow()
        {
            InitializeComponent();
        }

        private void BtnApDung_Click(object sender, RoutedEventArgs e)
        {
            FilterData = new FilterGoiTapData();

            // 1. Giá
            if (double.TryParse(txtGiaTu.Text, out double min)) FilterData.MinPrice = min;
            if (double.TryParse(txtGiaDen.Text, out double max)) FilterData.MaxPrice = max;

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
            else if (rbDVKhong.IsChecked == true) FilterData.SpecialService = "Không"; // Lưu ý DB lưu "không" hay "Không"
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