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
        private string _ChuoiKetNoi;
        private readonly TruyCapDB _dbAccess;
        private string CalculateSHA256(string rawData)
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
            InitializeComponent();
            try { SQLitePCL.Batteries_V2.Init(); } catch { }
            _dbAccess = new TruyCapDB();
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;

            txtUsername.Focus();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(username))
            {
                errUserContainer.Visibility = Visibility.Visible;
                hasError = true;
            }
            else errUserContainer.Visibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(password))
            {
                errPassContainer.Visibility = Visibility.Visible;
                hasError = true;
            }
            else errPassContainer.Visibility = Visibility.Collapsed;

            if (hasError) return;


            string passwordHash = CalculateSHA256(password);

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

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
            => errUserContainer.Visibility = Visibility.Collapsed;

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                var placeholder = (TextBlock)passwordBox.Template.FindName("Placeholder", passwordBox);
                if (placeholder != null)
                {
                    placeholder.Visibility = string.IsNullOrEmpty(passwordBox.Password)
                                             ? Visibility.Visible : Visibility.Collapsed;
                }

                if (txtVisiblePassword != null)
                    txtVisiblePassword.Text = passwordBox.Password;

                if (errPassContainer != null)
                    errPassContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnEye_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPassword.Visibility = Visibility.Collapsed;
            txtVisiblePassword.Visibility = Visibility.Visible;
        }

        private void BtnEye_MouseUp(object sender, MouseEventArgs e)
        {
            txtVisiblePassword.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;
            txtPassword.Focus();
        }
        private void txtHelp_Click(object sender, MouseButtonEventArgs e)
        {
            HelpPopup.HorizontalOffset = 10;
            HelpPopup.VerticalOffset = -20;
            HelpPopup.IsOpen = true;
            OverlayMask.Visibility = Visibility.Visible;
        }

        private void BtnClosePopup_Click(object sender, MouseButtonEventArgs e)
        {
            HelpPopup.IsOpen = false;
            OverlayMask.Visibility = Visibility.Collapsed;
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
                startHOffset = HelpPopup.HorizontalOffset;
                startVOffset = HelpPopup.VerticalOffset;
                border.CaptureMouse();
            }
        }

        private void Popup_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && sender is UIElement border)
            {
                Point currentMousePos = border.PointToScreen(e.GetPosition(border));
                HelpPopup.HorizontalOffset = startHOffset + (currentMousePos.X - startMousePos.X);
                HelpPopup.VerticalOffset = startVOffset + (currentMousePos.Y - startMousePos.Y);
            }
        }

        private void Popup_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            (sender as UIElement)?.ReleaseMouseCapture();
        }
    }
}