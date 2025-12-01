using System;

using System.Collections.Generic;

using System.Globalization;

using System.IO;

using System.Linq;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Input;

using LiveCharts;

using LiveCharts.Wpf;

using Microsoft.Data.Sqlite;



namespace TFitnessApp.Pages

{

    // Class chứa dữ liệu KPI (ViewModel)

    public class DashboardKpi

    {

        public decimal DoanhThu { get; set; }

        public int HocVienMoi { get; set; }

        public int TongHocVien { get; set; }

        public int LuotCheckIn { get; set; }

        public int GoiSapHetHan { get; set; }

    }



    public class ChiNhanhOption

    {

        public string MaCN { get; set; }

        public string TenCN { get; set; }

        public override string ToString()

        {

            return TenCN;

        }

    }



    public class ChartDataPoint

    {

        public DateTime Date { get; set; }

        public decimal Money { get; set; }

    }



    public class CheckInInfo

    {

        public string MaHV { get; set; }

        public string HoTen { get; set; }

        public string TenGoi { get; set; }

        public string ThoiGian { get; set; }

    }



    public class TopGoiTapInfo

    {

        public string Rank { get; set; }

        public string TenGoi { get; set; }

        public int SoLuong { get; set; }

        public string Color { get; set; }

    }



    public class HoatDongInfo

    {

        public string NguoiThucHien { get; set; }

        public string HanhDong { get; set; }

        public string ThoiGian { get; set; }

    }



    public partial class TongQuanPage : Page, System.ComponentModel.INotifyPropertyChanged

    {

        public SeriesCollection ChartSeries { get; set; }

        public string[] ChartLabels { get; set; }

        public SeriesCollection PieSeries { get; set; }

        public SeriesCollection CheckInSeries { get; set; }

        public string[] CheckInLabels { get; set; }

        public Func<double, string> ChartFormatter { get; set; }



        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;



        public System.Collections.ObjectModel.ObservableCollection<CustomLegendItem> LegendList { get; set; }

        // 2. Cập nhật Constructor
        public TongQuanPage()
        {
            InitializeComponent();
            ChartFormatter = value => value.ToString("N0");
            ChartSeries = new SeriesCollection();

            // --- THÊM DÒNG NÀY ---
            LegendList = new System.Collections.ObjectModel.ObservableCollection<CustomLegendItem>();

            PieSeries = new SeriesCollection();
            CheckInSeries = new SeriesCollection();
            this.DataContext = this;
        }

        public class CustomLegendItem
        {
            public string Title { get; set; }
            public System.Windows.Media.Brush Color { get; set; }
        }



        private void Page_Loaded(object sender, RoutedEventArgs e)

        {

            try

            {

                LoadComboBoxChiNhanh();

                if (cboCheckInChiNhanh != null && cboChiNhanh.ItemsSource != null)

                {

                    cboCheckInChiNhanh.ItemsSource = cboChiNhanh.ItemsSource;

                    cboCheckInChiNhanh.SelectedIndex = 0;

                }

                LoadCheckInChartData();

                LoadKpiData(""); // Mặc định load hết

                LoadChartData();

                LoadBottomData();

            }

            catch (Exception ex)

            {

                MessageBox.Show("Lỗi tải trang: " + ex.Message);

            }

        }



        // --- 1. TẢI DANH SÁCH CHI NHÁNH ---

        private void LoadComboBoxChiNhanh()

        {

            var listCN = new List<ChiNhanhOption>();

            listCN.Add(new ChiNhanhOption { MaCN = "", TenCN = "Tất cả chi nhánh" });



            try

            {

                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");

                using (var connection = new SqliteConnection($"Data Source={dbPath}"))

                {

                    connection.Open();

                    var cmd = new SqliteCommand("SELECT MaCN, TenCN FROM ChiNhanh", connection);

                    using (var reader = cmd.ExecuteReader())

                    {

                        while (reader.Read())

                        {

                            listCN.Add(new ChiNhanhOption

                            {

                                MaCN = reader["MaCN"].ToString(),

                                TenCN = reader["TenCN"].ToString()

                            });

                        }

                    }

                }

            }

            catch { }



            if (cboChiNhanh != null)

            {

                cboChiNhanh.ItemsSource = listCN;

                cboChiNhanh.SelectedIndex = 0;

            }

            if (cboChartChiNhanh != null)

            {

                cboChartChiNhanh.ItemsSource = listCN;

                cboChartChiNhanh.SelectedIndex = 0;

            }

        }



        // --- 2. SỰ KIỆN CHỌN CHI NHÁNH & TẢI KPI ---

        private void cboChiNhanh_SelectionChanged(object sender, SelectionChangedEventArgs e)

        {

            string maCN = cboChiNhanh.SelectedValue?.ToString();

            LoadKpiData(maCN);

        }



        private void LoadKpiData(string maCN)

