using System;
using System.IO;
using System.Windows;
using System.ComponentModel;
using Microsoft.Data.Sqlite;
using System.Text.RegularExpressions;
using TFitnessApp.Database;

namespace TFitnessApp.Windows
{
    // Lớp Window3 thực hiện giao diện INotifyPropertyChanged để hỗ trợ Data Binding
    public partial class Window3 : Window, INotifyPropertyChanged
    {
        #region Trường Dữ liệu Nội bộ
        private string _ChuoiKetNoi;
        private readonly DbAccess _dbAccess;
        private string _hoTen;
        private string _tenDangNhap;
        private string _email;
        private string _sdt;
        private string _phanQuyen;
        private string _trangThai;
        #endregion

        #region Thuộc tính Binding (Public Properties)
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
        #endregion

        #region Khởi tạo
        // Constructor của Window3
        public Window3()
        {
            InitializeComponent();

            _dbAccess = new DbAccess();
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;

            this.DataContext = this;

        }
        #endregion

        #region Xử lý Sự kiện UI
        // Sự kiện dùng để di chuyển cửa sổ (DragMove)
        private void Header_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        // Sự kiện đóng cửa sổ
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Sự kiện chính: Xử lý khi nhấn nút "Tạo Tài khoản"
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

            // Thêm tài khoản mới vào cơ sở dữ liệu
            try
            {
                // Tạo mã tài khoản và mật khẩu ngẫu nhiên
                string maTaiKhoan = TaoMaTaiKhoan();
                string matKhau = TaoMatKhau();

                using (SqliteConnection conn = DbAccess.CreateConnection())
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO TaiKhoan (MaTK, HoTen, PhanQuyen, TenDangNhap, MatKhau, Email, SDT, NgayTao, TrangThai)
                        VALUES (@MaTK, @HoTen, @PhanQuyen, @TenDangNhap, @MatKhau, @Email, @SDT, @NgayTao, @TrangThai)";

                    using (var command = new SqliteCommand(query, conn))
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
        #endregion

        #region Phương thức Tạo và Kiểm tra Dữ liệu
        // Phương thức tạo mã tài khoản dựa trên số lượng tài khoản hiện có
        private string TaoMaTaiKhoan()
        {
            try
            {
                using (SqliteConnection conn = DbAccess.CreateConnection())
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM TaiKhoan";
                    using (var command = new SqliteCommand(query, conn))
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

        // Phương thức tạo mật khẩu ngẫu nhiên không trùng lặp
        private string TaoMatKhau()
        {
            Random random = new Random();
            string matKhau;
            bool trung;

            do
            {
                int soNgauNhien = random.Next(100000, 999999);
                matKhau = $"TF@{soNgauNhien}";
                trung = KiemTraMatKhauTrung(matKhau);
            } while (trung);

            return matKhau;
        }

        // Phương thức kiểm tra mật khẩu có bị trùng trong database không
        private bool KiemTraMatKhauTrung(string matKhau)
        {
            try
            {
                using (SqliteConnection conn = DbAccess.CreateConnection())
                {
                    conn.Open();
                    string query = "SELECT COUNT(1) FROM TaiKhoan WHERE MatKhau = @MatKhau";
                    using (var command = new SqliteCommand(query, conn))
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

        // Phương thức kiểm tra định dạng email có hợp lệ không
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

        // Phương thức kiểm tra định dạng số điện thoại Việt Nam
        private bool KiemTraSoDienThoaiHopLe(string soDienThoai)
        {
            if (string.IsNullOrWhiteSpace(soDienThoai))
                return false;

            string pattern = @"^0\d{9,10}$";
            return Regex.IsMatch(soDienThoai, pattern);
        }

        // Phương thức kiểm tra sự tồn tại của một giá trị trong cột/bảng
        private bool KiemTraTonTai(string tableName, string columnName, string value)
        {
            try
            {
                using (SqliteConnection conn = DbAccess.CreateConnection())
                {
                    conn.Open();
                    string query = $"SELECT COUNT(1) FROM {tableName} WHERE {columnName} = @Value";
                    using (var command = new SqliteCommand(query, conn))
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
        #endregion

        #region Triển khai INotifyPropertyChanged
        // Triển khai sự kiện PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // Phương thức gọi sự kiện PropertyChanged khi giá trị thuộc tính thay đổi
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}