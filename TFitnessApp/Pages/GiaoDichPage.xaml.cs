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
using TFitnessApp.Interfaces;
using TFitnessApp.Repositories;
using TFitnessApp.Entities;

namespace TFitnessApp
{
    /// <summary>
    /// Interaction logic for GiaoDichPage.xaml
    /// </summary>
    public partial class GiaoDichPage : Page
    {
        // Khai báo Repository
        private readonly IGiaoDichRepository _giaoDichRepository;
        public GiaoDichPage()
        {
            InitializeComponent();
            // Khởi tạo Repository
            _giaoDichRepository = new GiaoDichRepository();

            // Thêm sự kiện Loaded để tải dữ liệu khi Page được hiển thị
            this.Loaded += GiaoDichPage_Loaded;
        }
        // Sự kiện khi Page được tải xong
        private async void GiaoDichPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadGiaoDichAsync(); // Tải dữ liệu bất đồng bộ
        }

        /// <summary>
        /// Tải dữ liệu giao dịch và gán cho DataGrid.
        /// </summary>
        private async Task LoadGiaoDichAsync()
        {
            // Hiển thị loading (nếu có UI loading)
            // ...

            try
            {
                // Thực thi tác vụ lấy dữ liệu DB trên một Thread nền (Task.Run)
                // để tránh chặn luồng UI, vì phương thức GetAll() là đồng bộ
                List<GiaoDich> giaoDichList = await Task.Run(() => _giaoDichRepository.GetAll());

                // Gán danh sách lấy được làm nguồn dữ liệu cho DataGrid
                // Giả định DataGrid của bạn trong XAML có tên là 'dgGiaoDich'
                if (dgGiaoDich != null)
                {
                    dgGiaoDich.ItemsSource = giaoDichList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu giao dịch: {ex.Message}", "Lỗi Tải Dữ Liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Ẩn loading (nếu có UI loading)
                // ...
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Khởi tạo cửa sổ mới
                Windows.Window1 themGiaoDichWindow = new Windows.Window1();

                // Mở cửa sổ dưới dạng Modal, chặn tương tác với cửa sổ cha
                themGiaoDichWindow.ShowDialog();
            }
            catch (System.Exception ex)
            {
                // Xử lý lỗi nếu không tìm thấy hoặc không thể tạo cửa sổ
                MessageBox.Show($"Lỗi khi mở cửa sổ giao dịch: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
