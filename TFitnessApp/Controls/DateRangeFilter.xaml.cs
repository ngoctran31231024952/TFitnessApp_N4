using System;
using System.ComponentModel; // Cần thiết cho INotifyPropertyChanged
using System.Windows;
using System.Windows.Controls;

namespace TFitnessApp
{
    // Thêm INotifyPropertyChanged vào đây
    public partial class DateRangeFilter : UserControl, INotifyPropertyChanged
    {
        public DateRangeFilter()
        {
            InitializeComponent();
            // Mặc định chọn tháng này
            TinhToanNgay("this_month");
            // Set text hiển thị ban đầu
            NhanHienTai = "Tháng này";
        }

        // --- 1. DEPENDENCY PROPERTIES (Để Binding ra ngoài) ---

        public static readonly DependencyProperty TuNgayProperty =
            DependencyProperty.Register("TuNgay", typeof(DateTime), typeof(DateRangeFilter),
                new PropertyMetadata(DateTime.Now));

        public static readonly DependencyProperty DenNgayProperty =
            DependencyProperty.Register("DenNgay", typeof(DateTime), typeof(DateRangeFilter),
                new PropertyMetadata(DateTime.Now));

        public DateTime TuNgay
        {
            get => (DateTime)GetValue(TuNgayProperty);
            set => SetValue(TuNgayProperty, value);
        }

        public DateTime DenNgay
        {
            get => (DateTime)GetValue(DenNgayProperty);
            set => SetValue(DenNgayProperty, value);
        }

        // --- 2. PROPERTIES CHO VIEW (Popup, Text hiển thị) ---

        private bool _isPopupOpen;
        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set { _isPopupOpen = value; OnPropertyChanged(nameof(IsPopupOpen)); }
        }

        private string _nhanHienTai;
        public string NhanHienTai
        {
            get => _nhanHienTai;
            set { _nhanHienTai = value; OnPropertyChanged(nameof(NhanHienTai)); }
        }

        // --- 3. EVENT HANDLERS ---

        // Logic xử lý khi chọn menu nhanh
        private void ChonNhanh_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                NhanHienTai = btn.Content.ToString(); // Cập nhật text hiển thị
                string tag = btn.Tag.ToString();
                TinhToanNgay(tag);
                IsPopupOpen = false; // Đóng popup
            }
        }

        // Nút Áp dụng (trong phần chọn ngày tùy chỉnh)
        private void ApDungNgayTuChon_Click(object sender, RoutedEventArgs e)
        {
            if (dpTuNgay.SelectedDate.HasValue && dpDenNgay.SelectedDate.HasValue)
            {
                TuNgay = dpTuNgay.SelectedDate.Value;
                DenNgay = dpDenNgay.SelectedDate.Value;
                NhanHienTai = $"{TuNgay:dd/MM} - {DenNgay:dd/MM}";
                IsPopupOpen = false;
            }
        }

        private void NgayTuChon_ThayDoi(object sender, SelectionChangedEventArgs e) { }

        // --- 4. LOGIC TÍNH TOÁN NGÀY ---

        private void TinhToanNgay(string tag)
        {
            DateTime now = DateTime.Now;
            DateTime start = now;
            DateTime end = now;

            switch (tag)
            {
                case "today":
                    start = end = now;
                    break;
                case "yesterday":
                    start = end = now.AddDays(-1);
                    break;
                case "this_week":
                    // Giả sử thứ 2 là đầu tuần
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    start = now.AddDays(-diff).Date;
                    end = start.AddDays(6).Date;
                    break;
                case "last_week":
                    start = now.AddDays(-(int)now.DayOfWeek - 6);
                    end = start.AddDays(6);
                    break;
                case "this_month":
                    start = new DateTime(now.Year, now.Month, 1);
                    end = start.AddMonths(1).AddDays(-1);
                    break;
                case "last_month":
                    start = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    end = start.AddMonths(1).AddDays(-1);
                    break;
                case "this_year":
                    start = new DateTime(now.Year, 1, 1);
                    end = new DateTime(now.Year, 12, 31);
                    break;
            }

            // Gán giá trị vào Dependency Property
            TuNgay = start;
            DenNgay = end;
        }

        // --- 5. INotifyPropertyChanged Implementation ---

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}