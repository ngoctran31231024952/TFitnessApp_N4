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
        private DateTime _filterTuNgay = DateTime.Today.AddDays(-30);
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

        // --- 3. DỮ LIỆU THỐNG KÊ KINH DOANH (DOANH SỐ) ---
        private string _tongDoanhThuFormatted;
        private string _doanhSoGiaHanCuFormatted;
        private string _doanhSoMoiFormatted;
        private double _tongDoanhThuValue;
        private double _doanhSoGiaHanCuValue;
        private double _doanhSoMoiValue;

        public string TongDoanhThuFormatted
        {
            get => _tongDoanhThuFormatted;
            set { _tongDoanhThuFormatted = value; OnPropertyChanged(nameof(TongDoanhThuFormatted)); }
        }

        public string DoanhSoGiaHanCuFormatted
        {
            get => _doanhSoGiaHanCuFormatted;
            set { _doanhSoGiaHanCuFormatted = value; OnPropertyChanged(nameof(DoanhSoGiaHanCuFormatted)); }
        }

        public string DoanhSoMoiFormatted
        {
            get => _doanhSoMoiFormatted;
            set { _doanhSoMoiFormatted = value; OnPropertyChanged(nameof(DoanhSoMoiFormatted)); }
        }

        public double TongDoanhThuValue
        {
            get => _tongDoanhThuValue;
            set { _tongDoanhThuValue = value; OnPropertyChanged(nameof(TongDoanhThuValue)); }
        }

        public double DoanhSoGiaHanCuValue
        {
            get => _doanhSoGiaHanCuValue;
            set { _doanhSoGiaHanCuValue = value; OnPropertyChanged(nameof(DoanhSoGiaHanCuValue)); }
        }

        public double DoanhSoMoiValue
        {
            get => _doanhSoMoiValue;
            set { _doanhSoMoiValue = value; OnPropertyChanged(nameof(DoanhSoMoiValue)); }
        }

        public string TongDoanhThuUnit { get; set; }
        public string DoanhSoGiaHanCuUnit { get; set; }
        public string DoanhSoMoiUnit { get; set; }

        // --- 4. DỮ LIỆU BIỂU ĐỒ ---
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
        private double _doanhSoGiaHanCuPercent;
        private double _doanhSoMoiPercent;

        public double DoanhSoGiaHanCuPercent
        {
            get => _doanhSoGiaHanCuPercent;
            set { _doanhSoGiaHanCuPercent = value; OnPropertyChanged(nameof(DoanhSoGiaHanCuPercent)); }
        }

        public double DoanhSoMoiPercent
        {
            get => _doanhSoMoiPercent;
            set { _doanhSoMoiPercent = value; OnPropertyChanged(nameof(DoanhSoMoiPercent)); }
        }
        public class GoiTapDetail
        {
            public string TenGoi { get; set; }
            public double DoanhThu { get; set; }
            public double PhanTram { get; set; }
        }
        
        public DashboardViewModel()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            _dbPath = Path.Combine(folder, "Database", "TFitness.db");
            if (!File.Exists(_dbPath)) _dbPath = Path.Combine(folder, "TFitness.db");

            FormatterTien = val => val.ToString("N0");

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
                    LoadThongTinKinhDoanh(tu, den, cn);
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
        private (string GiaTri, string DonVi) DinhDangTien(double soTien)
        {
            if (soTien >= 1000000000) // Ngưỡng Tỷ
                return ((soTien / 1000000000).ToString("0.###"), "tỷ VNĐ");

            if (soTien >= 1000000) // Ngưỡng Triệu
                return ((soTien / 1000000).ToString("0.###"), "triệu VNĐ");

            // Ngưỡng VNĐ (dưới 1 triệu)
            return (soTien.ToString("N0"), "VNĐ");
        }

        // --- THÊM: LOAD THÔNG TIN KINH DOANH (ĐÃ SỬA - XÓA ĐIỀU KIỆN TrangThai) ---
        private void LoadThongTinKinhDoanh(DateTime tu, DateTime den, string cn)
        {
            double tongDoanhThu = 0;
            double doanhSoGiaHanCu = 0;
            double doanhSoMoi = 0;

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                // Lấy dữ liệu phẳng để C# tự tính toán, đảm bảo không sót dòng nào (LEFT JOIN)
                string sql = @"
            SELECT 
                GD.NgayGD, 
                GD.DaThanhToan, 
                HD.LoaiHopDong,
                GD.TrangThai
            FROM GiaoDich GD
            LEFT JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV
            LEFT JOIN ChiNhanh CN ON HD.MaCN = CN.MaCN
            WHERE 1=1";

                if (cn != "Tất cả") sql += " AND CN.TenCN = @tenCN";

                using (var cmd = new SqliteCommand(sql, conn))
                {
                    if (cn != "Tất cả") cmd.Parameters.AddWithValue("@tenCN", cn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Kiểm tra ngày tháng
                            if (TryGetDate(reader["NgayGD"].ToString(), out DateTime date)
                                && date.Date >= tu.Date && date.Date <= den.Date)
                            {
                                double soTien = Convert.ToDouble(reader["DaThanhToan"]);
                                string loaiHD = reader["LoaiHopDong"]?.ToString();

                                // Cộng vào tổng doanh thu (Không sót dòng nào)
                                tongDoanhThu += soTien;

                                // Phân loại doanh số (Dựa trên logic của bạn)
                                if (loaiHD == "Gia Hạn")
                                    doanhSoGiaHanCu += soTien;
                                else if (loaiHD == "Mới")
                                    doanhSoMoi += soTien;
                            }
                        }
                    }
                }
            }



            Application.Current.Dispatcher.Invoke(() =>
            {
                // 1. Định dạng Tổng doanh thu
                var kqTong = DinhDangTien(tongDoanhThu);
                TongDoanhThuFormatted = kqTong.GiaTri;
                TongDoanhThuUnit = kqTong.DonVi;

                // 2. Định dạng Gia hạn gói cũ
                var kqGiaHan = DinhDangTien(doanhSoGiaHanCu);
                DoanhSoGiaHanCuFormatted = kqGiaHan.GiaTri;
                DoanhSoGiaHanCuUnit = kqGiaHan.DonVi;

                // 3. Định dạng Mua gói mới
                var kqMoi = DinhDangTien(doanhSoMoi);
                DoanhSoMoiFormatted = kqMoi.GiaTri;
                DoanhSoMoiUnit = kqMoi.DonVi;

                // 4. Thông báo cập nhật giao diện
                OnPropertyChanged(nameof(TongDoanhThuFormatted));
                OnPropertyChanged(nameof(TongDoanhThuUnit));
                OnPropertyChanged(nameof(DoanhSoGiaHanCuFormatted));
                OnPropertyChanged(nameof(DoanhSoGiaHanCuUnit));
                OnPropertyChanged(nameof(DoanhSoMoiFormatted));
                OnPropertyChanged(nameof(DoanhSoMoiUnit));
            });
        }
        
        // --- 1. BIỂU ĐỒ TĂNG TRƯỞNG ---
        private void LoadDoanhThuTangTruong(DateTime tu, DateTime den, string cn, int loaiBD)
        {
            var dataMap = new Dictionary<DateTime, double>();
            for (var d = tu.Date; d <= den.Date; d = d.AddDays(1))
            {
                dataMap[d] = 0;
            }

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                // KHÔNG CÓ ĐIỀU KIỆN TrangThai
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

                if (loaiBD == 0) // Cột
                {
                    DSDuongTangTruong.Add(new ColumnSeries
                    {
                        Title = "Doanh thu",
                        Values = values,
                        Fill = Brushes.IndianRed,
                        DataLabels = false
                    });
                }
                else // Đường
                {
                    DSDuongTangTruong.Add(new LineSeries
                    {
                        Title = "Doanh thu",
                        Values = values,
                        Stroke = Brushes.IndianRed,
                        Fill = Brushes.Transparent,
                        PointGeometrySize = 10,
                        DataLabels = false
                    });
                }

                NhanNgayThang = labels;
                OnPropertyChanged(nameof(NhanNgayThang));
            });
        }


        // --- 2. BIỂU ĐỒ TRÒN GÓI TẬP ---

        private void LoadTyLeGoiTap(DateTime tu, DateTime den, string cn)
        {
            var goiTapMap = new Dictionary<string, double>();

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                string sql = @"
            SELECT GT.TenGoi, CAST(GD.DaThanhToan AS REAL) as TongTien, GD.NgayGD
            FROM GiaoDich GD
            LEFT JOIN GoiTap GT ON GD.MaGoi = GT.MaGoi
            LEFT JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV
            LEFT JOIN ChiNhanh CN ON HD.MaCN = CN.MaCN
            WHERE 1=1";

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
                                string tenGoi = reader["TenGoi"]?.ToString();
                                if (string.IsNullOrEmpty(tenGoi)) tenGoi = "Khác";

                                tenGoi = FormatLabel(tenGoi);
                                double tien = Convert.ToDouble(reader["TongTien"]);

                                if (goiTapMap.ContainsKey(tenGoi))
                                    goiTapMap[tenGoi] += tien;
                                else
                                    goiTapMap.Add(tenGoi, tien);
                            }
                        }
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                DSTronDoanhThu.Clear();

                // Sắp xếp tất cả gói tập theo doanh thu giảm dần
                var allSorted = goiTapMap
                    .Where(x => x.Value > 0)
                    .OrderByDescending(x => x.Value)
                    .ToList();

                double tongTatCa = allSorted.Sum(x => x.Value);
                if (tongTatCa == 0) return; // Không có dữ liệu

                // Lấy top 5
                var top5GoiTap = allSorted.Take(5).ToList();

                // Phần còn lại
                var otherGoiTap = allSorted.Skip(5).ToList();
                double doanhThuKhac = otherGoiTap.Sum(x => x.Value);

                // Màu sắc
                var colors = new[] {
            "#F8B648", "#02973A", "#FF8080", "#C71A1B", "#fdcb6e",
            "#D0D0D0"
        };

                // Thêm top 5
                for (int i = 0; i < top5GoiTap.Count; i++)
                {
                    var item = top5GoiTap[i];
                    double phanTram = (item.Value / tongTatCa) * 100;
                    var dinhDangTien = DinhDangTien(item.Value);

                    DSTronDoanhThu.Add(new PieSeries
                    {
                        Title = item.Key, // Chỉ hiển thị tên
                        Values = new ChartValues<double> { item.Value },
                        Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(colors[i % colors.Length]),
                        DataLabels = false,
                        // Tooltip khi hover
                        ToolTip = $"{item.Key}\n{dinhDangTien.GiaTri} {dinhDangTien.DonVi}\n{phanTram:0.0}%"
                    });
                }

                // Thêm phần "Khác" nếu có
                if (doanhThuKhac > 0)
                {
                    double phanTramKhac = (doanhThuKhac / tongTatCa) * 100;
                    var dinhDangKhac = DinhDangTien(doanhThuKhac);

                    // Tạo tooltip chi tiết cho phần "Khác"
                    string tooltip = $"Khác\n{dinhDangKhac.GiaTri} {dinhDangKhac.DonVi}\n{phanTramKhac:0.0}%\n\nCác gói tập khác:\n";
                    foreach (var item in otherGoiTap)
                    {
                        double phanTramItem = (item.Value / tongTatCa) * 100;
                        var dinhDangItem = DinhDangTien(item.Value);
                        tooltip += $"- {item.Key}: {dinhDangItem.GiaTri} {dinhDangItem.DonVi} ({phanTramItem:0.0}%)\n";
                    }

                    var otherSeries = new PieSeries
                    {
                        Title = "Khác", // Chỉ hiển thị "Khác"
                        Values = new ChartValues<double> { doanhThuKhac },
                        Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(colors[colors.Length - 1]),
                        DataLabels = false,
                        ToolTip = tooltip
                    };

                    DSTronDoanhThu.Add(otherSeries);
                }

                OnPropertyChanged(nameof(DSTronDoanhThu));
            });
        }

        // --- 3. BIỂU ĐỒ TRÒN CHI NHÁNH (ĐÃ SỬA - XÓA ĐIỀU KIỆN TrangThai) ---
        private void LoadTyTrongChiNhanh(DateTime tu, DateTime den)
        {
            var cnMap = new Dictionary<string, double>();
            double total = 0;

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                // XÓA ĐIỀU KIỆN TrangThai
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
                var colors = new[] { "#F8B648", "#02973A", "#FF8080", "#C71A1B", "#fdcb6e" };
                int i = 0;
                foreach (var item in cnMap.Where(x => x.Value > 0))
                {
                    DSTronCN.Add(new PieSeries
                    {
                        Title = item.Key,
                        Values = new ChartValues<double> { item.Value },
                        Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(colors[i % colors.Length]),
                        DataLabels = false
                    });
                    i++;
                }
                TongDoanhThu = total.ToString("N0");
                OnPropertyChanged(nameof(TongDoanhThu));
            });
        }

        // --- 4. TOP 10 (ĐÃ SỬA - XÓA ĐIỀU KIỆN TrangThai) ---
        private void LoadTopXepHang(DateTime tu, DateTime den, string cn)
        {
            var topCN = new Dictionary<string, double>();
            var topKH = new Dictionary<string, double>();

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                // XÓA ĐIỀU KIỆN TrangThai
                string sql = @"
                    SELECT CN.TenCN, HV.HoTen, CAST(GD.DaThanhToan AS REAL) as Tien, GD.NgayGD
                    FROM GiaoDich GD
                    LEFT JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV
                    LEFT JOIN ChiNhanh CN ON HD.MaCN = CN.MaCN
                    LEFT JOIN HocVien HV ON GD.MaHV = HV.MaHV
                    WHERE 1=1";

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

            var listTopCN = topCN.OrderByDescending(x => x.Value).Take(10)
                                 .OrderBy(x => x.Value)
                                 .ToList();

            var listTopKH = topKH.OrderByDescending(x => x.Value).Take(10)
                                 .OrderBy(x => x.Value)
                                 .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                DSHangCN.Clear();
                DSHangCN.Add(new RowSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(listTopCN.Select(x => x.Value)),
                    Fill = Brushes.DodgerBlue,
                    DataLabels = false
                });
                NhanTenCN = listTopCN.Select(x => x.Key).ToArray();
                OnPropertyChanged(nameof(NhanTenCN));

                DSHangKH.Clear();
                DSHangKH.Add(new RowSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(listTopKH.Select(x => x.Value)),
                    Fill = Brushes.Orange,
                    DataLabels = false
                });
                NhanTenKH = listTopKH.Select(x => x.Key).ToArray();
                OnPropertyChanged(nameof(NhanTenKH));
            });
        }

        private bool TryGetDate(string dateStr, out DateTime date)
        {
            string[] formats = {
        "dd/MM/yyyy",
        "d/M/yyyy",
        "yyyy-MM-dd",
        "MM/dd/yyyy",
        "M/d/yyyy",
        // THÊM CÁC ĐỊNH DẠNG CÓ GIỜ
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm",
        "MM/dd/yyyy HH:mm:ss",
        "MM/dd/yyyy HH:mm"
    };

            return DateTime.TryParseExact(dateStr, formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out date);
        }        

        private string FormatLabel(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "Khác";

            string lower = input.ToLowerInvariant();
            System.Globalization.TextInfo textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(lower);
        }
    }
}