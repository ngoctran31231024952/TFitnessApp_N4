using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TFitnessApp
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private string _dbPath;

        // --- 1. DỮ LIỆU DANH SÁCH (ComboBox) ---
        public ObservableCollection<string> ListTenChiNhanh { get; set; }

        // --- 2. CÁC BIẾN BỘ LỌC ---
        private string _filterChiNhanh = "Tất cả";
        private int _filterLoaiBieuDo = 1; // 1: Đường (Line), 0: Cột (Column)
        // Mở rộng mặc định 60 ngày để chắc chắn có dữ liệu test
        private DateTime _filterTuNgay = DateTime.Today.AddDays(-60);
        private DateTime _filterDenNgay = DateTime.Today;

        public string FilterChiNhanh
        {
            get => _filterChiNhanh;
            set { _filterChiNhanh = value; OnPropertyChanged(nameof(FilterChiNhanh)); ReloadData(); }
        }

        public int FilterLoaiBieuDo
        {
            get => _filterLoaiBieuDo;
            set { _filterLoaiBieuDo = value; OnPropertyChanged(nameof(FilterLoaiBieuDo)); ReloadData(); }
        }

        public DateTime FilterTuNgay
        {
            get => _filterTuNgay;
            set { _filterTuNgay = value; OnPropertyChanged(nameof(FilterTuNgay)); ReloadData(); }
        }

        public DateTime FilterDenNgay
        {
            get => _filterDenNgay;
            set { _filterDenNgay = value; OnPropertyChanged(nameof(FilterDenNgay)); ReloadData(); }
        }

        // --- 3. DỮ LIỆU BIỂU ĐỒ ---
        public SeriesCollection DSDuongTangTruong { get; set; }
        public string[] NhanNgayThang { get; set; }

        public SeriesCollection DSTronDoanhThu { get; set; }

        public SeriesCollection DSTronCN { get; set; }
        public string TongDoanhThu { get; set; }

        public SeriesCollection DSHangCN { get; set; }
        public string[] NhanTenCN { get; set; }

        public SeriesCollection DSHangKH { get; set; }
        public string[] NhanTenKH { get; set; }

        public Func<double, string> FormatterTien { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public DashboardViewModel()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            _dbPath = Path.Combine(folder, "Database", "TFitness.db");
            if (!File.Exists(_dbPath)) _dbPath = Path.Combine(folder, "TFitness.db");

            FormatterTien = val => val >= 1000000 ? $"{val / 1000000:N1}M" : $"{val:N0}";

            // Init Collections
            ListTenChiNhanh = new ObservableCollection<string>();
            DSDuongTangTruong = new SeriesCollection();
            DSTronDoanhThu = new SeriesCollection();
            DSTronCN = new SeriesCollection();
            DSHangCN = new SeriesCollection();
            DSHangKH = new SeriesCollection();

            LoadDanhSachChiNhanh();
            ReloadData();
        }

        private void LoadDanhSachChiNhanh()
        {
            ListTenChiNhanh.Clear();
            ListTenChiNhanh.Add("Tất cả");
            try
            {
                using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    conn.Open();
                    using (var cmd = new SqliteCommand("SELECT TenCN FROM ChiNhanh ORDER BY TenCN", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) ListTenChiNhanh.Add(reader["TenCN"].ToString());
                    }
                }
            }
            catch { }
        }

        private async void ReloadData()
        {
            await Task.Run(() =>
            {
                var tu = _filterTuNgay;
                var den = _filterDenNgay;
                var cn = _filterChiNhanh;
                var loaiBD = _filterLoaiBieuDo;

                try
                {
                    LoadDoanhThuTangTruong(tu, den, cn, loaiBD);
                    LoadTyLeGoiTap(tu, den, cn);
                    LoadTyTrongChiNhanh(tu, den);
                    LoadTopXepHang(tu, den, cn);
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}"));
                }
            });
        }

        // --- 1. BIỂU ĐỒ TĂNG TRƯỞNG (FIX LỖI RỖNG) ---
        private void LoadDoanhThuTangTruong(DateTime tu, DateTime den, string cn, int loaiBD)
        {
            // Tạo map chứa tất cả các ngày trong khoảng, mặc định giá trị 0
            // Điều này đảm bảo biểu đồ luôn có điểm dữ liệu liên tục, không bị gãy
            var dataMap = new Dictionary<DateTime, double>();
            for (var d = tu.Date; d <= den.Date; d = d.AddDays(1))
            {
                dataMap[d] = 0;
            }

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                // Sử dụng LEFT JOIN để không mất dữ liệu nếu thiếu thông tin phụ
                string sql = @"
                    SELECT GD.NgayGD, CAST(GD.DaThanhToan AS REAL) as TongTien 
                    FROM GiaoDich GD
                    LEFT JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV
                    LEFT JOIN ChiNhanh CN ON HD.MaCN = CN.MaCN
                    WHERE 1=1 ";

                if (cn != "Tất cả") sql += " AND CN.TenCN = @tenCN";

                using (var cmd = new SqliteCommand(sql, conn))
                {
                    if (cn != "Tất cả") cmd.Parameters.AddWithValue("@tenCN", cn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (TryGetDate(reader["NgayGD"].ToString(), out DateTime date))
                            {
                                if (date.Date >= tu.Date && date.Date <= den.Date)
                                {
                                    // Cộng dồn vào ngày tương ứng
                                    if (dataMap.ContainsKey(date.Date))
                                        dataMap[date.Date] += Convert.ToDouble(reader["TongTien"]);
                                }
                            }
                        }
                    }
                }
            }

            var sortedData = dataMap.OrderBy(x => x.Key).ToList();
            var values = new ChartValues<double>(sortedData.Select(x => x.Value));
            var labels = sortedData.Select(x => x.Key.ToString("dd/MM")).ToArray();

            Application.Current.Dispatcher.Invoke(() =>
            {
                DSDuongTangTruong.Clear();

                // Logic chọn loại biểu đồ: 0 là Cột, 1 là Đường (Theo ComboBox)
                // Lưu ý: ComboBox index 0 là Cột, 1 là Đường
                if (loaiBD == 0) // Cột
                {
                    DSDuongTangTruong.Add(new ColumnSeries
                    {
                        Title = "Doanh thu",
                        Values = values,
                        Fill = Brushes.IndianRed,
                        DataLabels = false,
                        LabelPoint = p => p.Y > 0 ? p.Y.ToString("N0") : ""
                    });
                }
                else // Đường
                {
                    DSDuongTangTruong.Add(new LineSeries
                    {
                        Title = "Doanh thu",
                        Values = values,
                        Stroke = Brushes.IndianRed,
                        Fill = Brushes.Transparent, // Không tô màu nền dưới đường
                        PointGeometrySize = 10,
                        DataLabels = false
                    });
                }

                NhanNgayThang = labels;
                OnPropertyChanged(nameof(NhanNgayThang));
            });
        }

        // --- 2. BIỂU ĐỒ TRÒN GÓI TẬP (FIX HIỆN DỮ LIỆU) ---
        private void LoadTyLeGoiTap(DateTime tu, DateTime den, string cn)
        {
            var goiTapMap = new Dictionary<string, double>();

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                // Dùng LEFT JOIN
                string sql = @"
                    SELECT GT.TenGoi, CAST(GD.DaThanhToan AS REAL) as TongTien, GD.NgayGD
                    FROM GiaoDich GD
                    LEFT JOIN GoiTap GT ON GD.MaGoi = GT.MaGoi
                    LEFT JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV
                    LEFT JOIN ChiNhanh CN ON HD.MaCN = CN.MaCN
                    WHERE 1=1 ";

                if (cn != "Tất cả") sql += " AND CN.TenCN = @tenCN";

                using (var cmd = new SqliteCommand(sql, conn))
                {
                    if (cn != "Tất cả") cmd.Parameters.AddWithValue("@tenCN", cn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (TryGetDate(reader["NgayGD"].ToString(), out DateTime date) && date >= tu && date <= den)
                            {
                                // Xử lý nếu tên gói null (dữ liệu lỗi)
                                string tenGoi = reader["TenGoi"]?.ToString();
                                if (string.IsNullOrEmpty(tenGoi)) tenGoi = "Khác";

                                double tien = Convert.ToDouble(reader["TongTien"]);
                                if (goiTapMap.ContainsKey(tenGoi)) goiTapMap[tenGoi] += tien;
                                else goiTapMap.Add(tenGoi, tien);
                            }
                        }
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                DSTronDoanhThu.Clear();
                var colors = new[] { "#2ecc71", "#3498db", "#f1c40f", "#e67e22", "#9b59b6", "#e74c3c" };
                int i = 0;
                // Chỉ lấy những gói có doanh thu > 0
                foreach (var item in goiTapMap.Where(x => x.Value > 0).OrderByDescending(x => x.Value))
                {
                    DSTronDoanhThu.Add(new PieSeries
                    {
                        Title = item.Key,
                        Values = new ChartValues<double> { item.Value },
                        Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(colors[i % colors.Length]),
                        DataLabels = true,
                        LabelPoint = p => p.Y.ToString("N0")
                    });
                    i++;
                }
            });
        }

        // --- 3. BIỂU ĐỒ TRÒN CHI NHÁNH ---
        private void LoadTyTrongChiNhanh(DateTime tu, DateTime den)
        {
            var cnMap = new Dictionary<string, double>();
            double total = 0;

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                string sql = @"
                    SELECT CN.TenCN, CAST(GD.DaThanhToan AS REAL) as TongTien, GD.NgayGD
                    FROM GiaoDich GD
                    LEFT JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV
                    LEFT JOIN ChiNhanh CN ON HD.MaCN = CN.MaCN";

                using (var cmd = new SqliteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (TryGetDate(reader["NgayGD"].ToString(), out DateTime date) && date >= tu && date <= den)
                        {
                            string ten = reader["TenCN"]?.ToString();
                            if (string.IsNullOrEmpty(ten)) ten = "Không xác định";

                            double tien = Convert.ToDouble(reader["TongTien"]);
                            if (cnMap.ContainsKey(ten)) cnMap[ten] += tien;
                            else cnMap.Add(ten, tien);
                            total += tien;
                        }
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                DSTronCN.Clear();
                var colors = new[] { "#ff7675", "#55efc4", "#ffeaa7", "#74b9ff", "#fdcb6e" };
                int i = 0;
                foreach (var item in cnMap.Where(x => x.Value > 0))
                {
                    DSTronCN.Add(new PieSeries
                    {
                        Title = item.Key,
                        Values = new ChartValues<double> { item.Value },
                        Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(colors[i % colors.Length]),
                        DataLabels = true
                    });
                    i++;
                }
                TongDoanhThu = total.ToString("N0");
                OnPropertyChanged(nameof(TongDoanhThu));
            });
        }

        // --- 4. TOP 10 (FIX SẮP XẾP GIẢM DẦN) ---
        private void LoadTopXepHang(DateTime tu, DateTime den, string cn)
        {
            var topCN = new Dictionary<string, double>();
            var topKH = new Dictionary<string, double>();

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                string sql = @"
                    SELECT CN.TenCN, HV.HoTen, CAST(GD.DaThanhToan AS REAL) as Tien, GD.NgayGD
                    FROM GiaoDich GD
                    LEFT JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV
                    LEFT JOIN ChiNhanh CN ON HD.MaCN = CN.MaCN
                    LEFT JOIN HocVien HV ON GD.MaHV = HV.MaHV
                    WHERE 1=1 ";

                if (cn != "Tất cả") sql += " AND CN.TenCN = @tenCN";

                using (var cmd = new SqliteCommand(sql, conn))
                {
                    if (cn != "Tất cả") cmd.Parameters.AddWithValue("@tenCN", cn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (TryGetDate(reader["NgayGD"].ToString(), out DateTime date) && date >= tu && date <= den)
                            {
                                double tien = Convert.ToDouble(reader["Tien"]);
                                string tenCN = reader["TenCN"]?.ToString() ?? "N/A";
                                string tenKH = reader["HoTen"]?.ToString() ?? "Khách lẻ";

                                if (topCN.ContainsKey(tenCN)) topCN[tenCN] += tien; else topCN.Add(tenCN, tien);
                                if (topKH.ContainsKey(tenKH)) topKH[tenKH] += tien; else topKH.Add(tenKH, tien);
                            }
                        }
                    }
                }
            }

            // [FIX SẮP XẾP]: 
            // 1. OrderByDescending: Lấy Top cao nhất (VD: 100, 90, 80...)
            // 2. Take(10): Chỉ lấy 10 ông đầu
            // 3. OrderBy (Tăng dần): Để khi vẽ lên RowSeries (vẽ từ dưới lên), giá trị to nhất sẽ nằm trên cùng.

            var listTopCN = topCN.OrderByDescending(x => x.Value).Take(10)
                                 .OrderBy(x => x.Value) // Đảo lại để vẽ đúng thứ tự trên biểu đồ
                                 .ToList();

            var listTopKH = topKH.OrderByDescending(x => x.Value).Take(10)
                                 .OrderBy(x => x.Value) // Đảo lại
                                 .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Top CN
                DSHangCN.Clear();
                DSHangCN.Add(new RowSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(listTopCN.Select(x => x.Value)),
                    Fill = Brushes.DodgerBlue,
                    DataLabels = false,
                    LabelPoint = p => p.X.ToString("N0")
                });
                NhanTenCN = listTopCN.Select(x => x.Key).ToArray();
                OnPropertyChanged(nameof(NhanTenCN));

                // Top KH
                DSHangKH.Clear();
                DSHangKH.Add(new RowSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(listTopKH.Select(x => x.Value)),
                    Fill = Brushes.Orange,
                    DataLabels = false,
                    LabelPoint = p => p.X.ToString("N0")
                });
                NhanTenKH = listTopKH.Select(x => x.Key).ToArray();
                OnPropertyChanged(nameof(NhanTenKH));
            });
        }

        private bool TryGetDate(string dateStr, out DateTime date)
        {
            string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy" };
            return DateTime.TryParseExact(dateStr, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date);
        }
    }
}