using System;
using System.IO;
using System.Data;
using System.Windows;
using System.ComponentModel;
using Microsoft.Data.Sqlite;

namespace TFitnessApp.Windows
{
    public partial class Window1 : Window, INotifyPropertyChanged
    {
        private string _chuoiKetNoi;
        private string _maHocVien;
        private string _hoTenHocVien;
        private string _maGoi;
        private string _tenGoiTap;
        private string _maNhanVien;
        private string _hoTenNhanVien;
        private decimal _tongTien;
        private decimal _daThanhToan;
        private decimal _soTienNo;
        private string _phuongThucThanhToan;
        private string _trangThaiThanhToan;
        private DateTime _ngayGiaoDich = DateTime.Now;

        public string MaHocVien
        {
            get => _maHocVien;
            set { _maHocVien = value; OnPropertyChanged(nameof(MaHocVien)); TaiThongTinHocVien(); }
        }

        public string HoTenHocVien
        {
            get => _hoTenHocVien;
            set { _hoTenHocVien = value; OnPropertyChanged(nameof(HoTenHocVien)); }
        }

        public string MaGoi
        {
            get => _maGoi;
            set { _maGoi = value; OnPropertyChanged(nameof(MaGoi)); TaiThongTinGoiTap(); }
        }

        public string TenGoiTap
        {
            get => _tenGoiTap;
            set { _tenGoiTap = value; OnPropertyChanged(nameof(TenGoiTap)); }
        }

        public string MaNhanVien
        {
            get => _maNhanVien;
            set { _maNhanVien = value; OnPropertyChanged(nameof(MaNhanVien)); TaiThongTinNhanVien(); }
        }

        public string HoTenNhanVien
        {
            get => _hoTenNhanVien;
            set { _hoTenNhanVien = value; OnPropertyChanged(nameof(HoTenNhanVien)); }
        }

        public decimal TongTien
        {
            get => _tongTien;
            set { _tongTien = value; OnPropertyChanged(nameof(TongTien)); TinhSoTienNo(); }
        }

        public decimal DaThanhToan
        {
            get => _daThanhToan;
            set { _daThanhToan = value; OnPropertyChanged(nameof(DaThanhToan)); TinhSoTienNo(); }
        }

        public decimal SoTienNo
        {
            get => _soTienNo;
            set { _soTienNo = value; OnPropertyChanged(nameof(SoTienNo)); }
        }

        public string PhuongThucThanhToan
        {
            get => _phuongThucThanhToan;
            set { _phuongThucThanhToan = value; OnPropertyChanged(nameof(PhuongThucThanhToan)); }
        }

        public string TrangThaiThanhToan
        {
            get => _trangThaiThanhToan;
            set { _trangThaiThanhToan = value; OnPropertyChanged(nameof(TrangThaiThanhToan)); }
        }

        public DateTime NgayGiaoDich
        {
            get => _ngayGiaoDich;
            set { _ngayGiaoDich = value; OnPropertyChanged(nameof(NgayGiaoDich)); }
        }

