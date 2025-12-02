using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace TFitnessApp
{
    public partial class DashboardFilter : UserControl
    {
        public DashboardFilter()
        {
            InitializeComponent();
        }

        // --- CÁC THUỘC TÍNH ẨN/HIỆN GIAO DIỆN (VISIBILITY) ---

        // 1. Nút In (Cũ)
        public static readonly DependencyProperty HienNutInProperty =
            DependencyProperty.Register("HienNutIn", typeof(bool), typeof(DashboardFilter), new PropertyMetadata(true));

        // 2. Chọn Loại Biểu Đồ (Cũ)
        public static readonly DependencyProperty HienChonLoaiBDProperty =
            DependencyProperty.Register("HienChonLoaiBD", typeof(bool), typeof(DashboardFilter), new PropertyMetadata(false));

        // 3. [MỚI] Chọn Chi Nhánh (Mặc định hiện)
        public static readonly DependencyProperty HienChonChiNhanhProperty =
            DependencyProperty.Register("HienChonChiNhanh", typeof(bool), typeof(DashboardFilter), new PropertyMetadata(true));

        // 4. [MỚI] Bộ Lọc Ngày (Mặc định hiện)
        public static readonly DependencyProperty HienBoLocNgayProperty =
            DependencyProperty.Register("HienBoLocNgay", typeof(bool), typeof(DashboardFilter), new PropertyMetadata(true));


        // --- CÁC THUỘC TÍNH DỮ LIỆU (DATA) ---

        public static readonly DependencyProperty SelectedBranchProperty =
            DependencyProperty.Register("SelectedBranch", typeof(string), typeof(DashboardFilter), new PropertyMetadata("Tất cả"));

        public static readonly DependencyProperty BranchSourceProperty =
            DependencyProperty.Register("BranchSource", typeof(IEnumerable), typeof(DashboardFilter));

        public static readonly DependencyProperty SelectedChartTypeProperty =
            DependencyProperty.Register("SelectedChartType", typeof(int), typeof(DashboardFilter), new PropertyMetadata(0));

        public static readonly DependencyProperty FilterFromDateProperty =
            DependencyProperty.Register("FilterFromDate", typeof(DateTime), typeof(DashboardFilter));

        public static readonly DependencyProperty FilterToDateProperty =
            DependencyProperty.Register("FilterToDate", typeof(DateTime), typeof(DashboardFilter));

        // --- WRAPPERS ---

        public bool HienNutIn
        {
            get => (bool)GetValue(HienNutInProperty);
            set => SetValue(HienNutInProperty, value);
        }

        public bool HienChonLoaiBD
        {
            get => (bool)GetValue(HienChonLoaiBDProperty);
            set => SetValue(HienChonLoaiBDProperty, value);
        }

        public bool HienChonChiNhanh // [MỚI]
        {
            get => (bool)GetValue(HienChonChiNhanhProperty);
            set => SetValue(HienChonChiNhanhProperty, value);
        }

        public bool HienBoLocNgay // [MỚI]
        {
            get => (bool)GetValue(HienBoLocNgayProperty);
            set => SetValue(HienBoLocNgayProperty, value);
        }

        public string SelectedBranch
        {
            get => (string)GetValue(SelectedBranchProperty);
            set => SetValue(SelectedBranchProperty, value);
        }

        public IEnumerable BranchSource
        {
            get => (IEnumerable)GetValue(BranchSourceProperty);
            set => SetValue(BranchSourceProperty, value);
        }

        public int SelectedChartType
        {
            get => (int)GetValue(SelectedChartTypeProperty);
            set => SetValue(SelectedChartTypeProperty, value);
        }

        public DateTime FilterFromDate
        {
            get => (DateTime)GetValue(FilterFromDateProperty);
            set => SetValue(FilterFromDateProperty, value);
        }

        public DateTime FilterToDate
        {
            get => (DateTime)GetValue(FilterToDateProperty);
            set => SetValue(FilterToDateProperty, value);
        }
    }
}