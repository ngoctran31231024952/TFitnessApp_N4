using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using TFitnessApp.Database;

namespace TFitnessApp.Windows
{
    public partial class LoginWindow : Window
    {
        // KHAI BÁO BIẾN & HÀM HỖ TRỢ
        private string _ChuoiKetNoi;
        private readonly TruyCapDB _dbAccess;
        private string MaHoaSHA256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public LoginWindow()
        {
            // KHỞI TẠO WINDOW & CẤU HÌNH DB
            InitializeComponent();
            try { SQLitePCL.Batteries_V2.Init(); } catch { }
            _dbAccess = new TruyCapDB();
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;
            txtTenDangNhap.Focus();
        }

        // CÁC CHỨC NĂNG CHÍNH
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void btnThoat_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnDangNhap_Click(object sender, RoutedEventArgs e)
        {
            string username = txtTenDangNhap.Text.Trim();
            string password = txtMatKhau.Password;
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(username))
            {
                errTenDangNhap.Visibility = Visibility.Visible;
                hasError = true;
            }
            else errTenDangNhap.Visibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(password))
            {
                errMatKhau.Visibility = Visibility.Visible;
                hasError = true;
            }
            else errMatKhau.Visibility = Visibility.Collapsed;

            if (hasError) return;


            string passwordHash = MaHoaSHA256(password);

            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");

            using (SqliteConnection connection = TruyCapDB.TaoKetNoi())
            {
                connection.Open();

                string sql = "SELECT HoTen, PhanQuyen, TrangThai FROM TaiKhoan WHERE TenDangNhap = @user AND MatKhau = @pass";

                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@user", username);

                    command.Parameters.AddWithValue("@pass", passwordHash);

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) 
                        {
                            string trangThai = reader["TrangThai"].ToString();

                            if (trangThai == "Bị Khóa")
                            {
                                MessageBox.Show("Tài khoản của bạn đã bị khóa!\nVui lòng liên hệ quản trị viên: admin@tfitness.vn",
                                                "Thông báo",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Stop);
                                return;
                            }

                            string hoTenDB = reader["HoTen"].ToString();
                            string quyenDB = reader["PhanQuyen"].ToString();

                            MainWindow mainWin = new MainWindow(hoTenDB, quyenDB);
                            mainWin.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Sai tên đăng nhập hoặc mật khẩu!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
        }

        // XỬ LÝ GIAO DIỆN NHẬP LIỆU
        private void txtTaiKhoan_TextChanged(object sender, TextChangedEventArgs e)
            => errTenDangNhap.Visibility = Visibility.Collapsed;

        private void txtMatKhau_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                var placeholder = (TextBlock)passwordBox.Template.FindName("Placeholder", passwordBox);
                if (placeholder != null)
                {
                    placeholder.Visibility = string.IsNullOrEmpty(passwordBox.Password)
                                             ? Visibility.Visible : Visibility.Collapsed;
                }

                if (txtHienMatKhau != null)
                    txtHienMatKhau.Text = passwordBox.Password;

                if (errMatKhau != null)
                    errMatKhau.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnMat_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtMatKhau.Visibility = Visibility.Collapsed;
            txtHienMatKhau.Visibility = Visibility.Visible;
        }

        private void BtnMat_MouseUp(object sender, MouseEventArgs e)
        {
            txtHienMatKhau.Visibility = Visibility.Collapsed;
            txtMatKhau.Visibility = Visibility.Visible;
            txtMatKhau.Focus();
        }

        // CHỨC NĂNG POPUP HƯỚNG DẪN
        private void lblHuongDan_Click(object sender, MouseButtonEventArgs e)
        {
            popupHuongDan.HorizontalOffset = 10;
            popupHuongDan.VerticalOffset = -20;
            popupHuongDan.IsOpen = true;
            LopPhu.Visibility = Visibility.Visible;
        }

        private void BtnDongPopup_Click(object sender, MouseButtonEventArgs e)
        {
            popupHuongDan.IsOpen = false;
            LopPhu.Visibility = Visibility.Collapsed;
        }

        private bool isDragging = false;
        private Point startMousePos;
        private double startHOffset, startVOffset;

        private void Popup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement border)
            {
                isDragging = true;
                startMousePos = border.PointToScreen(e.GetPosition(border));
                startHOffset = popupHuongDan.HorizontalOffset;
                startVOffset = popupHuongDan.VerticalOffset;
                border.CaptureMouse();
            }
        }

        private void Popup_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && sender is UIElement border)
            {
                Point currentMousePos = border.PointToScreen(e.GetPosition(border));
                popupHuongDan.HorizontalOffset = startHOffset + (currentMousePos.X - startMousePos.X);
                popupHuongDan.VerticalOffset = startVOffset + (currentMousePos.Y - startMousePos.Y);
            }
        }

        private void Popup_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            (sender as UIElement)?.ReleaseMouseCapture();
        }
    }
}