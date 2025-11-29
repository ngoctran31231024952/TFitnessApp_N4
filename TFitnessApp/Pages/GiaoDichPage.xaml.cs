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
using TFitnessApp.ViewModels;

namespace TFitnessApp
{
    /// <summary>
    /// Interaction logic for GiaoDichPage.xaml
    /// </summary>
    public partial class GiaoDichPage : Page
    {
        // Khai báo ViewModel
        private readonly GiaoDichViewModel _viewModel;
        public GiaoDichPage()
        {
            InitializeComponent();
            // Khởi tạo ViewModel
            _viewModel = new GiaoDichViewModel();
            // GÁN VIEWMODEL LÀM DATA CONTEXT CHO PAGE
            this.DataContext = _viewModel;

            // THAY THẾ: Chỉ đăng ký sự kiện Loaded để gọi phương thức tải dữ liệu trong ViewModel
            this.Loaded += GiaoDichPage_Loaded;
        }
        // Sự kiện khi Page được tải xong (gọi hàm tải dữ liệu trong ViewModel)
        // Mục đích: Tải dữ liệu lần đầu khi vào Page
        private async void GiaoDichPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Dùng lệnh này để đảm bảo dữ liệu chỉ tải lại khi cần
            if (_viewModel.Transactions.Count == 0)
            {
                // Gọi phương thức tải dữ liệu từ ViewModel
                await _viewModel.LoadGiaoDichAsync();
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