        {

            try

            {

                var kpi = GetDashboardData(maCN);

                if (txtDoanhThu != null) txtDoanhThu.Text = kpi.DoanhThu.ToString("N0");

                if (txtHocVienMoi != null) txtHocVienMoi.Text = kpi.HocVienMoi.ToString();

                if (txtCheckIn != null) txtCheckIn.Text = kpi.LuotCheckIn.ToString();

                if (txtSapHetHan != null) txtSapHetHan.Text = kpi.GoiSapHetHan.ToString();

            }

            catch (Exception ex)

            {

                MessageBox.Show("Lỗi tải KPI: " + ex.Message);

            }

        }



        private DashboardKpi GetDashboardData(string maCN)

        {

            var data = new DashboardKpi();

            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");



            string today = "14/11/2025";



            using (var connection = new SqliteConnection($"Data Source={dbPath}"))

            {

                connection.Open();



                //Doanh thu

                string sqlRevenue = "";

                if (string.IsNullOrEmpty(maCN))

                {

                    sqlRevenue = "SELECT SUM(CAST(DaThanhToan AS REAL)) FROM GiaoDich WHERE NgayGD = @today";

                }

                else

                {

                    sqlRevenue = @"SELECT SUM(CAST(GD.DaThanhToan AS REAL)) 

                           FROM GiaoDich GD 

                           JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV

                           WHERE GD.NgayGD = @today AND HD.MaCN = @maCN";

                }



                using (var cmd = new SqliteCommand(sqlRevenue, connection))

                {

                    cmd.Parameters.AddWithValue("@today", today);

                    if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);

                    var res = cmd.ExecuteScalar();

                    data.DoanhThu = (res != DBNull.Value && res != null) ? Convert.ToDecimal(res) : 0;

                }



                // Học viên mới

                string sqlNew = "";



                if (string.IsNullOrEmpty(maCN))

                {

                    sqlNew = "SELECT COUNT(*) FROM HocVien WHERE NgayTao LIKE @todayLike";

                }

                else

                {

                    sqlNew = @"SELECT COUNT(DISTINCT HV.MaHV) 

                       FROM HocVien HV 

                       JOIN HopDong HD ON HV.MaHV = HD.MaHV

                       WHERE HV.NgayTao LIKE @todayLike AND HD.MaCN = @maCN";

                }



                using (var cmd = new SqliteCommand(sqlNew, connection))

                {

                    cmd.Parameters.AddWithValue("@todayLike", today + "%");

                    if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);

                    data.HocVienMoi = Convert.ToInt32(cmd.ExecuteScalar());

                }

                string filterCN = string.IsNullOrEmpty(maCN) ? "" : " AND HD.MaCN = @maCN ";



                // Lượt Check-in

                string sqlCheck = "";



                if (string.IsNullOrEmpty(maCN))

                {

                    sqlCheck = "SELECT COUNT(*) FROM DiemDanh WHERE instr(NgayDD, @shortDate) > 0";

                }

                else

                {

                    sqlCheck = @"SELECT COUNT(DISTINCT DD.MaDD) 

                         FROM DiemDanh DD 

                         LEFT JOIN HopDong HD ON DD.MaHV = HD.MaHV

                         WHERE instr(DD.NgayDD, @shortDate) > 0 AND HD.MaCN = @maCN";

                }



                using (var cmd = new SqliteCommand(sqlCheck, connection))

                {

                    cmd.Parameters.AddWithValue("@shortDate", today);



                    if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);



                    var result = cmd.ExecuteScalar();

                    data.LuotCheckIn = result != null ? Convert.ToInt32(result) : 0;

                }



                // Sắp hết hạn

