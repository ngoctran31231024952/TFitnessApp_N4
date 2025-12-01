using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TFitnessApp;

namespace TFitnessApp.Windows
{
    public partial class LocHocVienWindow : Window
    {
        private HocVienRepository _repository;
        public HocVienFilterData FilterData { get; private set; }
        public bool IsApply { get; private set; } = false;

        private List<ComboBoxItemData> _allPTs;

        public LocHocVienWindow()
        {
            InitializeComponent();
            _repository = new HocVienRepository();
            LoadFilterData();
        }

        private void LoadFilterData()
        {
            var goiTaps = _repository.GetList("GoiTap", "MaGoi", "TenGoi");
            goiTaps.Insert(0, new ComboBoxItemData { ID = "Tất cả", Name = "-- Tất cả --" });
            cmbGoiTap.ItemsSource = goiTaps;
            cmbGoiTap.SelectedIndex = 0;

            var chiNhanhs = _repository.GetList("ChiNhanh", "MaCN", "TenCN");
            chiNhanhs.Insert(0, new ComboBoxItemData { ID = "Tất cả", Name = "-- Tất cả --" });
            cmbChiNhanh.ItemsSource = chiNhanhs;
            cmbChiNhanh.SelectedIndex = 0;

            _allPTs = _repository.GetList("PT", "MaPT", "HoTen");
            _allPTs.Insert(0, new ComboBoxItemData { ID = "Tất cả", Name = "-- Tất cả --" });
            cmbPT.ItemsSource = _allPTs;
        }

        private void cmbPT_KeyUp(object sender, KeyEventArgs e)
        {
            var combo = sender as ComboBox;
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter) return;

            string searchText = combo.Text.ToLower();
            var filtered = _allPTs.Where(pt => pt.Name.ToLower().Contains(searchText)).ToList();

            combo.ItemsSource = filtered;
            combo.IsDropDownOpen = true;

        }

        private void BtnApDung_Click(object sender, RoutedEventArgs e)
        {
            FilterData = new HocVienFilterData();

            // Giới tính
            if (rbNam.IsChecked == true) FilterData.GioiTinh = "Nam";
            else if (rbNu.IsChecked == true) FilterData.GioiTinh = "Nữ";
            else FilterData.GioiTinh = "Tất cả";

            // Ngày tháng
            FilterData.NamSinhTu = dpNamSinhTu.SelectedDate;
            FilterData.NamSinhDen = dpNamSinhDen.SelectedDate;
            FilterData.NgayThamGiaTu = dpThamGiaTu.SelectedDate;
            FilterData.NgayThamGiaDen = dpThamGiaDen.SelectedDate;

            // Comboboxes
            FilterData.MaGoi = cmbGoiTap.SelectedValue?.ToString();
            FilterData.MaCN = cmbChiNhanh.SelectedValue?.ToString();
            FilterData.MaPT = cmbPT.SelectedValue?.ToString();

            if (string.IsNullOrEmpty(FilterData.MaPT) && !string.IsNullOrEmpty(cmbPT.Text) && cmbPT.Text != "-- Tất cả --")
            {
                var match = _allPTs.FirstOrDefault(p => p.Name.Equals(cmbPT.Text, StringComparison.OrdinalIgnoreCase));
                if (match != null) FilterData.MaPT = match.ID;
            }

            IsApply = true;
            this.Close();
        }

        private void BtnDatLai_Click(object sender, RoutedEventArgs e)
        {
            rbTatCa.IsChecked = true;
            dpNamSinhTu.SelectedDate = null;
            dpNamSinhDen.SelectedDate = null;
            dpThamGiaTu.SelectedDate = null;
            dpThamGiaDen.SelectedDate = null;
            cmbGoiTap.SelectedIndex = 0;
            cmbChiNhanh.SelectedIndex = 0;
            cmbPT.Text = "";
            cmbPT.ItemsSource = _allPTs; 
            cmbPT.SelectedIndex = -1;
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) { this.Close(); }
    }
}