using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents; // QUAN TRỌNG: Thêm thư viện này
using System.Windows.Media;

namespace TFitnessApp
{
    public partial class BaoCaoDoanhThuPage : Page
    {
        public BaoCaoDoanhThuPage()
        {
            InitializeComponent();
        }

        private void ChartCard_Loaded(object sender, RoutedEventArgs e) { }
        private void ChartCard_Loaded_1(object sender, RoutedEventArgs e) { }
        private void CartesianChart_Loaded(object sender, RoutedEventArgs e) { }
        private void CartesianChart_Loaded_1(object sender, RoutedEventArgs e) { }
        private void ChartCard_Loaded_2(object sender, RoutedEventArgs e) { }

        private void BtnDuDoan_Click(object sender, RoutedEventArgs e)
        {
            string csvPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "datahuanluyen.csv");
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");

            txtKetQuaDuDoan.Text = "⏳ Đang huấn luyện và so sánh 4 mô hình AI...";
            txtKetQuaDuDoan.Foreground = Brushes.Gray;
            ChartDuBao.Visibility = Visibility.Collapsed;

            Task.Run(() =>
            {
                try
                {
                    // 1. Lấy dữ liệu thực tế từ DB (Năm 2025)
                    var historyMap = GetHistoryDataFromDB(dbPath, 2025);

                    // 2. Chạy AI
                    var aiReport = DoanhThuPredictor.TrainAndPredict(csvPath);

                    Dispatcher.Invoke(() =>
                    {
                        // --- A. XỬ LÝ HIỂN THỊ TEXT KẾT QUẢ (TÔ MÀU) ---
                        if (!string.IsNullOrEmpty(aiReport.LogText) && (aiReport.ModelResults == null || aiReport.ModelResults.Count == 0))
                        {
                            txtKetQuaDuDoan.Text = aiReport.LogText;
                            txtKetQuaDuDoan.Foreground = Brushes.Red;
                            return;
                        }

                        // Xóa text cũ, dùng Inlines để add từng dòng màu khác nhau
                        txtKetQuaDuDoan.Inlines.Clear();
                        txtKetQuaDuDoan.Foreground = Brushes.Black; // Mặc định màu đen

                        txtKetQuaDuDoan.Inlines.Add(new Run("📊 KẾT QUẢ SO SÁNH HIỆU SUẤT:\n\n") { FontWeight = FontWeights.Bold, FontSize = 16 });

                        foreach (var r in aiReport.ModelResults)
                        {
                            bool isBest = (r.ModelName == aiReport.BestModelName);

                            // CHỌN MÀU: Nếu Best thì Đỏ (#C71A1B), còn lại Đen
                            var color = isBest ? (SolidColorBrush)new BrushConverter().ConvertFrom("#C71A1B") : Brushes.Black;
                            var weight = isBest ? FontWeights.Bold : FontWeights.Normal;
                            string icon = isBest ? "🏆 " : "   "; // Icon text

                            // Dòng 1: Tên model và độ chính xác
                            string line1 = $"{icon}{r.ModelName.PadRight(15)} | Độ chính xác: {r.Accuracy:F2}%\n";
                            if (!string.IsNullOrEmpty(r.Note)) line1 = $"❌ {r.ModelName}: {r.Note}\n";

                            var run1 = new Run(line1) { Foreground = color, FontWeight = weight, FontSize = 15 };
                            txtKetQuaDuDoan.Inlines.Add(run1);

                            if (string.IsNullOrEmpty(r.Note))
                            {
                                // Dòng 2 & 3: Kết quả dự báo
                                string line2 = $"      ➡ T1/2026: {r.ForecastT1:N0} đ\n";
                                string line3 = $"      ➡ T2/2026: {r.ForecastT2:N0} đ\n";

                                txtKetQuaDuDoan.Inlines.Add(new Run(line2) { Foreground = color, FontWeight = weight });
                                txtKetQuaDuDoan.Inlines.Add(new Run(line3) { Foreground = color, FontWeight = weight });
                            }

                            txtKetQuaDuDoan.Inlines.Add(new Run("--------------------------------------------------\n") { Foreground = Brushes.LightGray });
                        }

                        // --- B. VẼ BIỂU ĐỒ (LOGIC NHƯ CŨ) ---
                        if (historyMap.Count > 0 && aiReport.ForecastData.Count > 0)
                        {
                            ChartDuBao.Visibility = Visibility.Visible;
                            var labels = new List<string>();
                            var realValues = new ChartValues<double>();
                            var forecastValues = new ChartValues<double>();

                            foreach (var kvp in historyMap)
                            {
                                labels.Add($"T{kvp.Key}");
                                realValues.Add(kvp.Value);
                                forecastValues.Add(double.NaN);
                            }

                            // Nối dây
                            double lastVal = realValues.Last();
                            forecastValues[forecastValues.Count - 1] = lastVal;

                            labels.Add("T1/2026");
                            realValues.Add(double.NaN);
                            forecastValues.Add(aiReport.ForecastT1);

                            labels.Add("T2/2026");
                            realValues.Add(double.NaN);
                            forecastValues.Add(aiReport.ForecastT2);

                            var lineReal = new LineSeries
                            {
                                Title = "Thực tế 2025",
                                Values = realValues,
                                Stroke = Brushes.SteelBlue,
                                Fill = Brushes.Transparent,
                                PointGeometrySize = 10,
                                DataLabels = true
                            };

                            var lineForecast = new LineSeries
                            {
                                Title = $"Dự báo ({aiReport.BestModelName})",
                                Values = forecastValues,
                                Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#C71A1B"), // Đỏ theo yêu cầu
                                Fill = Brushes.Transparent,
                                PointGeometry = DefaultGeometries.Diamond,
                                PointGeometrySize = 12,
                                StrokeDashArray = new DoubleCollection { 4 },
                                DataLabels = true,
                                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#C71A1B")
                            };

                            ChartDuBao.Series = new SeriesCollection { lineReal, lineForecast };
                            AxisXDuBao.Labels = labels.ToArray();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => {
                        txtKetQuaDuDoan.Text = "Lỗi: " + ex.Message;
                        txtKetQuaDuDoan.Foreground = Brushes.Red;
                    });
                }
            });
        }

        private Dictionary<int, double> GetHistoryDataFromDB(string dbPath, int year)
        {
            var data = new Dictionary<int, double>();
            for (int i = 1; i <= 12; i++) data[i] = 0;

            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = @"
                    SELECT GD.NgayGD, CAST(GD.DaThanhToan AS REAL) as TongTien 
                    FROM GiaoDich GD
                    LEFT JOIN HopDong HD ON GD.MaGoi = HD.MaGoi AND GD.MaHV = HD.MaHV
                    LEFT JOIN ChiNhanh CN ON HD.MaCN = CN.MaCN
                    WHERE 1=1";

                using (var cmd = new SqliteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string dateStr = reader["NgayGD"].ToString();
                        double money = Convert.ToDouble(reader["TongTien"]);
                        if (TryGetDate(dateStr, out DateTime date))
                        {
                            if (date.Year == year) data[date.Month] += money;
                        }
                    }
                }
            }
            return data;
        }

        private bool TryGetDate(string dateStr, out DateTime date)
        {
            string[] formats = { "dd/MM/yyyy", "yyyy-MM-dd", "dd/MM/yyyy HH:mm", "yyyy-MM-dd HH:mm:ss" };
            return DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
    }
}