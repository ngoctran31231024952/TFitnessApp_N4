using System;
using System.IO;
using System.Windows;
using System.ComponentModel;
using Microsoft.Data.Sqlite;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace TFitnessApp.Windows
{
    public partial class Window4 : Window, INotifyPropertyChanged
    {
        private string _chuoiKetNoi;
        private MoDonDuLieuTaiKhoan _taiKhoan;
        private string _maTK;
        private string _hoTen;
        private string _phanQuyen;
        private string _tenDangNhap;
        private string _matKhau;
        private string _email;
        private string _SDT;
        private string _ngayTao;
        private string _trangThai;

        // Biến để kiểm tra xem có đang trong chế độ chỉnh sửa không
        private bool _isEditMode = false;

        public string MaTK
        {
            get => _maTK;
            set { _maTK = value; OnPropertyChanged(nameof(MaTK)); }
        }

        public string HoTen
        {
            get => _hoTen;
            set { _hoTen = value; OnPropertyChanged(nameof(HoTen)); }
        }

        public string PhanQuyen
        {
            get => _phanQuyen;
            set { _phanQuyen = value; OnPropertyChanged(nameof(PhanQuyen)); }
        }

        public string TenDangNhap
        {
            get => _tenDangNhap;
            set { _tenDangNhap = value; OnPropertyChanged(nameof(TenDangNhap)); }
        }

        public string MatKhau
        {
            get => _matKhau;
            set { _matKhau = value; OnPropertyChanged(nameof(MatKhau)); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        public string SDT
        {
            get => _SDT;
            set { _SDT = value; OnPropertyChanged(nameof(SDT)); }
        }

        public string NgayTao
        {
            get => _ngayTao;
            set { _ngayTao = value; OnPropertyChanged(nameof(NgayTao)); OnPropertyChanged(nameof(NgayTaoFormatted)); }
        }

        public string TrangThai
        {
            get => _trangThai;
            set { _trangThai = value; OnPropertyChanged(nameof(TrangThai)); }
        }

        public string NgayTaoFormatted
        {
            get
            {
                if (DateTime.TryParse(_ngayTao, out DateTime ngayTao))
                {
                    return ngayTao.ToString("dd/MM/yyyy");
                }
                return _ngayTao;
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set { _isEditMode = value; OnPropertyChanged(nameof(IsEditMode)); }
        }

        // Constructor nhận đối tượng MoDonDuLieuTaiKhoan
        public Window4(MoDonDuLieuTaiKhoan taiKhoan)
        {
            InitializeComponent();

            // Khởi tạo chuỗi kết nối
            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            _chuoiKetNoi = $"Data Source={duongDanDB};";

            this.DataContext = this;

            // Lưu đối tượng tài khoản
            _taiKhoan = taiKhoan;

            // Gán dữ liệu cơ bản từ đối tượng tài khoản
            GanDuLieuCoBanTuTaiKhoan(taiKhoan);

            // Tải thông tin đầy đủ từ database (bao gồm số điện thoại, email, phân quyền, trạng thái)
            TaiThongTinDayDuTuDatabase();

            // Khởi tạo chế độ xem
            SetEditMode(false);
        }

        private void GanDuLieuCoBanTuTaiKhoan(MoDonDuLieuTaiKhoan taiKhoan)
        {
            // Chỉ gán các trường cơ bản từ đối tượng tài khoản
            MaTK = taiKhoan.MaTK;
            HoTen = taiKhoan.HoTen;
            TenDangNhap = taiKhoan.TenDangNhap;
            MatKhau = taiKhoan.MatKhau;
            PhanQuyen = taiKhoan.PhanQuyen;
            TrangThai = taiKhoan.TrangThai;
            NgayTao = taiKhoan.NgayTao.ToString();

            // Các trường PhanQuyen, Email, SoDienThoai, TrangThai sẽ được lấy từ database
            // để đảm bảo dữ liệu mới nhất
        }

        private void TaiThongTinDayDuTuDatabase()
        {
            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();

                    // Lấy thông tin đầy đủ từ database
                    string query = @"
                        SELECT Email, SDT  
                        FROM TaiKhoan
                        WHERE MaTK = @MaTK";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaTK", MaTK);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Cập nhật tất cả các trường từ database với xử lý NULL
                                Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : "";
                                SDT = reader["SDT"] != DBNull.Value ? reader["SDT"].ToString() : "";

                                // Debug: Hiển thị giá trị đã lấy được
                                DebugDuLieu();
                            }
                            else
                            {
                                MessageBox.Show("Không tìm thấy thông tin tài khoản trong database!");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thông tin đầy đủ: {ex.Message}");
            }
        }

        // Phương thức debug để kiểm tra dữ liệu đã lấy được
        private void DebugDuLieu()
        {
            Console.WriteLine($"=== DEBUG DỮ LIỆU TÀI KHOẢN {MaTK} ===");
            Console.WriteLine($"Số điện thoại: '{SDT}'");
            Console.WriteLine($"Phân quyền: '{PhanQuyen}'");
            Console.WriteLine($"Trạng thái: '{TrangThai}'");
            Console.WriteLine($"Email: '{Email}'");
            Console.WriteLine($"Ngày tạo: '{NgayTao}'");
            Console.WriteLine("=====================================");
        }

        private void SetEditMode(bool isEdit)
        {
            IsEditMode = isEdit;

            // Cập nhật trạng thái các control có thể chỉnh sửa
            if (txtTenDangNhap != null)
                txtTenDangNhap.IsReadOnly = !isEdit;

            if (txtMatKhau != null)
                txtMatKhau.IsReadOnly = !isEdit;

            if (txtHoTen != null)
                txtHoTen.IsReadOnly = !isEdit;

            if (txtEmail != null)
                txtEmail.IsReadOnly = !isEdit;

            if (txtSoDienThoai != null)
                txtSoDienThoai.IsReadOnly = !isEdit;

            if (cbPhanQuyen != null)
                cbPhanQuyen.IsEnabled = isEdit;

            if (cbTrangThai != null)
                cbTrangThai.IsEnabled = isEdit;

            // Cập nhật nút
            if (btnSua != null)
            {
                var stackPanel = btnSua.Content as StackPanel;
                if (stackPanel != null)
                {
                    var textBlock = stackPanel.Children[1] as TextBlock;
                    if (textBlock != null)
                        textBlock.Text = isEdit ? "LƯU" : "SỬA";
                }
            }

            // Cập nhật style cho các textbox
            UpdateTextBoxStyles();
        }

        private void UpdateTextBoxStyles()
        {
            var editableStyle = (Style)FindResource("EditableTextBoxStyle");
            var readOnlyStyle = (Style)FindResource("ReadOnlyTextBoxStyle");

            if (txtTenDangNhap != null)
                txtTenDangNhap.Style = IsEditMode ? editableStyle : readOnlyStyle;

            if (txtMatKhau != null)
                txtMatKhau.Style = IsEditMode ? editableStyle : readOnlyStyle;

            if (txtHoTen != null)
                txtHoTen.Style = IsEditMode ? editableStyle : readOnlyStyle;

            if (txtEmail != null)
                txtEmail.Style = IsEditMode ? editableStyle : readOnlyStyle;

            if (txtSoDienThoai != null)
                txtSoDienThoai.Style = IsEditMode ? editableStyle : readOnlyStyle;
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

        private void btnSua_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEditMode)
            {
                // Chuyển sang chế độ chỉnh sửa
                SetEditMode(true);
            }
            else
            {
                // Lưu thay đổi
                try
                {
                    // Validate dữ liệu
                    if (string.IsNullOrEmpty(TenDangNhap))
                    {
                        MessageBox.Show("Vui lòng nhập tên đăng nhập!", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtTenDangNhap.Focus();
                        return;
                    }

                    if (string.IsNullOrEmpty(MatKhau))
                    {
                        MessageBox.Show("Vui lòng nhập mật khẩu!", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtMatKhau.Focus();
                        return;
                    }

                    if (string.IsNullOrEmpty(HoTen))
                    {
                        MessageBox.Show("Vui lòng nhập họ tên!", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtHoTen.Focus();
                        return;
                    }

                    if (string.IsNullOrEmpty(Email))
                    {
                        MessageBox.Show("Vui lòng nhập email!", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtEmail.Focus();
                        return;
                    }

                    if (!KiemTraEmailHopLe(Email))
                    {
                        MessageBox.Show("Email không hợp lệ! Vui lòng nhập đúng định dạng email.", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtEmail.Focus();
                        return;
                    }

                    if (string.IsNullOrEmpty(SDT))
                    {
                        MessageBox.Show("Vui lòng nhập số điện thoại!", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtSoDienThoai.Focus();
                        return;
                    }

                    // Kiểm tra tên đăng nhập đã tồn tại chưa (trừ tài khoản hiện tại)
                    if (KiemTraTonTai("TaiKhoan", "TenDangNhap", TenDangNhap, MaTK))
                    {
                        MessageBox.Show("Tên đăng nhập đã tồn tại! Vui lòng chọn tên đăng nhập khác.", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtTenDangNhap.Focus();
                        return;
                    }

                    // Kiểm tra email đã tồn tại chưa (trừ tài khoản hiện tại)
                    if (KiemTraTonTai("TaiKhoan", "Email", Email, MaTK))
                    {
                        MessageBox.Show("Email đã tồn tại! Vui lòng sử dụng email khác.", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtEmail.Focus();
                        return;
                    }

                    // Cập nhật giá trị
                    using (var connection = new SqliteConnection(_chuoiKetNoi))
                    {
                        connection.Open();
                        string query = @"
                            UPDATE TaiKhoan 
                            SET TenDangNhap = @TenDangNhap, 
                                MatKhau = @MatKhau, 
                                HoTen = @HoTen, 
                                Email = @Email, 
                                SDT = @SDT,  -- SỬA: SoDienThoai -> SDT
                                PhanQuyen = @PhanQuyen, 
                                TrangThai = @TrangThai
                            WHERE MaTK = @MaTK";

                        using (var command = new SqliteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@TenDangNhap", TenDangNhap);
                            command.Parameters.AddWithValue("@MatKhau", MatKhau);
                            command.Parameters.AddWithValue("@HoTen", HoTen);
                            command.Parameters.AddWithValue("@Email", Email);
                            command.Parameters.AddWithValue("@SDT", SDT);  // SỬA: @SoDienThoai -> @SDT
                            command.Parameters.AddWithValue("@PhanQuyen", PhanQuyen);
                            command.Parameters.AddWithValue("@TrangThai", TrangThai);
                            command.Parameters.AddWithValue("@MaTK", MaTK);

                            int rowsAffected = command.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Cập nhật tài khoản thành công!", "Thành công",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                SetEditMode(false);

                                // Cập nhật lại đối tượng tài khoản
                                _taiKhoan.TenDangNhap = TenDangNhap;
                                _taiKhoan.MatKhau = MatKhau;
                                _taiKhoan.HoTen = HoTen;
                                _taiKhoan.Email = Email;
                                _taiKhoan.PhanQuyen = PhanQuyen;
                                _taiKhoan.TrangThai = TrangThai;
                            }
                            else
                            {
                                MessageBox.Show("Cập nhật tài khoản thất bại!", "Lỗi",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi cập nhật tài khoản: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnXoa_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa tài khoản {MaTK} - {HoTen} không?\n\nHành động này không thể hoàn tác!", "Xác nhận xóa",
                                       MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var connection = new SqliteConnection(_chuoiKetNoi))
                    {
                        connection.Open();
                        string query = "DELETE FROM TaiKhoan WHERE MaTK = @MaTK";

                        using (var command = new SqliteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@MaTK", MaTK);
                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Xóa tài khoản thành công!", "Thành công",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Xóa tài khoản thất bại!", "Lỗi",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa tài khoản: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

        private bool KiemTraTonTai(string tableName, string columnName, string value, string excludeMaTK = null)
        {
            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();
                    string query = $"SELECT COUNT(1) FROM {tableName} WHERE {columnName} = @Value";

                    if (!string.IsNullOrEmpty(excludeMaTK))
                    {
                        query += " AND MaTK != @ExcludeMaTK";
                    }

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Value", value);
                        if (!string.IsNullOrEmpty(excludeMaTK))
                        {
                            command.Parameters.AddWithValue("@ExcludeMaTK", excludeMaTK);
                        }

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