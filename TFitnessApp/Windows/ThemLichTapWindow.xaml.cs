using Microsoft.Data.Sqlite;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Globalization;
using System.Windows.Media;

namespace TFitnessApp.Windows
{
    public partial class ThemLichTapWindow : Window
    {
        private readonly string chuoiKetNoi;
        public Action OnScheduleAdded { get; set; }

        public ThemLichTapWindow()
        {
            InitializeComponent();

            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            chuoiKetNoi = $"Data Source={duongDanDB};";

            if (!File.Exists(duongDanDB))
            {
                MessageBox.Show($"Không tìm thấy cơ sở dữ liệu tại:\n{duongDanDB}", "Cảnh Báo Database", MessageBoxButton.OK, MessageBoxImage.Warning);
                chuoiKetNoi = string.Empty;
            }
        }

        // =========================================================================
        // LOGIC MỚI: TÌM MA_TK TỪ MA_PT (DỰA TRÊN QUY TẮC ĐẶT TÊN PTxxx -> TKxxx)
        // =========================================================================
        private string DeriveMaTKFromMaPT(string maPT)
        {
            string cleanedMaPT = maPT.Trim().ToUpper();
            // Đảm bảo Mã PT không rỗng, có ít nhất 3 ký tự (PTx), và bắt đầu bằng "PT"
            if (string.IsNullOrWhiteSpace(cleanedMaPT) || cleanedMaPT.Length < 3 || !cleanedMaPT.StartsWith("PT"))
            {
                return string.Empty;
            }
            // Lấy phần số và thêm tiền tố "TK"
            string numericPart = cleanedMaPT.Substring(2);
            return $"TK{numericPart}";
        }


        // =========================================================================
        // LOGIC KIỂM TRA KHÓA NGOẠI (GIỮ NGUYÊN)
        // =========================================================================

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

        private void MaHVTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LookupHocVienData(MaHVTextBox.Text.Trim());
        }

