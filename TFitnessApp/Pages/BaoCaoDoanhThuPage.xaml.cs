using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Windows;
using System.Windows.Controls;
using System.IO;

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
            // Đường dẫn tuyệt đối tới file CSV
            // Tự động tìm đường dẫn từ thư mục chạy ứng dụng
            string csvPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "datahuanluyen.csv");

            txtKetQuaDuDoan.Text = "⏳ Đang phân tích dữ liệu và huấn luyện mô hình AI... Vui lòng đợi.";
            txtKetQuaDuDoan.Foreground = System.Windows.Media.Brushes.Gray;

            System.Threading.Tasks.Task.Run(() =>
            {
                // Gọi hàm dự báo (Không cần chỉ định namespace vì cùng namespace cha)
                string ketQua = DoanhThuPredictor.TrainAndPredict(csvPath);

                Dispatcher.Invoke(() =>
                {
                    txtKetQuaDuDoan.Text = ketQua;

                    if (ketQua.Contains("Lỗi") || ketQua.Contains("xảy ra"))
                        txtKetQuaDuDoan.Foreground = System.Windows.Media.Brushes.Red;
                    else
                        txtKetQuaDuDoan.Foreground = System.Windows.Media.Brushes.Green;
                });
            });
        }
    }
}