                DateTime dtStart = DateTime.ParseExact(today, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                DateTime dtEnd = dtStart.AddDays(7);



                int countExpire = 0;

                string sqlGetDates = "SELECT NgayHetHan, MaCN FROM HopDong";



                using (var cmd = new SqliteCommand(sqlGetDates, connection))

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        string dateStr = reader["NgayHetHan"].ToString();

                        string dbMaCN = reader["MaCN"].ToString();

                        if (!string.IsNullOrEmpty(maCN) && dbMaCN != maCN) continue;



                        string[] formats = { "d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy" };



                        if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expDate))

                        {

                            if (expDate.Date >= dtStart.Date && expDate.Date <= dtEnd.Date)

                            {

                                countExpire++;

                            }

                        }

                    }

                }



                data.GoiSapHetHan = countExpire;

            }

            return data;

        }



        //Biểu đồ cột doanh thu
        private void LoadChartData()
        {
            // 1. Lấy thông tin từ bộ lọc
            string maCN = cboChartChiNhanh.SelectedValue?.ToString();
            string timeMode = (cboChartThoiGian.SelectedItem as ComboBoxItem)?.Tag.ToString();

            // 2. Thiết lập thời gian (Ngày hiện tại giả định: 14/11/2025)
            DateTime today = new DateTime(2025, 11, 14);

            DateTime startDate = today;
            DateTime endDate = today;
            bool isGroupByMonth = false;

            // 3. Tính toán khoảng thời gian
            switch (timeMode)
            {
                case "7days":
                    startDate = today.AddDays(-6);
                    endDate = today;
                    break;
                case "thisMonth":
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = today;
                    break;
                case "lastMonth":
                    startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    break;
                case "thisYear":
                    startDate = new DateTime(today.Year, 1, 1);
                    endDate = today;
                    isGroupByMonth = true;
                    break;
                case "lastYear":
                    startDate = new DateTime(today.Year - 1, 1, 1);
                    endDate = new DateTime(today.Year - 1, 12, 31);
                    isGroupByMonth = true;
                    break;
                default:
                    startDate = today.AddDays(-6);
                    break;
            }

            // 4. Lấy dữ liệu thô từ Database
            var rawData = new List<ChartDataPoint>();
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");

            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                string sql = "";

                if (string.IsNullOrEmpty(maCN))
                {
                    // Lấy tất cả (CAST AS REAL để đọc đúng số tiền)
                    sql = @"SELECT NgayGD, CAST(DaThanhToan AS REAL) as Tien FROM GiaoDich";
                }
                else
                {
                    // Lọc theo chi nhánh
                    sql = @"SELECT GD.NgayGD, CAST(GD.DaThanhToan AS REAL) as Tien
                            FROM GiaoDich GD
                            JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV
                            WHERE HD.MaCN = @maCN";
                }

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dateString = reader["NgayGD"].ToString();
                            decimal money = Convert.ToDecimal(reader["Tien"]);

                            string[] formats = { "d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy" };
                            if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime realDate))
                            {
                                if (realDate.Date >= startDate.Date && realDate.Date <= endDate.Date)
                                {
                                    rawData.Add(new ChartDataPoint { Date = realDate, Money = money });
                                }
                            }
                        }
                    }
                }
            }

            // 5. Gom nhóm dữ liệu
            var chartValues = new ChartValues<double>();
            var labels = new List<string>();
            decimal totalRevenue = 0;

            if (isGroupByMonth)
            {
                // [SỬA ĐỔI QUAN TRỌNG]: Không chạy cố định đến 12 nữa
                // Mà chạy từ tháng 1 đến tháng của endDate.
                // VD: Năm nay (endDate=14/11) -> chạy đến 11.
                // VD: Năm ngoái (endDate=31/12) -> chạy đến 12.
                int endMonth = endDate.Year == startDate.Year ? endDate.Month : 12;

                for (int m = 1; m <= endMonth; m++)
                {
                    decimal sum = rawData.Where(x => x.Date.Month == m && x.Date.Year == startDate.Year).Sum(x => x.Money);
                    chartValues.Add((double)sum);
                    labels.Add("T" + m);
                    totalRevenue += sum;
                }
            }
            else
            {
                // Logic theo ngày: Chạy từ ngày bắt đầu đến ngày kết thúc
                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    decimal sum = rawData.Where(x => x.Date.Date == date.Date).Sum(x => x.Money);
                    chartValues.Add((double)sum);
                    labels.Add(date.ToString("dd/MM"));
                    totalRevenue += sum;
                }
            }

            // 6. Cấu hình giao diện động (Giữ nguyên logic bạn đã duyệt)
            double colWidth, colPadding, rotation;

            if (labels.Count > 12)
            {
                colWidth = 30;
                colPadding = 6;
                rotation = 35;
            }
            else
            {
                colWidth = 60;
                colPadding = 2;
                rotation = 0;
            }

            if (ChartDoanhThu.AxisX.Count > 0)
            {
                ChartDoanhThu.AxisX[0].LabelsRotation = rotation;
            }

            // Xóa tooltip mặc định (để tránh lỗi hiện 2 cái)
            ChartDoanhThu.DataTooltip = null;

            if (ChartSeries == null) ChartSeries = new SeriesCollection();
            ChartSeries.Clear();

            ChartSeries.Add(new ColumnSeries
            {
                Title = "",
                Values = chartValues,
                Fill = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#D32F2F"),
                MaxColumnWidth = colWidth,
                ColumnPadding = colPadding,
                LabelPoint = point =>
                {
                    string dateLabel = labels.Count > (int)point.X ? labels[(int)point.X] : "";
                    return $"{dateLabel}\n{point.Y.ToString("N0")}";
                }
            });

            ChartLabels = labels.ToArray();

            if (txtChartTotal != null)
                txtChartTotal.Text = totalRevenue.ToString("N0");

            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("ChartSeries"));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("ChartLabels"));
        }

        private void Chart_DataHover(object sender, ChartPoint chartPoint)
        {
            // 1. Xử lý khung đỏ (Giữ nguyên logic cũ của bạn)
            if (HoverSection != null)
            {
                HoverSection.Visibility = Visibility.Visible;
                HoverSection.Value = chartPoint.X - 0.5;
            }

            var pixelPoint = ChartDoanhThu.ConvertToPixels(new Point(chartPoint.X, chartPoint.Y));

            double x = pixelPoint.X;
            double y = pixelPoint.Y;

            // Cập nhật nội dung
            string dateLabel = ChartLabels.Length > (int)chartPoint.X ? ChartLabels[(int)chartPoint.X] : "";
            txtColDate.Text = dateLabel;
            txtColMoney.Text = chartPoint.Y.ToString("N0");

            // 3. Bật hiển thị Box NGAY LẬP TỨC
            ColumnTooltip.Visibility = Visibility.Visible;

            // 4. [QUAN TRỌNG NHẤT] Ép buộc WPF tính toán lại kích thước Box ngay lúc này
            // Lệnh này đảm bảo ColumnTooltip.ActualWidth lấy được độ rộng chính xác của text mới
            ColumnTooltip.UpdateLayout();

            // 5. Tính toán vị trí (Lúc này ActualWidth đã chuẩn 100%)
            double newLeft = x - (ColumnTooltip.ActualWidth / 2);
            double newTop = y - ColumnTooltip.ActualHeight - 10;

            // --- KẾT THÚC SỬA ---

            // Gán vị trí
            Canvas.SetLeft(ColumnTooltip, newLeft);
            Canvas.SetTop(ColumnTooltip, newTop);
        }

        private void Chart_MouseLeave(object sender, MouseEventArgs e)
        {
            // Ẩn khung đỏ (Cũ)
            if (HoverSection != null)
            {
                HoverSection.Visibility = Visibility.Hidden;
            }

            // Ẩn Box chú thích (Mới)
            if (ColumnTooltip != null)
            {
                ColumnTooltip.Visibility = Visibility.Collapsed;
            }
        }

        // File: TongQuanPage.xaml.cs

        private void LoadPieChartData()
        {
            // 1. Lấy mốc thời gian (GIỮ NGUYÊN)
            string timeMode = (cboChartThoiGian.SelectedItem as ComboBoxItem)?.Tag.ToString();
            DateTime today = new DateTime(2025, 11, 14);
            DateTime startDate = today;
            DateTime endDate = today;

            // (Logic Switch case ngày tháng GIỮ NGUYÊN - Không chỉnh sửa)
            switch (timeMode)
            {
                case "7days": startDate = today.AddDays(-6); endDate = today; break;
                case "thisMonth": startDate = new DateTime(today.Year, today.Month, 1); endDate = startDate.AddMonths(1).AddDays(-1); break;
                case "lastMonth": startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1); endDate = startDate.AddMonths(1).AddDays(-1); break;
                case "thisYear": startDate = new DateTime(today.Year, 1, 1); endDate = new DateTime(today.Year, 12, 31); break;
                case "lastYear": startDate = new DateTime(today.Year - 1, 1, 1); endDate = new DateTime(today.Year - 1, 12, 31); break;
                default: startDate = today.AddDays(-6); break;
            }

            var pieData = new Dictionary<string, decimal>();
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");

            try
            {
                // (Phần lấy dữ liệu SQL GIỮ NGUYÊN - Không chỉnh sửa)
                using (var connection = new SqliteConnection($"Data Source={dbPath}"))
                {
                    connection.Open();
                    string sql = @"SELECT CN.TenCN, CAST(GD.DaThanhToan AS REAL) as Tien, GD.NgayGD
                           FROM GiaoDich GD
                           JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV
                           JOIN ChiNhanh CN ON HD.MaCN = CN.MaCN";

                    using (var cmd = new SqliteCommand(sql, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dateStr = reader["NgayGD"].ToString();
                            string tenCN = reader["TenCN"].ToString();
                            decimal money = Convert.ToDecimal(reader["Tien"]);
                            string[] formats = { "d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "dd-MM-yyyy" };

                            if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime realDate))
                            {
                                if (realDate.Date >= startDate.Date && realDate.Date <= endDate.Date)
                                {
                                    if (pieData.ContainsKey(tenCN)) pieData[tenCN] += money;
                                    else pieData.Add(tenCN, money);
                                }
                            }
                        }
                    }
                }

                // --- BẮT ĐẦU PHẦN CHỈNH SỬA GIAO DIỆN & HOVER ---

                // 1. Cấu hình Tooltip cho đẹp (Nền trắng, chữ đen, có bóng đổ)


                if (PieSeries == null) PieSeries = new SeriesCollection();
                PieSeries.Clear();
                LegendList.Clear();

                decimal totalRevenue = pieData.Sum(x => x.Value);
                string[] colors = { "#EF5350", "#42A5F5", "#FFCA28", "#66BB6A", "#AB47BC", "#8D6E63" };
                int colorIndex = 0;

                var sortedData = pieData.OrderByDescending(x => x.Value).ToList();

                foreach (var item in sortedData)
                {
                    var hexColor = colors[colorIndex % colors.Length];
                    var brush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(hexColor);

                    // Tạo Series
                    var series = new PieSeries
                    {
                        Title = item.Key,
                        Values = new ChartValues<decimal> { item.Value },
                        DataLabels = false,
                        Fill = brush
                    };

                    // --- ĐOẠN NÀY LÀ QUAN TRỌNG: ĐÃ XÓA HẾT CÁC SỰ KIỆN series.MouseEnter/MouseLeave CŨ ---
                    // Vì chúng ta đã dùng DataHover của Chart ở Bước 1 rồi.

                    PieSeries.Add(series);
                    LegendList.Add(new CustomLegendItem { Title = item.Key, Color = brush });
                    colorIndex++;
                }

                if (txtCenterTotal != null) txtCenterTotal.Text = totalRevenue.ToString("N0");

                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("PieSeries"));
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("LegendList"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        // Sự kiện di chuyển chuột trên biểu đồ
        // Sự kiện cập nhật vị trí hộp thoại theo con chuột
        // Sửa tên hàm cũ ChartTyTrong_MouseMove thành ChartContainer_MouseMove
        // Sự kiện di chuyển chuột trên Grid bao ngoài
        private void ChartContainer_MouseMove(object sender, MouseEventArgs e)
        {
            // [QUAN TRỌNG] Đổi tham số bên trong GetPosition thành "TooltipCanvas"
            // Điều này đảm bảo tọa độ chuột và tọa độ vẽ Box nằm trên cùng một hệ quy chiếu
            var point = e.GetPosition(TooltipCanvas);

            // Tinh chỉnh vị trí: Cộng thêm 15px để nó nằm ngay cạnh đuôi con chuột
            double x = point.X + 15;
            double y = point.Y + 15;

            // Giới hạn không cho box chạy ra khỏi khung (Optional - Giúp không bị mất box nếu ra sát lề phải)
            // Nếu box to quá thì bạn có thể bỏ qua đoạn if này cũng được
            if (x + MouseTooltip.ActualWidth > TooltipCanvas.ActualWidth)
            {
                x = point.X - MouseTooltip.ActualWidth - 5; // Đẩy sang trái nếu hết chỗ
            }
            if (y + MouseTooltip.ActualHeight > TooltipCanvas.ActualHeight)
            {
                y = point.Y - MouseTooltip.ActualHeight - 5; // Đẩy lên trên nếu hết chỗ
            }

            // Cập nhật vị trí
            Canvas.SetLeft(MouseTooltip, x);
            Canvas.SetTop(MouseTooltip, y);
        }

        // 1. Khi chuột chạm vào một miếng bánh -> Cập nhật nội dung và Hiện Box
        private void ChartTyTrong_DataHover(object sender, ChartPoint chartPoint)
        {
            var series = chartPoint.SeriesView as PieSeries;
            if (series == null) return;

            // Cập nhật nội dung
            ttTitle.Text = series.Title;
            ttTitle.Foreground = series.Fill; // Lấy màu của miếng bánh gán cho chữ tiêu đề
            ttPercent.Text = $"Tỷ lệ: {chartPoint.Participation:P1}";
            ttValue.Text = $"Tổng tiền: {chartPoint.Y:N0}";

            // Bật Box lên
            MouseTooltip.Visibility = Visibility.Visible;
        }

        // 2. Khi chuột rời khỏi biểu đồ -> Ẩn Box
        private void ChartTyTrong_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseTooltip.Visibility = Visibility.Collapsed;
        }

        private void ChartFilter_Changed(object sender, SelectionChangedEventArgs e)

        {

            if (cboChartChiNhanh == null || cboChartThoiGian == null || ChartDoanhThu == null) return;



            if (ChartDoanhThu.Visibility == Visibility.Visible) LoadChartData();

            else LoadPieChartData();

        }

        // Biến theo dõi tab đang chọn (Mặc định là Doanh Thu)
        private string currentTab = "DoanhThu";

        // 1. SỰ KIỆN CLICK (CHỌN CHÍNH THỨC)
        private void SwitchChartType_Click(object sender, MouseButtonEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;

            if (btn.Name == "btnTabDoanhThu")
            {
                currentTab = "DoanhThu";
                SetTabStyle(txtTabDoanhThu, indDoanhThu, true);
                SetTabStyle(txtTabTyTrong, indTyTrong, false);

                // --- SỬA ĐOẠN NÀY ---
                ChartDoanhThu.Visibility = Visibility.Visible;
                pnlTotalBottom.Visibility = Visibility.Visible;
                cboChartChiNhanh.Visibility = Visibility.Visible;

                // Ẩn nhóm tỷ trọng đi
                if (grpTyTrong != null) grpTyTrong.Visibility = Visibility.Collapsed;

                LoadChartData();
            }
            else
            {
                currentTab = "TyTrong";
                SetTabStyle(txtTabTyTrong, indTyTrong, true);
                SetTabStyle(txtTabDoanhThu, indDoanhThu, false);

                // --- SỬA ĐOẠN NÀY ---
                // Ẩn nhóm doanh thu
                ChartDoanhThu.Visibility = Visibility.Collapsed;
                pnlTotalBottom.Visibility = Visibility.Collapsed;
                cboChartChiNhanh.Visibility = Visibility.Collapsed;

                // Hiện nhóm tỷ trọng (bao gồm cả biểu đồ, số tổng và legend)
                if (grpTyTrong != null) grpTyTrong.Visibility = Visibility.Visible;

                LoadPieChartData();
            }
        }

        // 2. SỰ KIỆN HOVER VÀO (MOUSE ENTER)
        private void BtnTab_MouseEnter(object sender, MouseEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;

            // Nếu hover vào Doanh Thu mà nó KHÔNG PHẢI là tab đang chọn -> Hiện hiệu ứng xám
            if (btn.Name == "btnTabDoanhThu" && currentTab != "DoanhThu")
            {
                txtTabDoanhThu.Foreground = System.Windows.Media.Brushes.Gray; // Chữ xám
                indDoanhThu.Fill = System.Windows.Media.Brushes.LightGray;     // Thanh xám
                indDoanhThu.Visibility = Visibility.Visible;                   // Hiện thanh
            }
            // Tương tự cho Tỷ Trọng
            else if (btn.Name == "btnTabTyTrong" && currentTab != "TyTrong")
            {
                txtTabTyTrong.Foreground = System.Windows.Media.Brushes.Gray;
                indTyTrong.Fill = System.Windows.Media.Brushes.LightGray;
                indTyTrong.Visibility = Visibility.Visible;
            }
        }

        // 3. SỰ KIỆN RỜI CHUỘT RA (MOUSE LEAVE)
        private void BtnTab_MouseLeave(object sender, MouseEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;

            // Nếu rời khỏi Doanh Thu mà nó KHÔNG PHẢI tab chọn -> Trả về màu Đen, ẩn thanh
            if (btn.Name == "btnTabDoanhThu" && currentTab != "DoanhThu")
            {
                txtTabDoanhThu.Foreground = System.Windows.Media.Brushes.Black; // Trả về đen
                indDoanhThu.Visibility = Visibility.Hidden;                     // Ẩn thanh
            }
            // Tương tự cho Tỷ Trọng
            else if (btn.Name == "btnTabTyTrong" && currentTab != "TyTrong")
            {
                txtTabTyTrong.Foreground = System.Windows.Media.Brushes.Black;
                indTyTrong.Visibility = Visibility.Hidden;
            }
        }

        // Hàm phụ trợ để set màu nhanh (Đỏ hoặc Đen)
        private void SetTabStyle(TextBlock txt, System.Windows.Shapes.Rectangle ind, bool isActive)
        {
            if (isActive)
            {
                // Màu Đỏ (#D32F2F)
                txt.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D32F2F"));
                txt.FontWeight = FontWeights.ExtraBold;
                ind.Fill = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D32F2F"));
                ind.Visibility = Visibility.Visible;
            }
            else
            {
                // Màu Đen (Black) & Ẩn thanh
                txt.Foreground = System.Windows.Media.Brushes.Black;
                txt.FontWeight = FontWeights.SemiBold;
                ind.Visibility = Visibility.Hidden;
            }
        }



        // --- CHECK-IN LOGIC ---

        private void LoadCheckInChartData()

        {

            string maCN = cboCheckInChiNhanh.SelectedValue?.ToString();

            string buoi = (cboCheckInBuoi.SelectedItem as ComboBoxItem)?.Tag.ToString();

            int startHour = 6, endHour = 12;

            switch (buoi)

            {

                case "Sang": startHour = 6; endHour = 12; break;

                case "Chieu": startHour = 13; endHour = 17; break;

                case "Toi": startHour = 18; endHour = 22; break;

            }



            var rawData = new List<int>();

            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");

            string targetDate = "20-11-2024";



            using (var connection = new SqliteConnection($"Data Source={dbPath}"))

            {

                connection.Open();

                string filterCN = string.IsNullOrEmpty(maCN) ? "" : " AND HD.MaCN = @maCN ";

                string sql = $@"

                    SELECT DD.ThoiGianVao 

                    FROM DiemDanh DD

                    JOIN HocVien HV ON DD.MaHV = HV.MaHV

                    JOIN HopDong HD ON HV.MaHV = HD.MaHV

                    WHERE DD.NgayDD = @date {filterCN}";



                using (var cmd = new SqliteCommand(sql, connection))

                {

                    cmd.Parameters.AddWithValue("@date", targetDate);

                    if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);

                    using (var reader = cmd.ExecuteReader())

                    {

                        while (reader.Read())

                        {

                            string timeStr = reader["ThoiGianVao"].ToString();

                            if (TimeSpan.TryParse(timeStr, out TimeSpan ts)) rawData.Add(ts.Hours);

                        }

                    }

                }

            }



            var values = new ChartValues<int>();

            var labels = new List<string>();

            for (int h = startHour; h <= endHour; h++)

            {

                int count = rawData.Count(x => x == h);

                values.Add(count);

                labels.Add(h + ":00");

            }



            if (CheckInSeries == null) CheckInSeries = new SeriesCollection();

            CheckInSeries.Clear();

            CheckInSeries.Add(new LineSeries

            {

                Title = "Lượt Check-in",

                Values = values,

                PointGeometry = DefaultGeometries.Circle,

                PointGeometrySize = 10,

                Stroke = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#D32F2F"),

                Fill = System.Windows.Media.Brushes.Transparent

            });

            CheckInLabels = labels.ToArray();

            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("CheckInSeries"));

            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("CheckInLabels"));

        }



        private void CheckInFilter_Changed(object sender, SelectionChangedEventArgs e)

        {

            if (cboCheckInChiNhanh == null || cboCheckInBuoi == null || ChartCheckInLine == null) return;

            if (ChartCheckInLine.Visibility == Visibility.Visible) LoadCheckInChartData();

            else LoadCheckInListData();

        }



        // Biến theo dõi tab Check-in hiện tại (Mặc định là Chart)
        private string currentCheckInTab = "Chart";

        // --- 1. SỰ KIỆN CLICK CHUYỂN TAB (SECTION 3) ---
        private void SwitchCheckInTab_Click(object sender, MouseButtonEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;

            string selectedTag = btn.Tag.ToString();

            if (selectedTag == "Chart")
            {
                currentCheckInTab = "Chart";
                SetCheckInTabStyle(txtTabCheckInChart, indCheckInChart, true);
                SetCheckInTabStyle(txtTabCheckInList, indCheckInList, false);

                // Hiển thị Chart, ẩn List
                ChartCheckInLine.Visibility = Visibility.Visible;
                GridCheckInList.Visibility = Visibility.Collapsed;
                txtTotalCheckInList.Visibility = Visibility.Collapsed;

                // Hiển thị bộ lọc buổi (nếu logic yêu cầu chỉ chart mới cần, hoặc cả 2 đều cần thì để nguyên)
                cboCheckInBuoi.Visibility = Visibility.Visible;

                LoadCheckInChartData();
            }
            else // List
            {
                currentCheckInTab = "List";
                SetCheckInTabStyle(txtTabCheckInList, indCheckInList, true);
                SetCheckInTabStyle(txtTabCheckInChart, indCheckInChart, false);

                // Ẩn Chart, hiện List
                ChartCheckInLine.Visibility = Visibility.Collapsed;
                GridCheckInList.Visibility = Visibility.Visible;
                txtTotalCheckInList.Visibility = Visibility.Visible;

                // Giữ nguyên bộ lọc
                cboCheckInBuoi.Visibility = Visibility.Visible;

                LoadCheckInListData();
            }
        }

        // --- 2. SỰ KIỆN HOVER (MOUSE ENTER) ---
        private void BtnCheckInTab_MouseEnter(object sender, MouseEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;

            // Nếu hover vào Chart mà đang chọn List -> Hiệu ứng xám
            if (btn.Name == "btnTabCheckInChart" && currentCheckInTab != "Chart")
            {
                txtTabCheckInChart.Foreground = System.Windows.Media.Brushes.Gray;
                indCheckInChart.Fill = System.Windows.Media.Brushes.LightGray;
                indCheckInChart.Visibility = Visibility.Visible;
            }
            // Nếu hover vào List mà đang chọn Chart -> Hiệu ứng xám
            else if (btn.Name == "btnTabCheckInList" && currentCheckInTab != "List")
            {
                txtTabCheckInList.Foreground = System.Windows.Media.Brushes.Gray;
                indCheckInList.Fill = System.Windows.Media.Brushes.LightGray;
                indCheckInList.Visibility = Visibility.Visible;
            }
        }

        // --- 3. SỰ KIỆN RỜI CHUỘT (MOUSE LEAVE) ---
        // 1. SỰ KIỆN RỜI CHUỘT (MOUSE LEAVE) -> Trả về màu Đen
        private void BtnCheckInTab_MouseLeave(object sender, MouseEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;

            // Nếu rời khỏi tab Chart mà nó KHÔNG PHẢI tab đang chọn -> Về màu Đen
            if (btn.Name == "btnTabCheckInChart" && currentCheckInTab != "Chart")
            {
                txtTabCheckInChart.Foreground = System.Windows.Media.Brushes.Black;
                indCheckInChart.Visibility = Visibility.Hidden;
            }
            // Nếu rời khỏi tab List mà nó KHÔNG PHẢI tab đang chọn -> Về màu Đen
            else if (btn.Name == "btnTabCheckInList" && currentCheckInTab != "List")
            {
                txtTabCheckInList.Foreground = System.Windows.Media.Brushes.Black;
                indCheckInList.Visibility = Visibility.Hidden;
            }
        }

        // 2. HÀM SET STYLE -> Set màu Đen cho tab không active
        private void SetCheckInTabStyle(TextBlock txt, System.Windows.Shapes.Rectangle ind, bool isActive)
        {
            if (isActive)
            {
                // Màu Đỏ khi đang chọn
                txt.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D32F2F"));
                txt.FontWeight = FontWeights.ExtraBold;
                ind.Fill = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D32F2F"));
                ind.Visibility = Visibility.Visible;
            }
            else
            {
                // Màu Đen khi không chọn (giống Section 2)
                txt.Foreground = System.Windows.Media.Brushes.Black;
                txt.FontWeight = FontWeights.SemiBold;
                ind.Visibility = Visibility.Hidden;
            }
        }



        private void LoadCheckInListData()

        {

            var listCheckIn = new List<CheckInInfo>();

            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");

            string maCN = cboCheckInChiNhanh.SelectedValue?.ToString();

            string buoi = (cboCheckInBuoi.SelectedItem as ComboBoxItem)?.Tag.ToString();

            string targetDate = "20-11-2024";



            int startHour = 0, endHour = 24;

            switch (buoi)

            {

                case "Sang": startHour = 6; endHour = 12; break;

                case "Chieu": startHour = 13; endHour = 17; break;

                case "Toi": startHour = 18; endHour = 22; break;

            }



            try

            {

                using (var connection = new SqliteConnection($"Data Source={dbPath}"))

                {

                    connection.Open();

                    string filterCN = string.IsNullOrEmpty(maCN) ? "" : " AND HD.MaCN = @maCN ";

                    string sql = $@"

                        SELECT DD.MaHV, HV.HoTen, GT.TenGoi, DD.ThoiGianVao, DD.NgayDD

                        FROM DiemDanh DD

                        JOIN HocVien HV ON DD.MaHV = HV.MaHV

                        JOIN HopDong HD ON HV.MaHV = HD.MaHV

                        JOIN GoiTap GT ON HD.MaGoi = GT.MaGoi

                        WHERE DD.NgayDD = @date {filterCN}";



                    using (var cmd = new SqliteCommand(sql, connection))

                    {

                        cmd.Parameters.AddWithValue("@date", targetDate);

                        if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);



                        using (var reader = cmd.ExecuteReader())

                        {

                            while (reader.Read())

                            {

                                string timeStr = reader["ThoiGianVao"].ToString();

                                if (TimeSpan.TryParse(timeStr, out TimeSpan ts))

                                {

                                    if (ts.Hours >= startHour && ts.Hours <= endHour)

                                    {

                                        listCheckIn.Add(new CheckInInfo

                                        {

                                            MaHV = reader["MaHV"].ToString(),

                                            HoTen = reader["HoTen"].ToString(),

                                            TenGoi = reader["TenGoi"].ToString(),

                                            ThoiGian = $"{reader["NgayDD"]} - {timeStr}"

                                        });

                                    }

                                }

                            }

                        }

                    }

                }

                if (GridCheckInList != null) GridCheckInList.ItemsSource = listCheckIn;

                if (txtTotalCheckInList != null) txtTotalCheckInList.Text = $"Đang hiện diện: {listCheckIn.Count}";

            }

            catch (Exception ex)

            {

                MessageBox.Show("Lỗi tải danh sách check-in: " + ex.Message);

            }

        }



        private void LoadBottomData()

        {

            var listTop = new List<TopGoiTapInfo>();

            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");

            string targetMonth = "10";

            string targetYear = "2024";

            string[] colors = { "#FF7043", "#66BB6A", "#42A5F5", "#FFA726", "#EF5350" };



            using (var connection = new SqliteConnection($"Data Source={dbPath}"))

            {

                connection.Open();

                string sqlTop = @"

                    SELECT GT.TenGoi, COUNT(HD.MaHD) as SoLuong

                    FROM HopDong HD

                    JOIN GoiTap GT ON HD.MaGoi = GT.MaGoi

                    WHERE substr(HD.NgayBatDau, 4, 2) = @m 

                    AND substr(HD.NgayBatDau, 7, 4) = @y

                    GROUP BY GT.TenGoi

                    ORDER BY SoLuong DESC

                    LIMIT 5";



                using (var cmd = new SqliteCommand(sqlTop, connection))

                {

                    cmd.Parameters.AddWithValue("@m", targetMonth);

                    cmd.Parameters.AddWithValue("@y", targetYear);

                    using (var reader = cmd.ExecuteReader())

                    {

                        int rank = 1;

                        while (reader.Read())

                        {

                            listTop.Add(new TopGoiTapInfo

                            {

                                Rank = rank.ToString("00"),

                                TenGoi = reader["TenGoi"].ToString(),

                                SoLuong = Convert.ToInt32(reader["SoLuong"]),

                                Color = colors[(rank - 1) % colors.Length]

                            });

                            rank++;

                        }

                    }

                }

            }

            if (listTop.Count == 0)

            {

                listTop.Add(new TopGoiTapInfo { Rank = "01", TenGoi = "GÓI STUDENT - 3 THÁNG", SoLuong = 5, Color = colors[0] });

                listTop.Add(new TopGoiTapInfo { Rank = "02", TenGoi = "GÓI HUẤN LUYỆN VIÊN", SoLuong = 4, Color = colors[1] });

            }

            ListTopGoiTap.ItemsSource = listTop;



            var listAct = new List<HoatDongInfo>();

            listAct.Add(new HoatDongInfo

            {

                NguoiThucHien = "Nguyễn Văn A #0102",

                HanhDong = " vừa thanh toán gói Student - 3 tháng",

                ThoiGian = "23/11/2025 09:39:40"

            });

            listAct.Add(new HoatDongInfo

            {

                NguoiThucHien = "Trần Thị B #0103",

                HanhDong = " vừa ký hợp đồng PT cá nhân",

                ThoiGian = "23/11/2025 09:30:02"

            });

            listAct.Add(new HoatDongInfo

            {

                NguoiThucHien = "Lê Văn C #0104",

                HanhDong = " vừa check-in chi nhánh Quận 2",

                ThoiGian = "23/11/2025 09:00:00"

            });

            ListHoatDong.ItemsSource = listAct;

        }

    }
}