        private void MaPTTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LookupPTData(MaPTTextBox.Text.Trim());
        }

        private void LookupHocVienData(string maHV)
        {
            // ... (Giữ nguyên)
            TenHocVienTextBox.Text = string.Empty;
            SDTTextBox.Text = string.Empty;
            EmailTextBox.Text = string.Empty;
            GioiTinhTextBox.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(maHV) || string.IsNullOrEmpty(chuoiKetNoi)) return;

            string sql = @"
                SELECT 
                    hv.HoTen, hv.SDT, hv.Email, hv.GioiTinh
                FROM HocVien hv
                WHERE hv.MaHV = @MaHV;
            ";

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
                                TenHocVienTextBox.Text = reader["HoTen"].ToString();
                                SDTTextBox.Text = reader["SDT"].ToString();
                                EmailTextBox.Text = reader["Email"].ToString();
                                GioiTinhTextBox.Text = reader["GioiTinh"].ToString();
                            }
                            else
                            {
                                TenHocVienTextBox.Text = "[HV không tồn tại]";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI TRA CỨU HV: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LookupPTData(string maPT)
        {
            // ... (Giữ nguyên)
            TenPTTextBox.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(maPT) || string.IsNullOrEmpty(chuoiKetNoi)) return;

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
                            TenPTTextBox.Text = result.ToString();
                        }
                        else
                        {
                            TenPTTextBox.Text = "[PT không tồn tại]";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI TRA CỨU PT: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================================
        // LOGIC LƯU DATABASE (ĐÃ SỬA LỖI KHÓA NGOẠI BẰNG PHƯƠNG PHÁP SUY LUẬN ID)
        // =========================================================================

        private bool SaveLichTap()
        {
            // Lấy dữ liệu và TRIM
            string maLichTap = MaLichTapTextBox.Text.Trim();
            string maHV = MaHVTextBox.Text.Trim();
            string maPT = MaPTTextBox.Text.Trim(); // Mã PT do người dùng nhập (ví dụ: PT001)
            string maCN = MaCNTextBox.Text.Trim();
            string lopTap = LopTapTextBox.Text.Trim();
            string trangThai = TrangThaiComboBox.Text;

            string ngayTapStr = NgayTapDatePicker.SelectedDate.HasValue ? NgayTapDatePicker.SelectedDate.Value.ToString("dd-MM-yyyy") : null;
            string gioBatDau = GioBatDauTextBox.Text.Trim();
            string gioKetThuc = GioKetThucTextBox.Text.Trim();

            // 1. Kiểm tra ràng buộc cơ bản
            if (string.IsNullOrWhiteSpace(maLichTap) || string.IsNullOrWhiteSpace(maHV) || string.IsNullOrWhiteSpace(maPT) || string.IsNullOrWhiteSpace(maCN) || string.IsNullOrWhiteSpace(ngayTapStr))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Mã LT, Mã HV, Mã PT, Mã CN và Ngày tập.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 2. KIỂM TRA VÀ XỬ LÝ KHÓA NGOẠI

            // Kiểm tra Mã Học viên và Chi nhánh
            if (!CheckKeyExistence("HocVien", "MaHV", maHV))
            {
                MessageBox.Show($"LỖI KHÓA NGOẠI: Mã Học viên '{maHV}' không tồn tại trong bảng HocVien.", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!CheckKeyExistence("ChiNhanh", "MaCN", maCN))
            {
                MessageBox.Show($"LỖI KHÓA NGOẠI: Mã Chi nhánh '{maCN}' không tồn tại trong bảng ChiNhanh.", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // A. SỬA LỖI KHÓA NGOẠI MA_TK: Chuyển đổi MaPT thành MaTK

            // Bước A1: Đảm bảo Mã PT hợp lệ (tồn tại trong bảng PT)
            if (!CheckKeyExistence("PT", "MaPT", maPT))
            {
                MessageBox.Show($"LỖI KHÓA NGOẠI: Mã PT '{maPT}' không tồn tại trong bảng PT.", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Bước A2: Suy luận MaTK từ MaPT (ví dụ: PT001 -> TK001)
            string maTK = DeriveMaTKFromMaPT(maPT);

            if (string.IsNullOrWhiteSpace(maTK))
            {
                MessageBox.Show("Lỗi nội bộ: Không thể suy luận Mã Tài khoản (MaTK) từ Mã PT. Vui lòng kiểm tra định dạng MaPT (phải là PTxxx).", "Lỗi Logic", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Bước A3: Kiểm tra tính hợp lệ của MaTK lấy được (FK tới TaiKhoan)
            if (!CheckKeyExistence("TaiKhoan", "MaTK", maTK))
            {
                // Nếu MaTK được suy luận (TKxxx) không tồn tại trong bảng TaiKhoan, thông báo lỗi.
                MessageBox.Show($"LỖI KHÓA NGOẠI: Mã Tài khoản '{maTK}' (được suy luận từ Mã PT) không tồn tại trong bảng TaiKhoan. FK constraint failed.", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            string thoiGianBatDauDB = $"{gioBatDau} - {ngayTapStr}";
            string thoiGianKetThucDB = $"{gioKetThuc} - {ngayTapStr}";

            // SQL INSERT (ĐÃ SỬA: BINDING GIÁ TRỊ MA_TK VÀO CỘT MA_TK)
            string sql = @"
                INSERT INTO LichTap (MaLichTap, TenBuoiTap, TrangThai, ThoiGianBatDau, ThoiGianKetThuc, MaHV, MaTK, MaCN)
                VALUES (@MaLichTap, @TenBuoiTap, @TrangThai, @TGBD, @TGKT, @MaHV, @MaTKValue, @MaCN);
            ";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        // Parameters
                        command.Parameters.AddWithValue("@MaLichTap", maLichTap);
                        command.Parameters.AddWithValue("@TenBuoiTap", lopTap);
                        command.Parameters.AddWithValue("@TrangThai", trangThai);
                        command.Parameters.AddWithValue("@TGBD", thoiGianBatDauDB);
                        command.Parameters.AddWithValue("@TGKT", thoiGianKetThucDB);
                        command.Parameters.AddWithValue("@MaHV", maHV);
                        command.Parameters.AddWithValue("@MaTKValue", maTK); // <<< SỬ DỤNG MA_TK ĐÃ SUY LUẬN
                        command.Parameters.AddWithValue("@MaCN", maCN);

                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Bắt lỗi database (ví dụ: UNIQUE constraint failed cho MaLichTap)
                MessageBox.Show($"LỖI TẠO LỊCH TẬP:\nSQLite Error: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // =========================================================================
        // SỰ KIỆN GIAO DIỆN (GIỮ NGUYÊN)
        // =========================================================================

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveLichTap())
            {
                MessageBox.Show("Tạo lịch tập mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                OnScheduleAdded?.Invoke();
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}