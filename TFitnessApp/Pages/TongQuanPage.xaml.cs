using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Data.Sqlite;
using TFitnessApp.Database;

namespace TFitnessApp.Pages
{
    // CÁC LỚP DỮ LIỆU
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
        public override string ToString() => TenCN;
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
        public string Ngay { get; set; }
        public string GioVao { get; set; }
        public string GioRa { get; set; }
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
    // KHỞI TẠO
    public partial class TongQuanPage : Page, System.ComponentModel.INotifyPropertyChanged
    {
        private readonly DateTime currentToday = new DateTime(2025, 11, 14); // GIẢ LẬP NGÀY HIỆN TẠI LÀ 14/11/2025
        private readonly string[] dateFormats = { "d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm" };

        public SeriesCollection ChartSeries { get; set; }
        public string[] ChartLabels { get; set; }
        public SeriesCollection PieSeries { get; set; }
        public SeriesCollection CheckInSeries { get; set; }
        public string[] CheckInLabels { get; set; }
        public Func<double, string> ChartFormatter { get; set; }
        public System.Collections.ObjectModel.ObservableCollection<CustomLegendItem> LegendList { get; set; }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public TongQuanPage()
        {
            InitializeComponent();
            ChartFormatter = value => value.ToString("N0");
            ChartSeries = new SeriesCollection();
            LegendList = new System.Collections.ObjectModel.ObservableCollection<CustomLegendItem>();
            PieSeries = new SeriesCollection();
            CheckInSeries = new SeriesCollection();
            this.DataContext = this;
        }

        public class CustomLegendItem
        {
            public string Title { get; set; }
            public Brush Color { get; set; }
        }
    // CÁC HÀM XỬ LÝ LOGIC CHUNG
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TaiDuLieuTheoChiNhanh();
                if (cboCheckInChiNhanh != null && cboChiNhanh.ItemsSource != null)
                {
                    cboCheckInChiNhanh.ItemsSource = cboChiNhanh.ItemsSource;
                    cboCheckInChiNhanh.SelectedIndex = 0;
                }