        public Window1()
        {
            InitializeComponent();

            // Khởi tạo chuỗi kết nối
            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            _chuoiKetNoi = $"Data Source={duongDanDB};";

            this.DataContext = this;

            // Set default values
            PhuongThucThanhToan = "Tiền mặt";
            TrangThaiThanhToan = "Chưa Thanh Toán";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TaiThongTinHocVien()
        {
            if (string.IsNullOrEmpty(MaHocVien))
            {
                HoTenHocVien = "";
                txtHocVienError.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();
                    string query = "SELECT HoTen FROM HocVien WHERE MaHV = @MaHV";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaHV", MaHocVien);
                        var result = command.ExecuteScalar();

                        if (result != null)
                        {
                            HoTenHocVien = result.ToString();
                            txtHocVienError.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            HoTenHocVien = "";
                            txtHocVienError.Text = "Không tồn tại mã học viên";
                            txtHocVienError.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thông tin học viên: {ex.Message}");
            }
        }

        private void TaiThongTinGoiTap()
        {
            if (string.IsNullOrEmpty(MaGoi))
            {
                TenGoiTap = "";
                TongTien = 0;
                txtGoiTapError.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();
                    string query = "SELECT TenGoi, GiaNiemYet FROM GoiTap WHERE MaGoi = @MaGoi";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaGoi", MaGoi);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                TenGoiTap = reader["TenGoi"].ToString();
                                TongTien = Convert.ToDecimal(reader["GiaNiemYet"]);
                                txtGoiTapError.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                TenGoiTap = "";
                                TongTien = 0;
                                txtGoiTapError.Text = "Không tồn tại mã gói tập";
                                txtGoiTapError.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thông tin gói tập: {ex.Message}");
            }
        }

        private void TaiThongTinNhanVien()
        {
            if (string.IsNullOrEmpty(MaNhanVien))
            {
                HoTenNhanVien = "";
                txtNhanVienError.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();
                    string query = "SELECT HoTen FROM TaiKhoan WHERE MaTK = @MaTK";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaTK", MaNhanVien);
                        var result = command.ExecuteScalar();

                        if (result != null)
                        {
                            HoTenNhanVien = result.ToString();
                            txtNhanVienError.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            HoTenNhanVien = "";
                            txtNhanVienError.Text = "Không tồn tại mã nhân viên";
                            txtNhanVienError.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thông tin nhân viên: {ex.Message}");
            }
        }

        private void TinhSoTienNo()
        {
            SoTienNo = TongTien - DaThanhToan;
        }

        private void TaoGiaoDichButton_Click(object sender, RoutedEventArgs e)
        {
            // Tính toán số tiền nợ cuối cùng trước khi lưu
            TinhSoTienNo();

            // Kiểm tra dữ liệu
            if (string.IsNullOrEmpty(MaHocVien) || string.IsNullOrEmpty(MaGoi) || string.IsNullOrEmpty(MaNhanVien))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ mã học viên, mã gói tập và mã nhân viên!");
                return;
            }

            // Kiểm tra mã có tồn tại không
            if (!KiemTraTonTai("HocVien", "MaHV", MaHocVien))
            {
                MessageBox.Show("Mã học viên không tồn tại!");
                return;
            }

            if (!KiemTraTonTai("GoiTap", "MaGoi", MaGoi))
            {
                MessageBox.Show("Mã gói tập không tồn tại!");
                return;
            }

            if (!KiemTraTonTai("TaiKhoan", "MaTK", MaNhanVien))
            {
                MessageBox.Show("Mã nhân viên không tồn tại!");
                return;
            }

            if (DaThanhToan < 0)
            {
                MessageBox.Show("Số tiền đã thanh toán không được âm!");
                return;
            }

            if (DaThanhToan > TongTien)
            {
                MessageBox.Show("Số tiền đã thanh toán không được lớn hơn tổng tiền!");
                return;
            }

            // Tạo mã giao dịch tự động
            string maGD = TaoMaGiaoDich();

            // Tạo giao dịch mới
            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO GiaoDich (MaGD, TongTien, DaThanhToan, SoTienNo, PhuongThuc, NgayGD, TrangThai, MaHV, MaGoi, MaTK)
                        VALUES (@MaGD, @TongTien, @DaThanhToan, @SoTienNo, @PhuongThuc, @NgayGD, @TrangThai, @MaHV, @MaGoi, @MaTK)";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaGD", maGD);
                        command.Parameters.AddWithValue("@TongTien", TongTien);
                        command.Parameters.AddWithValue("@DaThanhToan", DaThanhToan);
                        command.Parameters.AddWithValue("@SoTienNo", SoTienNo);
                        command.Parameters.AddWithValue("@PhuongThuc", PhuongThucThanhToan);
                        command.Parameters.AddWithValue("@NgayGD", NgayGiaoDich.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@TrangThai", TrangThaiThanhToan);
                        command.Parameters.AddWithValue("@MaHV", MaHocVien);
                        command.Parameters.AddWithValue("@MaGoi", MaGoi);
                        command.Parameters.AddWithValue("@MaTK", MaNhanVien);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"Tạo giao dịch thành công!\nMã giao dịch: {maGD}");
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Tạo giao dịch thất bại!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo giao dịch: {ex.Message}");
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

        private string TaoMaGiaoDich()
        {
            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();
                    string query = "SELECT MaGD FROM GiaoDich ORDER BY MaGD DESC LIMIT 1";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();

                        if (result == null)
                        {
                            return "GD0001";
                        }

                        string lastMa = result.ToString();
                        if (lastMa.StartsWith("GD") && int.TryParse(lastMa.Substring(2), out int number))
                        {
                            return $"GD{(number + 1):D4}";
                        }

                        return "GD0001";
                    }
                }
            }
            catch (Exception)
            {
                return "GD0001";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}