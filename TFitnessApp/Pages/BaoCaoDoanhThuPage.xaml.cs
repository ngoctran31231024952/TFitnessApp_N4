using LiveCharts;
using LiveCharts.Wpf;
using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TFitnessApp
{
    /// <summary>
    /// Interaction logic for BaoCaoDoanhThuPage.xaml
    /// </summary>
    public partial class BaoCaoDoanhThuPage : Page
    {
        public BaoCaoDoanhThuPage()
        {
            InitializeComponent();
        }

        private void ChartCard_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ChartCard_Loaded_1(object sender, RoutedEventArgs e)
        {

        }

        private void CartesianChart_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void CartesianChart_Loaded_1(object sender, RoutedEventArgs e)
        {

        }

        private void ChartCard_Loaded_2(object sender, RoutedEventArgs e)
        {

        }
        // Hàm xử lý nút bấm (Code bên trong vẫn giữ nguyên)
        private void BtnDuDoan_Click(object sender, RoutedEventArgs e)
        {
            string csvPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "datahuanluyen.csv");

            txtKetQuaDuDoan.Text = "⏳ Đang huấn luyện 4 mô hình & phân tích...";
            txtKetQuaDuDoan.Foreground = Brushes.Gray;
            BorderBieuDoDuBao.Visibility = Visibility.Collapsed; // Ẩn biểu đồ cũ

            Task.Run(() =>
            {
                // Gọi hàm dự báo mới trả về ForecastReport
                var report = DoanhThuPredictor.TrainAndPredict(csvPath);

                Dispatcher.Invoke(() =>
                {
                    txtKetQuaDuDoan.Text = report.LogText;

                    if (report.HistoryData != null && report.HistoryData.Count > 0)
                    {
                        txtKetQuaDuDoan.Foreground = Brushes.Green;

                        // --- VẼ BIỂU ĐỒ ---
                        BorderBieuDoDuBao.Visibility = Visibility.Visible;

                        // 1. Dữ liệu Lịch sử (Năm 2025) - Màu Xanh/Xám
                        var lineHistory = new LineSeries
                        {
                            Title = "Thực tế 2025",
                            Values = new ChartValues<double>(report.HistoryData),
                            Stroke = Brushes.Gray,
                            Fill = Brushes.Transparent,
                            PointGeometrySize = 10,
                            DataLabels = true
                        };

                        // 2. Dữ liệu Dự báo (Nối tiếp) - Màu Đỏ Nổi Bật
                        // Tạo mảng values cho dự báo: [null...null, T12, T1, T2] để nối dây
                        var forecastValues = new ChartValues<double>();
                        // Điền null cho 11 tháng đầu để không vẽ
                        for (int i = 0; i < 11; i++) forecastValues.Add(double.NaN);

                        // Thêm 3 điểm: T12(Nối), T1(Dự), T2(Dự)
                        foreach (var val in report.ForecastData) forecastValues.Add(val);

                        var lineForecast = new LineSeries
                        {
                            Title = "Dự báo AI 2026",
                            Values = forecastValues,
                            Stroke = Brushes.Red,
                            Fill = Brushes.Transparent,
                            PointGeometry = DefaultGeometries.Diamond,
                            PointGeometrySize = 12,
                            StrokeDashArray = new DoubleCollection { 4 }, // Nét đứt
                            DataLabels = true,
                            Foreground = Brushes.Red
                        };

                        // Cập nhật Chart
                        ChartDuBao.Series = new SeriesCollection { lineHistory, lineForecast };
                        AxisXDuBao.Labels = report.Labels;
                    }
                    else
                    {
                        txtKetQuaDuDoan.Foreground = Brushes.Red;
                    }
                });
            });
        }
    }
}