                TaiBieuDoCheckIn();
                TaiDuLieuKPI("");
                TaiBieuDoCot();
                TaiDuLieuPhanCuoi();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải trang: " + ex.Message);
            }
        }

        private void LayKhoangThoiGian(string timeMode, out DateTime start, out DateTime end, out bool isGroupByMonth)
        {
            start = currentToday;
            end = currentToday;
            isGroupByMonth = false;

            switch (timeMode)
            {
                case "7days":
                    start = currentToday.AddDays(-6);
                    break;
                case "thisMonth":
                    start = new DateTime(currentToday.Year, currentToday.Month, 1);
                    end = start.AddMonths(1).AddDays(-1);
                    if (end > currentToday) end = currentToday;
                    break;
                case "lastMonth":
                    start = new DateTime(currentToday.Year, currentToday.Month, 1).AddMonths(-1);
                    end = start.AddMonths(1).AddDays(-1);
                    break;
                case "thisYear":
                    start = new DateTime(currentToday.Year, 1, 1);
                    isGroupByMonth = true;
                    break;
                case "lastYear":
                    start = new DateTime(currentToday.Year - 1, 1, 1);
                    end = new DateTime(currentToday.Year - 1, 12, 31);
                    isGroupByMonth = true;
                    break;
                default:
                    start = currentToday.AddDays(-6);
                    break;
            }
        }

    // XỬ LÝ DỮ LIỆU & DATABASE
    // DỮ LIỆU KPI
        private void TaiDuLieuTheoChiNhanh()
        {
            var listCN = new List<ChiNhanhOption>();
            listCN.Add(new ChiNhanhOption { MaCN = "", TenCN = "Tất cả chi nhánh" });

            try
            {
                using (var connection = TruyCapDB.TaoKetNoi())
                {
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

            if (cboChiNhanh != null) { cboChiNhanh.ItemsSource = listCN; cboChiNhanh.SelectedIndex = 0; }
            if (cboChartChiNhanh != null) { cboChartChiNhanh.ItemsSource = listCN; cboChartChiNhanh.SelectedIndex = 0; }
        }

     // SỰ KIỆN GIAO DIỆN   
        private void cboChiNhanh_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string maCN = cboChiNhanh.SelectedValue?.ToString();
            TaiDuLieuKPI(maCN);
        }
        // PHÍM TẮT
        private void TaiDuLieuKPI(string maCN)
        {
            try
            {
                var data = new DashboardKpi();
                string todayStr = currentToday.ToString("dd/MM/yyyy");

                using (var connection = TruyCapDB.TaoKetNoi())
                {
                    // 1. Doanh thu
                    string sqlRevenue = "SELECT DaThanhToan FROM GiaoDich WHERE NgayGD LIKE @todayLike";
                    if (!string.IsNullOrEmpty(maCN))
                    {
                        sqlRevenue = @"SELECT GD.DaThanhToan 
                                   FROM GiaoDich GD 
                                   JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV
                                   WHERE GD.NgayGD LIKE @todayLike AND HD.MaCN = @maCN";
                    }

                    using (var cmd = new SqliteCommand(sqlRevenue, connection))
                    {
                        cmd.Parameters.AddWithValue("@todayLike", todayStr + "%");

                        if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);

                        using (var reader = cmd.ExecuteReader())
                        {
                            decimal totalRevenue = 0;
                            while (reader.Read())
                            {
                                string rawMoney = reader["DaThanhToan"].ToString();
                                string cleanMoney = new string(rawMoney.Where(char.IsDigit).ToArray());

                                if (decimal.TryParse(cleanMoney, out decimal val))
                                {
                                    totalRevenue += val;
                                }
                            }
                            data.DoanhThu = totalRevenue;
                        }
                    }

                    // 2. Học viên mới
                    string sqlNew = "SELECT COUNT(*) FROM HocVien WHERE NgayTao LIKE @todayLike";
                    if (!string.IsNullOrEmpty(maCN))
                    {
                        sqlNew = @"SELECT COUNT(DISTINCT HV.MaHV) FROM HocVien HV 
                                   JOIN HopDong HD ON HV.MaHV = HD.MaHV
                                   WHERE HV.NgayTao LIKE @todayLike AND HD.MaCN = @maCN";
                    }
                    using (var cmd = new SqliteCommand(sqlNew, connection))
                    {
                        cmd.Parameters.AddWithValue("@todayLike", todayStr + "%");
                        if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);
                        data.HocVienMoi = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 3. Check-in
                    string sqlCheck = "SELECT COUNT(*) FROM DiemDanh WHERE instr(NgayDD, @shortDate) > 0";
                    if (!string.IsNullOrEmpty(maCN))
                    {
                        sqlCheck = @"SELECT COUNT(DISTINCT DD.MaDD) FROM DiemDanh DD 
                                     LEFT JOIN HopDong HD ON DD.MaHV = HD.MaHV
                                     WHERE instr(DD.NgayDD, @shortDate) > 0 AND HD.MaCN = @maCN";
                    }
                    using (var cmd = new SqliteCommand(sqlCheck, connection))
                    {
                        cmd.Parameters.AddWithValue("@shortDate", todayStr);
                        if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);
                        var res = cmd.ExecuteScalar();
                        data.LuotCheckIn = res != null ? Convert.ToInt32(res) : 0;
                    }

                    // 4. Sắp hết hạn
                    DateTime dtEnd = currentToday.AddDays(7);
                    int countExpire = 0;
                    string sqlGetDates = "SELECT NgayHetHan, MaCN FROM HopDong";
                    using (var cmd = new SqliteCommand(sqlGetDates, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!string.IsNullOrEmpty(maCN) && reader["MaCN"].ToString() != maCN) continue;

                            string dateStr = reader["NgayHetHan"].ToString();
                            if (DateTime.TryParseExact(dateStr, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expDate))
                            {
                                if (expDate.Date >= currentToday.Date && expDate.Date <= dtEnd.Date)
                                    countExpire++;
                            }
                        }
                    }
                    data.GoiSapHetHan = countExpire;
                }

                if (txtDoanhThu != null) txtDoanhThu.Text = data.DoanhThu.ToString("N0");
                if (txtHocVienMoi != null) txtHocVienMoi.Text = data.HocVienMoi.ToString();
                if (txtCheckIn != null) txtCheckIn.Text = data.LuotCheckIn.ToString();
                if (txtSapHetHan != null) txtSapHetHan.Text = data.GoiSapHetHan.ToString();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải KPI: " + ex.Message); }
        }

        // DỮ LIỆU DOANH THU THEO BIỂU ĐỒ
        private void TaiBieuDoCot()
        {
            string maCN = cboChartChiNhanh.SelectedValue?.ToString();
            string timeMode = (cboChartThoiGian.SelectedItem as ComboBoxItem)?.Tag.ToString();

            LayKhoangThoiGian(timeMode, out DateTime startDate, out DateTime endDate, out bool isGroupByMonth);

            var rawData = new List<ChartDataPoint>();

            using (var connection = TruyCapDB.TaoKetNoi())
            {
                string sql = string.IsNullOrEmpty(maCN)
                    ? @"SELECT NgayGD, CAST(DaThanhToan AS REAL) as Tien FROM GiaoDich"
                    : @"SELECT GD.NgayGD, CAST(GD.DaThanhToan AS REAL) as Tien FROM GiaoDich GD 
                        JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV WHERE HD.MaCN = @maCN";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dateString = reader["NgayGD"].ToString();
                            if (DateTime.TryParseExact(dateString, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime realDate))
                            {
                                if (realDate.Date >= startDate.Date && realDate.Date <= endDate.Date)
                                {
                                    rawData.Add(new ChartDataPoint { Date = realDate, Money = Convert.ToDecimal(reader["Tien"]) });
                                }
                            }
                        }
                    }
                }
            }

            var chartValues = new ChartValues<double>();
            var labels = new List<string>();
            decimal totalRevenue = 0;

            if (isGroupByMonth)
            {
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
                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    decimal sum = rawData.Where(x => x.Date.Date == date.Date).Sum(x => x.Money);
                    chartValues.Add((double)sum);
                    labels.Add(date.ToString("dd/MM"));
                    totalRevenue += sum;
                }
            }

            double colWidth = labels.Count > 12 ? 30 : 60;
            double colPadding = labels.Count > 12 ? 6 : 2;
            if (ChartDoanhThu.AxisX.Count > 0) ChartDoanhThu.AxisX[0].LabelsRotation = labels.Count > 12 ? 35 : 0;
            ChartDoanhThu.DataTooltip = null;

            if (ChartSeries == null) ChartSeries = new SeriesCollection();
            ChartSeries.Clear();

            ChartSeries.Add(new ColumnSeries
            {
                Values = chartValues,
                Fill = (Brush)new BrushConverter().ConvertFrom("#D32F2F"),
                MaxColumnWidth = colWidth,
                ColumnPadding = colPadding,
                LabelPoint = point => {
                    string dateLabel = labels.Count > (int)point.X ? labels[(int)point.X] : "";
                    return $"{dateLabel}\n{point.Y:N0}";
                }
            });

            ChartLabels = labels.ToArray();
            if (txtChartTotal != null) txtChartTotal.Text = totalRevenue.ToString("N0");

            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("ChartSeries"));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("ChartLabels"));
        }

        private void TaiBieuDoTron()
        {
            string timeMode = (cboChartThoiGian.SelectedItem as ComboBoxItem)?.Tag.ToString();
            LayKhoangThoiGian(timeMode, out DateTime startDate, out DateTime endDate, out bool _);

            var pieData = new Dictionary<string, decimal>();

            try
            {
                using (var connection = TruyCapDB.TaoKetNoi())
                {
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
                            if (DateTime.TryParseExact(dateStr, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime realDate))
                            {
                                if (realDate.Date >= startDate.Date && realDate.Date <= endDate.Date)
                                {
                                    string tenCN = reader["TenCN"].ToString();
                                    decimal money = Convert.ToDecimal(reader["Tien"]);
                                    if (pieData.ContainsKey(tenCN)) pieData[tenCN] += money;
                                    else pieData.Add(tenCN, money);
                                }
                            }
                        }
                    }
                }

                if (PieSeries == null) PieSeries = new SeriesCollection();
                PieSeries.Clear();
                LegendList.Clear();

                decimal totalRevenue = pieData.Sum(x => x.Value);
                string[] colors = { "#EF5350", "#42A5F5", "#FFCA28", "#66BB6A", "#AB47BC", "#8D6E63" };
                int colorIndex = 0;

                foreach (var item in pieData.OrderByDescending(x => x.Value))
                {
                    var brush = (Brush)new BrushConverter().ConvertFrom(colors[colorIndex % colors.Length]);
                    PieSeries.Add(new PieSeries
                    {
                        Title = item.Key,
                        Values = new ChartValues<decimal> { item.Value },
                        DataLabels = false,
                        Fill = brush
                    });
                    LegendList.Add(new CustomLegendItem { Title = item.Key, Color = brush });
                    colorIndex++;
                }

                if (txtCenterTotal != null) txtCenterTotal.Text = totalRevenue.ToString("N0");
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("PieSeries"));
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("LegendList"));
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        // DỮ LIỆU CHECK-IN
        private void TaiBieuDoCheckIn()
        {
            string maCN = cboCheckInChiNhanh.SelectedValue?.ToString();
            string buoi = (cboCheckInBuoi.SelectedItem as ComboBoxItem)?.Tag.ToString();

            int startHour = 6, endHour = 22;
            switch (buoi)
            {
                case "Sang": startHour = 6; endHour = 12; break;
                case "Chieu": startHour = 13; endHour = 17; break;
                case "Toi": startHour = 18; endHour = 22; break;
            }

            var hourCounts = new List<int>();
            string targetDateStr = currentToday.ToString("dd/MM/yyyy");

            try
            {
                using (var connection = TruyCapDB.TaoKetNoi())
                {
                    string filterCN = string.IsNullOrEmpty(maCN) ? "" : " AND HD.MaCN = @maCN ";
                    string sql = $@"SELECT DD.ThoiGianVao FROM DiemDanh DD
                                    LEFT JOIN HopDong HD ON DD.MaHV = HD.MaHV
                                    WHERE DD.NgayDD = @date {filterCN}";

                    using (var cmd = new SqliteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@date", targetDateStr);
                        if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (TimeSpan.TryParse(reader["ThoiGianVao"].ToString(), out TimeSpan ts))
                                {
                                    if (ts.Hours >= startHour && ts.Hours <= endHour) hourCounts.Add(ts.Hours);
                                }
                            }
                        }
                    }
                }

                var values = new ChartValues<int>();
                var labels = new List<string>();
                int totalCheckIn = 0;

                for (int h = startHour; h <= endHour; h++)
                {
                    int count = hourCounts.Count(x => x == h);
                    values.Add(count);
                    labels.Add($"{h}:00");
                    totalCheckIn += count;
                }

                if (CheckInSeries == null) CheckInSeries = new SeriesCollection();
                CheckInSeries.Clear();
                CheckInSeries.Add(new LineSeries
                {
                    Title = "Lượt Check-in",
                    Values = values,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 11,
                    StrokeThickness = 2,
                    Stroke = (Brush)new BrushConverter().ConvertFrom("#D32F2F"),
                    Fill = Brushes.Transparent,
                    LineSmoothness = 0
                });

                CheckInLabels = labels.ToArray();
                if (txtTotalCheckInChart != null) txtTotalCheckInChart.Text = totalCheckIn.ToString("N0");

                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("CheckInSeries"));
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("CheckInLabels"));
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải biểu đồ Check-in: " + ex.Message); }
        }

        private void TaiDanhSachCheckIn()
        {
            string maCN = cboCheckInChiNhanh.SelectedValue?.ToString();
            string buoi = (cboCheckInBuoi.SelectedItem as ComboBoxItem)?.Tag.ToString();

            int startHour = 6, endHour = 22;
            switch (buoi)
            {
                case "Sang": startHour = 6; endHour = 12; break;
                case "Chieu": startHour = 13; endHour = 17; break;
                case "Toi": startHour = 18; endHour = 22; break;
            }

            var listCheckIn = new List<CheckInInfo>();
            string targetDateStr = currentToday.ToString("dd/MM/yyyy");

            try
            {
                using (var connection = TruyCapDB.TaoKetNoi())
                {
                    string filterCN = string.IsNullOrEmpty(maCN) ? "" : " AND HD.MaCN = @maCN ";
                    string sql = $@"SELECT DD.MaHV, HV.HoTen, GT.TenGoi, DD.ThoiGianVao, DD.ThoiGianRa, DD.NgayDD
                                    FROM DiemDanh DD
                                    JOIN HocVien HV ON DD.MaHV = HV.MaHV
                                    LEFT JOIN HopDong HD ON HV.MaHV = HD.MaHV
                                    LEFT JOIN GoiTap GT ON HD.MaGoi = GT.MaGoi
                                    WHERE DD.NgayDD = @date {filterCN}";

                    using (var cmd = new SqliteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@date", targetDateStr);
                        if (!string.IsNullOrEmpty(maCN)) cmd.Parameters.AddWithValue("@maCN", maCN);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string timeVaoStr = reader["ThoiGianVao"].ToString();
                                if (TimeSpan.TryParse(timeVaoStr, out TimeSpan ts))
                                {
                                    if (ts.Hours >= startHour && ts.Hours <= endHour)
                                    {
                                        listCheckIn.Add(new CheckInInfo
                                        {
                                            MaHV = reader["MaHV"].ToString(),
                                            HoTen = reader["HoTen"].ToString(),
                                            TenGoi = reader["TenGoi"].ToString(),
                                            Ngay = reader["NgayDD"].ToString(),
                                            GioVao = timeVaoStr,
                                            GioRa = reader["ThoiGianRa"]?.ToString() ?? ""
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                if (GridCheckInList != null) GridCheckInList.ItemsSource = listCheckIn;
                int currentPresent = listCheckIn.Count(x => string.IsNullOrEmpty(x.GioRa));
                if (txtTotalCheckInList != null) txtTotalCheckInList.Text = $"Đang hiện diện: {currentPresent}";
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải danh sách: " + ex.Message); }
        }

        // DỮ LIỆU GÓI TẬP
        private void TaiTopGoiTap()
        {
            if (cboTopGoiTapTime == null) return;
            string timeMode = (cboTopGoiTapTime.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "7days";
            LayKhoangThoiGian(timeMode, out DateTime startDate, out DateTime endDate, out bool _);

            var listTop = new List<TopGoiTapInfo>();
            string[] colors = { "#FF7043", "#66BB6A", "#42A5F5", "#FFA726", "#EF5350" };

            try
            {
                var rawDataList = new List<string>();

                using (var connection = TruyCapDB.TaoKetNoi())
                {
                    string sqlTop = @"SELECT GT.TenGoi, HD.NgayBatDau FROM HopDong HD JOIN GoiTap GT ON HD.MaGoi = GT.MaGoi";

                    using (var cmd = new SqliteCommand(sqlTop, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dateStr = reader["NgayBatDau"].ToString();
                            if (DateTime.TryParseExact(dateStr, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime realDate))
                            {
                                if (realDate.Date >= startDate.Date && realDate.Date <= endDate.Date)
                                {
                                    rawDataList.Add(reader["TenGoi"].ToString());
                                }
                            }
                        }
                    }
                }

                var groupedData = rawDataList.GroupBy(x => x)
                                             .Select(g => new { TenGoi = g.Key, SoLuong = g.Count() })
                                             .OrderByDescending(x => x.SoLuong)
                                             .Take(5).ToList();

                int rank = 1;
                foreach (var item in groupedData)
                {
                    listTop.Add(new TopGoiTapInfo
                    {
                        Rank = rank.ToString("00"),
                        TenGoi = item.TenGoi,
                        SoLuong = item.SoLuong,
                        Color = colors[(rank - 1) % colors.Length]
                    });
                    rank++;
                }
            }
            catch { }

            if (listTop.Count == 0) listTop.Add(new TopGoiTapInfo { Rank = "--", TenGoi = "(Chưa có dữ liệu)", SoLuong = 0, Color = "#BDBDBD" });
            if (ListTopGoiTap != null) ListTopGoiTap.ItemsSource = listTop;
        }

        private void TaiDuLieuPhanCuoi()
        {
            TaiTopGoiTap();

            var displayList = new List<HoatDongInfo>();

            DateTime simulatedNow = new DateTime(2025, 11, 14, 23, 59, 59);

            try
            {
                using (var connection = TruyCapDB.TaoKetNoi())
                {
                    string sql = @"
                        SELECT T.HoTen, T.MaTK, 'vừa đăng nhập hệ thống' AS NoiDung, L.ThoiGian
                        FROM LichSuDangNhap L
                        JOIN TaiKhoan T ON L.MaTK = T.MaTK

                        UNION ALL

                        SELECT IFNULL(T.HoTen, 'Hệ Thống') AS HoTen, 
                               IFNULL(T.MaTK, 'SYS') AS MaTK, 
                               N.NoiDung, 
                               N.ThoiGian
                        FROM NhatKyHeThong N
                        LEFT JOIN TaiKhoan T ON N.MaTK = T.MaTK
                    ";

                    var tempList = new List<dynamic>();

                    string[] formats = {
                        "d/M/yyyy HH:mm:ss", "d/M/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm",
                        "d-M-yyyy HH:mm:ss", "d-M-yyyy HH:mm", "dd-MM-yyyy HH:mm:ss", "dd-MM-yyyy HH:mm",
                        "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"
                    };

                    using (var cmd = new SqliteCommand(sql, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string rawTime = reader["ThoiGian"]?.ToString() ?? "";

                            string cleanTime = rawTime.Replace("-", "/").Trim();

                            DateTime dt;

                            if (DateTime.TryParseExact(cleanTime, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                            {
                                if (dt <= simulatedNow)
                                {
                                    tempList.Add(new
                                    {
                                        HoTen = reader["HoTen"]?.ToString(),
                                        MaTK = reader["MaTK"]?.ToString(),
                                        NoiDung = reader["NoiDung"]?.ToString(),
                                        ThoiGianChuan = dt
                                    });
                                }
                            }
                        }
                    }

                    var sortedList = tempList.OrderByDescending(x => x.ThoiGianChuan).Take(50);

                    foreach (var item in sortedList)
                    {
                        displayList.Add(new HoatDongInfo
                        {
                            NguoiThucHien = $"{item.HoTen} (#{item.MaTK})",
                            HanhDong = " " + item.NoiDung,
                            ThoiGian = item.ThoiGianChuan.ToString("dd/MM/yyyy HH:mm")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải hoạt động: " + ex.Message);
            }

            if (ListHoatDong != null) ListHoatDong.ItemsSource = displayList;
        }

        private void TheKPI_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null || border.Tag == null) return;
            string tag = border.Tag.ToString();
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.ChuyenTab(tag);
            }
        }
        private void BieuDoCot_DataHover(object sender, ChartPoint chartPoint)
        {
            if (HoverSection != null)
            {
                HoverSection.Visibility = Visibility.Visible;
                HoverSection.Value = chartPoint.X - 0.5;
            }
            var pixelPoint = ChartDoanhThu.ConvertToPixels(new Point(chartPoint.X, chartPoint.Y));
            txtColDate.Text = ChartLabels.Length > (int)chartPoint.X ? ChartLabels[(int)chartPoint.X] : "";
            txtColMoney.Text = chartPoint.Y.ToString("N0");
            ColumnTooltip.Visibility = Visibility.Visible;
            ColumnTooltip.UpdateLayout();
            Canvas.SetLeft(ColumnTooltip, pixelPoint.X - (ColumnTooltip.ActualWidth / 2));
            Canvas.SetTop(ColumnTooltip, pixelPoint.Y - ColumnTooltip.ActualHeight - 10);
        }

        private void BieuDoCot_MouseLeave(object sender, MouseEventArgs e)
        {
            if (HoverSection != null) HoverSection.Visibility = Visibility.Hidden;
            if (ColumnTooltip != null) ColumnTooltip.Visibility = Visibility.Collapsed;
        }

        private void KhungBieuDoTron_MouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(TooltipCanvas);
            double x = point.X + 15;
            double y = point.Y + 15;
            if (x + MouseTooltip.ActualWidth > TooltipCanvas.ActualWidth) x = point.X - MouseTooltip.ActualWidth - 5;
            if (y + MouseTooltip.ActualHeight > TooltipCanvas.ActualHeight) y = point.Y - MouseTooltip.ActualHeight - 5;
            Canvas.SetLeft(MouseTooltip, x);
            Canvas.SetTop(MouseTooltip, y);
        }

        private void BieuDoTron_DataHover(object sender, ChartPoint chartPoint)
        {
            var series = chartPoint.SeriesView as PieSeries;
            if (series == null) return;
            ttTitle.Text = series.Title;
            ttTitle.Foreground = series.Fill;
            ttPercent.Text = $"Tỷ lệ: {chartPoint.Participation:P1}";
            ttValue.Text = $"Tổng tiền: {chartPoint.Y:N0}";
            MouseTooltip.Visibility = Visibility.Visible;
        }

        private void BieuDoTron_MouseLeave(object sender, MouseEventArgs e) => MouseTooltip.Visibility = Visibility.Collapsed;

        private void BoLocBieuDo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cboChartChiNhanh == null || cboChartThoiGian == null || ChartDoanhThu == null) return;
            if (ChartDoanhThu.Visibility == Visibility.Visible) TaiBieuDoCot(); else TaiBieuDoTron();
        }

        private void BoLocCheckIn_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cboCheckInChiNhanh == null || cboCheckInBuoi == null || ChartCheckInLine == null) return;
            if (currentCheckInTab == "Chart") TaiBieuDoCheckIn(); else TaiDanhSachCheckIn();
        }

        private void TopGoiTap_FilterChanged(object sender, SelectionChangedEventArgs e) => TaiTopGoiTap();

        private string currentTab = "DoanhThu";
        private void ChuyenBieuDo_Click(object sender, MouseButtonEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;
            if (btn.Name == "btnTabDoanhThu")
            {
                currentTab = "DoanhThu";
                ThietLapTab(txtTabDoanhThu, indDoanhThu, true);
                ThietLapTab(txtTabTyTrong, indTyTrong, false);
                ChartDoanhThu.Visibility = Visibility.Visible;
                pnlTotalBottom.Visibility = Visibility.Visible;
                cboChartChiNhanh.Visibility = Visibility.Visible;
                if (grpTyTrong != null) grpTyTrong.Visibility = Visibility.Collapsed;
                TaiBieuDoCot();
            }
            else
            {
                currentTab = "TyTrong";
                ThietLapTab(txtTabTyTrong, indTyTrong, true);
                ThietLapTab(txtTabDoanhThu, indDoanhThu, false);
                ChartDoanhThu.Visibility = Visibility.Collapsed;
                pnlTotalBottom.Visibility = Visibility.Collapsed;
                cboChartChiNhanh.Visibility = Visibility.Collapsed;
                if (grpTyTrong != null) grpTyTrong.Visibility = Visibility.Visible;
                TaiBieuDoTron();
            }
        }

        private string currentCheckInTab = "Chart";
        private void ChuyenTabCheckIn_Click(object sender, MouseButtonEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;
            if (btn.Tag.ToString() == "Chart")
            {
                currentCheckInTab = "Chart";
                ThietLapTab(txtTabCheckInChart, indCheckInChart, true);
                ThietLapTab(txtTabCheckInList, indCheckInList, false);
                ChartCheckInLine.Visibility = Visibility.Visible;
                GridCheckInList.Visibility = Visibility.Collapsed;
                if (pnlTotalCheckInChart != null) pnlTotalCheckInChart.Visibility = Visibility.Visible;
                if (txtTotalCheckInList != null) txtTotalCheckInList.Visibility = Visibility.Collapsed;
                TaiBieuDoCheckIn();
            }
            else
            {
                currentCheckInTab = "List";
                ThietLapTab(txtTabCheckInList, indCheckInList, true);
                ThietLapTab(txtTabCheckInChart, indCheckInChart, false);
                ChartCheckInLine.Visibility = Visibility.Collapsed;
                GridCheckInList.Visibility = Visibility.Visible;
                if (pnlTotalCheckInChart != null) pnlTotalCheckInChart.Visibility = Visibility.Collapsed;
                if (txtTotalCheckInList != null) txtTotalCheckInList.Visibility = Visibility.Visible;
                TaiDanhSachCheckIn();
            }
        }

        private void TabBieuDo_MouseEnter(object sender, MouseEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;
            if (btn.Name == "btnTabDoanhThu" && currentTab != "DoanhThu") { txtTabDoanhThu.Foreground = Brushes.Gray; indDoanhThu.Fill = Brushes.LightGray; indDoanhThu.Visibility = Visibility.Visible; }
            else if (btn.Name == "btnTabTyTrong" && currentTab != "TyTrong") { txtTabTyTrong.Foreground = Brushes.Gray; indTyTrong.Fill = Brushes.LightGray; indTyTrong.Visibility = Visibility.Visible; }
        }

        private void TabBieuDo_MouseLeave(object sender, MouseEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;
            if (btn.Name == "btnTabDoanhThu" && currentTab != "DoanhThu") { txtTabDoanhThu.Foreground = Brushes.Black; indDoanhThu.Visibility = Visibility.Hidden; }
            else if (btn.Name == "btnTabTyTrong" && currentTab != "TyTrong") { txtTabTyTrong.Foreground = Brushes.Black; indTyTrong.Visibility = Visibility.Hidden; }
        }

        private void TabCheckIn_MouseEnter(object sender, MouseEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;
            if (btn.Name == "btnTabCheckInChart" && currentCheckInTab != "Chart") { txtTabCheckInChart.Foreground = Brushes.Gray; indCheckInChart.Fill = Brushes.LightGray; indCheckInChart.Visibility = Visibility.Visible; }
            else if (btn.Name == "btnTabCheckInList" && currentCheckInTab != "List") { txtTabCheckInList.Foreground = Brushes.Gray; indCheckInList.Fill = Brushes.LightGray; indCheckInList.Visibility = Visibility.Visible; }
        }

        private void TabCheckIn_MouseLeave(object sender, MouseEventArgs e)
        {
            var btn = sender as Border;
            if (btn == null) return;
            if (btn.Name == "btnTabCheckInChart" && currentCheckInTab != "Chart") { txtTabCheckInChart.Foreground = Brushes.Black; indCheckInChart.Visibility = Visibility.Hidden; }
            else if (btn.Name == "btnTabCheckInList" && currentCheckInTab != "List") { txtTabCheckInList.Foreground = Brushes.Black; indCheckInList.Visibility = Visibility.Hidden; }
        }

        private void ThietLapTab(TextBlock txt, System.Windows.Shapes.Rectangle ind, bool isActive)
        {
            if (isActive)
            {
                txt.Foreground = (Brush)new BrushConverter().ConvertFrom("#D32F2F");
                txt.FontWeight = FontWeights.ExtraBold;
                ind.Fill = (Brush)new BrushConverter().ConvertFrom("#D32F2F");
                ind.Visibility = Visibility.Visible;
            }
            else
            {
                txt.Foreground = Brushes.Black;
                txt.FontWeight = FontWeights.SemiBold;
                ind.Visibility = Visibility.Hidden;
            }
        }

        private void BieuDoCheckIn_DataHover(object sender, ChartPoint chartPoint)
        {
            var pixelPoint = ChartCheckInLine.ConvertToPixels(new Point(chartPoint.X, chartPoint.Y));
            txtCheckInTime.Text = $"Giờ: {(CheckInLabels.Length > (int)chartPoint.X ? CheckInLabels[(int)chartPoint.X] : "")}";
            txtCheckInNumber.Text = chartPoint.Y.ToString();
            CheckInTooltip.Visibility = Visibility.Visible;
            CheckInTooltip.UpdateLayout();
            Canvas.SetLeft(CheckInTooltip, pixelPoint.X - (CheckInTooltip.ActualWidth / 2));
            Canvas.SetTop(CheckInTooltip, pixelPoint.Y - CheckInTooltip.ActualHeight - 15);
        }

        private void BieuDoCheckIn_MouseLeave(object sender, MouseEventArgs e)
        {
            if (CheckInTooltip != null) CheckInTooltip.Visibility = Visibility.Collapsed;
        }

    }
}