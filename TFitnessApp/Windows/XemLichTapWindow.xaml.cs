using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace TFitnessApp.Windows
{
    // =========================================================================
    // HELPER CLASSES (Convertor & Model)
    // =========================================================================

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = (bool)value;
            if (parameter != null && parameter.ToString() == "Inverse")
            {
                isVisible = !isVisible;
            }
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Model chi tiết để Data Binding
    public class LichTapDetailModel : INotifyPropertyChanged
    {
        private bool _isEditing;
        private string _tenHocVien;
        private string _soDienThoai;
        private string _email;
        private string _tenPT;
        private string _maPTInput;
        private string _chiNhanh;

        public string TitleText { get; set; } = "Chi tiết Lịch tập";
        public string MaLichTap { get; set; }

        // Editable fields
        public string TenBuoiTap { get; set; }
        public string MaHocVien { get; set; }
        public string GioBatDau { get; set; }
        public string GioKetThuc { get; set; }
        public string TrangThai { get; set; }
        public DateTime? NgayTap { get; set; }

        // Field lưu Mã Tài khoản (FK) dùng cho DB Update. KHÔNG HIỂN THỊ TRÊN UI.
        public string MaTK { get; set; }

        // Editable field: Mã PT mà người dùng nhập/thấy (PT001)
        public string MaPTInput
        {
            get => _maPTInput;
            set { _maPTInput = value; OnPropertyChanged(nameof(MaPTInput)); }
        }

        // Editable field: Mã Chi nhánh (TextBox)
        public string ChiNhanh
        {
            get => _chiNhanh;
            set { _chiNhanh = value; OnPropertyChanged(nameof(ChiNhanh)); }
        }

        // Display-only fields (from lookups)
        public string TenHocVien
        {
            get => _tenHocVien;
            set { _tenHocVien = value; OnPropertyChanged(nameof(TenHocVien)); }
        }
        public string SoDienThoai
        {
            get => _soDienThoai;
            set { _soDienThoai = value; OnPropertyChanged(nameof(SoDienThoai)); }
        }
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }
        public string TenPT
        {
            get => _tenPT;
            set { _tenPT = value; OnPropertyChanged(nameof(TenPT)); }
        }

        public bool IsEditing
        {
            get { return _isEditing; }
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged(nameof(IsEditing));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // =========================================================================
    // WINDOW LOGIC
    // =========================================================================
    public partial class XemLichTapWindow : Window
    {
        private readonly string chuoiKetNoi;
        private LichTapDetailModel CurrentData { get; set; }
        private LichTapDetailModel OriginalData { get; set; }
        public Action OnScheduleUpdated { get; set; }

        // Constructor không tham số (Dành cho XAML Designer)
        public XemLichTapWindow()
        {
            InitializeComponent();
        }

        public XemLichTapWindow(string maLichTap)
        {
            InitializeComponent();

            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            chuoiKetNoi = $"Data Source={duongDanDB};";

            if (!File.Exists(duongDanDB))
            {
                MessageBox.Show($"Không tìm thấy cơ sở dữ liệu tại:\n{duongDanDB}", "Cảnh Báo Database", MessageBoxButton.OK, MessageBoxImage.Warning);
                chuoiKetNoi = string.Empty;
            }

            LoadLichTapData(maLichTap);
            this.DataContext = CurrentData;
        }

        // =========================================================================
        // LOGIC TRA CỨU ID VÀ LOAD DỮ LIỆU
        // =========================================================================

        // HÀM HELPER: Chuyển MaTK thành MaPT (Ví dụ: TK001 -> PT001)
        private string DeriveMaPTFromMaTK(string maTK)
        {
            string cleanedMaTK = maTK.Trim().ToUpper();
            if (cleanedMaTK.Length > 2 && cleanedMaTK.StartsWith("TK"))
            {
                return $"PT{cleanedMaTK.Substring(2)}";
            }
            return cleanedMaTK;
        }

        // HÀM HELPER: Chuyển MaPT thành MaTK (Ví dụ: PT001 -> TK001)
        private string DeriveMaTKFromMaPT(string maPT)
        {
            string cleanedMaPT = maPT.Trim().ToUpper();
            if (cleanedMaPT.Length > 2 && cleanedMaPT.StartsWith("PT"))
            {
                return $"TK{cleanedMaPT.Substring(2)}";
            }
            return cleanedMaPT;
        }

        private void LoadLichTapData(string maLichTap)
        {
            if (string.IsNullOrEmpty(chuoiKetNoi)) return;

            string sql = @"
                SELECT 
                    lt.MaLichTap, lt.TenBuoiTap, lt.MaHV, lt.MaTK, lt.MaCN, lt.TrangThai,
                    SUBSTR(lt.ThoiGianBatDau, 1, INSTR(lt.ThoiGianBatDau, ' ') - 1) AS GioBatDau,
                    SUBSTR(lt.ThoiGianKetThuc, 1, INSTR(lt.ThoiGianKetThuc, ' ') - 1) AS GioKetThuc,
                    SUBSTR(lt.ThoiGianBatDau, INSTR(lt.ThoiGianBatDau, ' ') + 3) AS NgayTapStr
                FROM LichTap lt
                WHERE lt.MaLichTap = @MaLichTap;
            ";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@MaLichTap", maLichTap);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string maTKLoaded = reader["MaTK"].ToString();

                                CurrentData = new LichTapDetailModel
                                {
                                    MaLichTap = reader["MaLichTap"].ToString(),
                                    TenBuoiTap = reader["TenBuoiTap"].ToString(),
                                    MaHocVien = reader["MaHV"].ToString(),
                                    MaTK = maTKLoaded,
                                    MaPTInput = DeriveMaPTFromMaTK(maTKLoaded),
                                    ChiNhanh = reader["MaCN"].ToString(),
                                    TrangThai = reader["TrangThai"].ToString(),
                                    GioBatDau = reader["GioBatDau"].ToString(),
                                    GioKetThuc = reader["GioKetThuc"].ToString()
                                };

                                if (DateTime.TryParseExact(reader["NgayTapStr"].ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ngayTap))
                                {
                                    CurrentData.NgayTap = ngayTap;
                                }

                                // Tra cứu thông tin chi tiết
                                LookupHocVienData(CurrentData.MaHocVien);
                                LookupPTData(CurrentData.MaPTInput);

                                CurrentData.TitleText = $"Chi tiết Lịch tập: {CurrentData.MaLichTap}";

                                OriginalData = DeepCopy(CurrentData);
                            }
                            else
                            {
                                MessageBox.Show("Không tìm thấy lịch tập này.", "Lỗi Dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                                this.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI TẢI DỮ LIỆU: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        // CẬP NHẬT: Lấy HoTen, SDT, Email
        private void LookupHocVienData(string maHV)
        {
            string tenHV = "Không tìm thấy";
            string sdt = "Không tìm thấy";
            string email = "Không tìm thấy";

            if (string.IsNullOrWhiteSpace(maHV) || string.IsNullOrEmpty(chuoiKetNoi))
            {
                CurrentData.TenHocVien = string.Empty;
                CurrentData.SoDienThoai = string.Empty;
                CurrentData.Email = string.Empty;
                return;
            }

            // TRUY VẤN: Lấy HoTen, SDT, Email từ bảng HocVien
            string sql = "SELECT HoTen, SDT, Email FROM HocVien WHERE MaHV = @MaHV;";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@MaHV", maHV.Trim());
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                tenHV = reader["HoTen"].ToString();
                                sdt = reader["SDT"].ToString();
                                email = reader["Email"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI TRA CỨU HV: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            CurrentData.TenHocVien = tenHV;
            CurrentData.SoDienThoai = sdt;
            CurrentData.Email = email;
        }

        // CẬP NHẬT: Tra cứu Tên PT từ bảng PT bằng MaPT
        private void LookupPTData(string maPT)
        {
            string tenPT = "Không tìm thấy";

            if (string.IsNullOrWhiteSpace(maPT) || string.IsNullOrEmpty(chuoiKetNoi))
            {
                CurrentData.TenPT = string.Empty;
                return;
            }

            // TRUY VẤN: Lấy HoTen từ bảng PT bằng MaPT
            string sql = "SELECT HoTen FROM PT WHERE MaPT = @MaPT;";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@MaPT", maPT.Trim());
                        object result = command.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            tenPT = result.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI TRA CỨU PT: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            CurrentData.TenPT = tenPT;
        }

        private LichTapDetailModel DeepCopy(LichTapDetailModel source)
        {
            return new LichTapDetailModel
            {
                MaLichTap = source.MaLichTap,
                TenBuoiTap = source.TenBuoiTap,
                MaHocVien = source.MaHocVien,
                MaTK = source.MaTK,
                MaPTInput = source.MaPTInput,
                NgayTap = source.NgayTap,
                GioBatDau = source.GioBatDau,
                GioKetThuc = source.GioKetThuc,
                ChiNhanh = source.ChiNhanh,
                TrangThai = source.TrangThai,
                TenHocVien = source.TenHocVien,
                SoDienThoai = source.SoDienThoai,
                Email = source.Email,
                TenPT = source.TenPT,
                TitleText = source.TitleText,
                IsEditing = false
            };
        }

        // =========================================================================
        // SỰ KIỆN GIAO DIỆN & LƯU
        // =========================================================================

        private void TxtMaHV_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentData != null && CurrentData.IsEditing)
            {
                // Gọi lookup khi Mã HV thay đổi
                LookupHocVienData(CurrentData.MaHocVien);
            }
        }

        private void TxtMaPT_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentData != null && CurrentData.IsEditing)
            {
                // Gọi lookup Tên PT bằng MaPTInput
                LookupPTData(CurrentData.MaPTInput);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            CurrentData.IsEditing = true;
            CurrentData.TitleText = $"Chỉnh sửa Lịch tập: {CurrentData.MaLichTap}";
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentData.IsEditing)
            {
                CurrentData.IsEditing = false;

                // Khôi phục dữ liệu từ bản gốc
                CurrentData.MaHocVien = OriginalData.MaHocVien;
                CurrentData.MaPTInput = OriginalData.MaPTInput;
                CurrentData.MaTK = OriginalData.MaTK;
                CurrentData.TenBuoiTap = OriginalData.TenBuoiTap;
                CurrentData.NgayTap = OriginalData.NgayTap;
                CurrentData.GioBatDau = OriginalData.GioBatDau;
                CurrentData.GioKetThuc = OriginalData.GioKetThuc;
                CurrentData.ChiNhanh = OriginalData.ChiNhanh;
                CurrentData.TrangThai = OriginalData.TrangThai;

                // Cập nhật lại UI dựa trên dữ liệu đã khôi phục
                LookupHocVienData(CurrentData.MaHocVien);
                LookupPTData(CurrentData.MaPTInput);
                CurrentData.TitleText = OriginalData.TitleText;

                MessageBox.Show("Đã hủy và khôi phục dữ liệu gốc.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                this.Close();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (SaveLichTapUpdate())
            {
                CurrentData.IsEditing = false;
                OriginalData = DeepCopy(CurrentData);
                CurrentData.TitleText = OriginalData.TitleText;
                OnScheduleUpdated?.Invoke();
                MessageBox.Show("Cập nhật lịch tập thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool SaveLichTapUpdate()
        {
            if (string.IsNullOrEmpty(chuoiKetNoi)) return false;

            // 1. Lấy dữ liệu
            string maLichTap = CurrentData.MaLichTap;
            string maHV = CurrentData.MaHocVien?.Trim();
            string maPTInput = CurrentData.MaPTInput?.Trim();
            string maCN = CurrentData.ChiNhanh?.Trim();
            string lopTap = CurrentData.TenBuoiTap?.Trim();

            // LẤY GIÁ TRỊ VÀ TRÍCH XUẤT CHUỖI TRẠNG THÁI (FIX LỖI COMBOBOXITEM)
            string trangThai = CurrentData.TrangThai;
            if (trangThai != null && trangThai.StartsWith("System.Windows.Controls.ComboBoxItem:"))
            {
                trangThai = trangThai.Split(new char[] { ':' }, 2).Last().Trim();
            }
            trangThai = trangThai?.Trim();

            // 2. Chuyển đổi MaPTInput thành MaTK (FK)
            string maTK_FK = DeriveMaTKFromMaPT(maPTInput);

            string ngayTapStr = CurrentData.NgayTap.HasValue ? CurrentData.NgayTap.Value.ToString("dd-MM-yyyy") : null;
            string gioBatDau = CurrentData.GioBatDau?.Trim();
            string gioKetThuc = CurrentData.GioKetThuc?.Trim();

            // 3. Kiểm tra ràng buộc cơ bản
            if (string.IsNullOrWhiteSpace(lopTap) || string.IsNullOrWhiteSpace(maHV) || string.IsNullOrWhiteSpace(maPTInput) || string.IsNullOrWhiteSpace(maCN) || string.IsNullOrWhiteSpace(ngayTapStr) || string.IsNullOrWhiteSpace(gioBatDau) || string.IsNullOrWhiteSpace(gioKetThuc) || string.IsNullOrWhiteSpace(trangThai))
            {
                MessageBox.Show("Vui lòng điền đầy đủ các trường bắt buộc (bao gồm Trạng thái).", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 4. Kiểm tra Khóa ngoại (Đã dùng maTK_FK)

            // Kiểm tra Trạng thái (CHECK CONSTRAINT)
            string[] allowedStatuses = { "Đã Hoàn Thành", "Chưa Hoàn Thành", "Đã Hủy" };
            if (!allowedStatuses.Contains(trangThai, StringComparer.Ordinal))
            {
                MessageBox.Show($"LỖI DỮ LIỆU: Trạng thái '{trangThai}' không hợp lệ. Vui lòng chọn một trong các giá trị: Đã Hoàn Thành, Chưa Hoàn Thành, Đã Hủy.", "Lỗi Ràng buộc (Trạng thái)", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Kiểm tra các FK khác
            if (!CheckKeyExistence("HocVien", "MaHV", maHV))
            {
                MessageBox.Show($"LỖI KHÓA NGOẠI: Mã Học viên '{maHV}' không tồn tại.", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!CheckKeyExistence("TaiKhoan", "MaTK", maTK_FK))
            {
                MessageBox.Show($"LỖI KHÓA NGOẠI: Mã PT '{maPTInput}' không tương ứng với Mã Tài khoản hợp lệ trong TaiKhoan.", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!CheckKeyExistence("ChiNhanh", "MaCN", maCN))
            {
                MessageBox.Show($"LỖI KHÓA NGOẠI: Mã Chi nhánh '{maCN}' không tồn tại.", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }


            // 5. Chuẩn bị SQL
            string thoiGianBatDauDB = $"{gioBatDau} - {ngayTapStr}";
            string thoiGianKetThucDB = $"{gioKetThuc} - {ngayTapStr}";

            string sql = @"
                UPDATE LichTap SET 
                    TenBuoiTap = @TenBuoiTap, 
                    TrangThai = @TrangThai, 
                    ThoiGianBatDau = @TGBD, 
                    ThoiGianKetThuc = @TGKT, 
                    MaHV = @MaHV, 
                    MaTK = @MaTK, 
                    MaCN = @MaCN
                WHERE MaLichTap = @MaLichTap;
            ";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@TenBuoiTap", lopTap);
                        command.Parameters.AddWithValue("@TrangThai", trangThai);
                        command.Parameters.AddWithValue("@TGBD", thoiGianBatDauDB);
                        command.Parameters.AddWithValue("@TGKT", thoiGianKetThucDB);
                        command.Parameters.AddWithValue("@MaHV", maHV);
                        command.Parameters.AddWithValue("@MaTK", maTK_FK); // Sử dụng MaTK đã Derive
                        command.Parameters.AddWithValue("@MaCN", maCN);
                        command.Parameters.AddWithValue("@MaLichTap", maLichTap);

                        command.ExecuteNonQuery();

                        // Cập nhật lại MaTK trong model sau khi lưu thành công
                        CurrentData.MaTK = maTK_FK;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI CẬP NHẬT: SQLite Error: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool CheckKeyExistence(string tableName, string columnName, string value)
        {
            string cleanedValue = value.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(cleanedValue)) return false;

            string sql = $"SELECT COUNT(*) FROM {tableName} WHERE UPPER({columnName}) = @Value;";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Value", cleanedValue);
                        return Convert.ToInt32(command.ExecuteScalar()) > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}