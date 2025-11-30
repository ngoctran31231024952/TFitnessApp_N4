using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TFitnessApp.Windows
{
    /// <summary>
    /// Interaction logic for Window2.xaml (Chi tiết Giao dịch)
    /// </summary>
    public partial class Window2 : Window
    {
        public Window2()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Xử lý sự kiện đóng cửa sổ khi click vào nút X (Close button).
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        // Các phương thức xử lý sự kiện khác (nếu cần)
        
        private void btnSua_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng Sửa đang được thực hiện...", "Thông báo");
        }
        
        private void btnXoa_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng Xóa đang được thực hiện...", "Thông báo");
        }
        
        private void btnXuat_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng Xuất đang được thực hiện...", "Thông báo");
        }
    }
}