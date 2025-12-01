using System;
using System.IO;
using System.Windows;
using System.ComponentModel;
using Microsoft.Data.Sqlite;
using System.Text.RegularExpressions;

namespace TFitnessApp.Windows
{
    public partial class Window3 : Window, INotifyPropertyChanged
    {
        private string _chuoiKetNoi;
        private string _hoTen;
        private string _tenDangNhap;
        private string _email;
        private string _sdt;
        private string _phanQuyen;
        private string _trangThai;

        public string HoTen
        {
            get => _hoTen;
            set { _hoTen = value; OnPropertyChanged(nameof(HoTen)); }
        }

        public string TenDangNhap
        {
            get => _tenDangNhap;
            set { _tenDangNhap = value; OnPropertyChanged(nameof(TenDangNhap)); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        public string SDT
        {
            get => _sdt;
            set { _sdt = value; OnPropertyChanged(nameof(SDT)); }
        }

        public string PhanQuyen
        {
            get => _phanQuyen;
            set { _phanQuyen = value; OnPropertyChanged(nameof(PhanQuyen)); }
        }

        public string TrangThai
        {
            get => _trangThai;
            set { _trangThai = value; OnPropertyChanged(nameof(TrangThai)); }
        }

        public Window3()
        {
            InitializeComponent();

            // Khởi tạo chuỗi kết nối
            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            _chuoiKetNoi = $"Data Source={duongDanDB};";

            this.DataContext = this;

        }

        private void Header_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private string TaoMaTaiKhoan()
        {
            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM TaiKhoan";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        int count = result != null ? Convert.ToInt32(result) : 0;
                        return $"TK{count + 1:000}";
                    }
                }
            }
            catch (Exception)
            {
                return "TK001";
            }
        }

        private string TaoMatKhau()
        {
            Random random = new Random();
            string matKhau;
            bool trung;

            do
            {
                int soNgauNhien = random.Next(100000, 999999); // Số 6 chữ số
                matKhau = $"TF@{soNgauNhien}";
                trung = KiemTraMatKhauTrung(matKhau);
            } while (trung);

            return matKhau;
        }

        private bool KiemTraMatKhauTrung(string matKhau)
        {
            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();
                    string query = "SELECT COUNT(1) FROM TaiKhoan WHERE MatKhau = @MatKhau";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MatKhau", matKhau);
                        var result = command.ExecuteScalar();
                        int count = result != null ? Convert.ToInt32(result) : 0;
                        return count > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool KiemTraEmailHopLe(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, pattern);
            }
            catch
            {
                return false;
            }
        }

        private bool KiemTraSoDienThoaiHopLe(string soDienThoai)
        {
            if (string.IsNullOrWhiteSpace(soDienThoai))
                return false;

            // Kiểm tra số điện thoại Việt Nam (10-11 số, bắt đầu bằng 0)
            string pattern = @"^0\d{9,10}$";
            return Regex.IsMatch(soDienThoai, pattern);
        }

        private void TaoTaiKhoanButton_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra dữ liệu
            if (string.IsNullOrEmpty(HoTen))
            {
                MessageBox.Show("Vui lòng nhập họ tên!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtHoTen.Focus();
                return;
            }

            if (string.IsNullOrEmpty(TenDangNhap))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTenDangNhap.Focus();
                return;
            }

            if (string.IsNullOrEmpty(Email))
            {
                MessageBox.Show("Vui lòng nhập email!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            if (!KiemTraEmailHopLe(Email))
            {
                MessageBox.Show("Email không hợp lệ! Vui lòng nhập đúng định dạng email.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            if (string.IsNullOrEmpty(SDT))
            {
                MessageBox.Show("Vui lòng nhập số điện thoại!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSoDienThoai.Focus();
                return;
            }

            if (!KiemTraSoDienThoaiHopLe(SDT))
            {
                MessageBox.Show("Số điện thoại không hợp lệ! Vui lòng nhập số điện thoại Việt Nam (10-11 số).", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSoDienThoai.Focus();
                return;
            }

            // Kiểm tra tên đăng nhập đã tồn tại chưa
            if (KiemTraTonTai("TaiKhoan", "TenDangNhap", TenDangNhap))
            {
                MessageBox.Show("Tên đăng nhập đã tồn tại! Vui lòng chọn tên đăng nhập khác.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTenDangNhap.Focus();
                return;
            }

            // Kiểm tra email đã tồn tại chưa
            if (KiemTraTonTai("TaiKhoan", "Email", Email))
            {
                MessageBox.Show("Email đã tồn tại! Vui lòng sử dụng email khác.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            // Kiểm tra số điện thoại đã tồn tại chưa
            if (KiemTraTonTai("TaiKhoan", "SDT", SDT))
            {
                MessageBox.Show("Số điện thoại đã tồn tại! Vui lòng sử dụng số điện thoại khác.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSoDienThoai.Focus();
                return;
            }

            // Thêm tài khoản mới
            try
            {
                // Tạo mã tài khoản và mật khẩu
                string maTaiKhoan = TaoMaTaiKhoan();
                string matKhau = TaoMatKhau();

                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO TaiKhoan (MaTK, HoTen, PhanQuyen, TenDangNhap, MatKhau, Email, SDT, NgayTao, TrangThai)
                        VALUES (@MaTK, @HoTen, @PhanQuyen, @TenDangNhap, @MatKhau, @Email, @SDT, @NgayTao, @TrangThai)";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaTK", maTaiKhoan);
                        command.Parameters.AddWithValue("@HoTen", HoTen);
                        command.Parameters.AddWithValue("@PhanQuyen", PhanQuyen);
                        command.Parameters.AddWithValue("@TenDangNhap", TenDangNhap);
                        command.Parameters.AddWithValue("@MatKhau", matKhau);
                        command.Parameters.AddWithValue("@Email", Email);
                        command.Parameters.AddWithValue("@SDT", SDT);
                        command.Parameters.AddWithValue("@NgayTao", DateTime.Now.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@TrangThai", TrangThai);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"Tạo tài khoản thành công!\n\nMã tài khoản: {maTaiKhoan}\nMật khẩu: {matKhau}\n\nVui lòng ghi nhớ thông tin đăng nhập!",
                                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Tạo tài khoản thất bại!", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo tài khoản: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool KiemTraTonTai(string tableName, string columnName, string value)
        {
            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();
                    string query = $"SELECT COUNT(1) FROM {tableName} WHERE {columnName} = @Value";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Value", value);
                        var result = command.ExecuteScalar();
                        int count = result != null ? Convert.ToInt32(result) : 0;
                        return count > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}