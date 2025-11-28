using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TFitnessApp.Windows // Lưu ý namespace có thể là TFitnessApp hoặc TFitnessApp.Windows tùy cấu trúc thư mục của em
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtUsername.Focus();
        }

        // Kéo thả cửa sổ
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // Nút tắt
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Xử lý Đăng nhập
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim(); // Trim để xóa khoảng trắng thừa
            string password = txtPassword.Password;
            bool hasError = false;

            // 1. Validate Username
            if (string.IsNullOrEmpty(username))
            {
                errUserContainer.Visibility = Visibility.Visible;
                hasError = true;
            }
            else
            {
                errUserContainer.Visibility = Visibility.Collapsed;
            }

            // 2. Validate Password
            if (string.IsNullOrEmpty(password))
            {
                errPassContainer.Visibility = Visibility.Visible;
                hasError = true;
            }
            else
            {
                errPassContainer.Visibility = Visibility.Collapsed;
            }

            // Nếu có lỗi thì dừng lại, không xử lý đăng nhập
            if (hasError) return;

            // --- LOGIC ĐĂNG NHẬP CŨ CỦA BẠN ---
            if (username == "admin" && password == "123")
            {
                // ... code chuyển màn hình ...
                MainWindow mainWin = new MainWindow();
                mainWin.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Sai tên đăng nhập hoặc mật khẩu!", "Lỗi đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Khi gõ vào ô Username thì ẩn lỗi đi
        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (errUserContainer != null)
                errUserContainer.Visibility = Visibility.Collapsed;
        }

        // Xử lý Placeholder Mật khẩu
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // ... (Code xử lý Placeholder cũ giữ nguyên) ...
            var passwordBox = sender as PasswordBox;
            if (passwordBox == null) return;

            // Code placeholder cũ...
            var template = passwordBox.Template;
            var placeholder = (TextBlock)template.FindName("Placeholder", passwordBox);
            if (placeholder != null)
            {
                placeholder.Visibility = string.IsNullOrEmpty(passwordBox.Password)
                                         ? Visibility.Visible : Visibility.Collapsed;
            }

            // Code đồng bộ text cũ...
            if (txtVisiblePassword != null)
            {
                txtVisiblePassword.Text = passwordBox.Password;
            }

            // [MỚI] Ẩn thông báo lỗi khi bắt đầu nhập mật khẩu
            if (errPassContainer != null)
                errPassContainer.Visibility = Visibility.Collapsed;
        }

        // 1. Khi NHẤN GIỮ chuột trái vào con mắt
        private void BtnEye_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Copy mật khẩu từ ô chấm tròn sang ô chữ thường
            txtVisiblePassword.Text = txtPassword.Password;

            // Ẩn ô chấm tròn, hiện ô chữ thường
            txtPassword.Visibility = Visibility.Collapsed;
            txtVisiblePassword.Visibility = Visibility.Visible;
        }

        // 2. Khi THẢ chuột ra (hoặc di chuột ra ngoài icon)
        private void BtnEye_MouseUp(object sender, MouseEventArgs e)
        {
            // Ẩn ô chữ thường, hiện lại ô chấm tròn
            txtVisiblePassword.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;

            // Trả lại focus cho ô password để gõ tiếp được ngay
            txtPassword.Focus();
        }
        // 1. Sự kiện khi bấm vào dòng chữ "Xem hướng dẫn"
        private void txtHelp_Click(object sender, MouseButtonEventArgs e)
        {
            HelpPopup.HorizontalOffset = 10;
            HelpPopup.VerticalOffset = -20;
            OverlayMask.Visibility = Visibility.Visible;
            // Lệnh quan trọng nhất: Mở Popup lên
            HelpPopup.IsOpen = true;
        }

        // 2. Sự kiện khi bấm vào dấu X nhỏ trong Popup
        private void BtnClosePopup_Click(object sender, MouseButtonEventArgs e)
        {
            OverlayMask.Visibility = Visibility.Collapsed;
            HelpPopup.IsOpen = false;
        }

        // --- KHAI BÁO BIẾN TOÀN CỤC (Để lưu trạng thái ban đầu) ---
        private bool isDragging = false;
        private Point startMousePos;      // Vị trí chuột trên màn hình lúc bắt đầu nhấn
        private double startHOffset;      // Độ lệch ngang ban đầu của Popup
        private double startVOffset;      // Độ lệch dọc ban đầu của Popup

        // 1. NHẤN CHUỘT (Lưu mốc tọa độ)
        private void Popup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as UIElement;
            if (border != null)
            {
                isDragging = true;

                // Quan trọng: Lấy tọa độ tuyệt đối trên màn hình Desktop
                startMousePos = border.PointToScreen(e.GetPosition(border));

                // Lưu vị trí hiện tại của Popup
                startHOffset = HelpPopup.HorizontalOffset;
                startVOffset = HelpPopup.VerticalOffset;

                border.CaptureMouse(); // Giữ chuột không cho tuột
            }
        }

        // 2. DI CHUỘT (Tính toán khoảng cách di chuyển)
        private void Popup_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                var border = sender as UIElement;
                if (border != null)
                {
                    // Lấy tọa độ chuột hiện tại trên màn hình
                    Point currentMousePos = border.PointToScreen(e.GetPosition(border));

                    // Tính khoảng cách chuột đã đi được (Delta)
                    double deltaX = currentMousePos.X - startMousePos.X;
                    double deltaY = currentMousePos.Y - startMousePos.Y;

                    // Cập nhật vị trí Popup dựa trên vị trí gốc + khoảng cách đi được
                    // Cách này không cộng dồn sai số nên cực kỳ mượt
                    HelpPopup.HorizontalOffset = startHOffset + deltaX;
                    HelpPopup.VerticalOffset = startVOffset + deltaY;
                }
            }
        }

        // 3. THẢ CHUỘT (Kết thúc)
        private void Popup_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            var border = sender as UIElement;
            if (border != null)
            {
                border.ReleaseMouseCapture();
            }
        }
    }
}