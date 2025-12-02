using Microsoft.Data.Sqlite;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Globalization;
using System.Linq;

namespace TFitnessApp
{
    public partial class ThemHopDongWindow : Window
    {
        private readonly string chuoiKetNoi;

        // Định nghĩa Action để thông báo cho cửa sổ cha (HopDongPage) làm mới dữ liệu
        public Action OnContractAdded { get; set; }

        public ThemHopDongWindow()
        {
            InitializeComponent();
            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            chuoiKetNoi = $"Data Source={duongDanDB};";
            if (!File.Exists(duongDanDB))
            {
                MessageBox.Show($"Không tìm thấy cơ sở dữ liệu tại:\n{duongDanDB}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                chuoiKetNoi = string.Empty;
            }
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

        // =========================================================================
        // LOGIC TRA CỨU HỌC VIÊN
        // =========================================================================

        private void MaHVTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LookupHocVienData(MaHVTextBox.Text.Trim());
        }

        private void LookupHocVienData(string maHV)
        {
            if (string.IsNullOrWhiteSpace(maHV) || string.IsNullOrEmpty(chuoiKetNoi))
            {
                HoTenHocVienTextBox.Text = string.Empty;
                SDTTextBox.Text = string.Empty;
                EmailTextBox.Text = string.Empty;
                GioiTinhComboBox.SelectedIndex = -1;
                return;
            }

            string sql = @"
                SELECT HoTen, GioiTinh, SDT, Email
                FROM HocVien
                WHERE MaHV = @MaHV;
            ";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@MaHV", maHV);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                HoTenHocVienTextBox.Text = reader["HoTen"].ToString();
                                SDTTextBox.Text = reader["SDT"].ToString();
                                EmailTextBox.Text = reader["Email"].ToString();

                                string gioiTinhDB = reader["GioiTinh"].ToString();

                                if (gioiTinhDB.Equals("Nam", StringComparison.OrdinalIgnoreCase))
                                {
                                    GioiTinhComboBox.SelectedIndex = 0;
                                }
                                else if (gioiTinhDB.Equals("Nữ", StringComparison.OrdinalIgnoreCase))
                                {
                                    GioiTinhComboBox.SelectedIndex = 1;
                                }
                                else
                                {
                                    GioiTinhComboBox.SelectedIndex = -1;
                                }

                            }
                            else
                            {
                                HoTenHocVienTextBox.Text = "[Học viên mới/không tìm thấy]";
                                SDTTextBox.Text = string.Empty;
                                EmailTextBox.Text = string.Empty;
                                GioiTinhComboBox.SelectedIndex = -1;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI TRA CỨU HỌC VIÊN: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================================
        // LOGIC TRA CỨU GÓI TẬP VÀ PT (Không thay đổi)
        // =========================================================================

        private void MaGoiTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LookupGoiTapData(MaGoiTextBox.Text.Trim());
        }

        private void LookupGoiTapData(string maGoi)
        {
            if (string.IsNullOrWhiteSpace(maGoi) || string.IsNullOrEmpty(chuoiKetNoi))
            {
                TongTienTextBox.Text = string.Empty;
                return;
            }

            string sql = "SELECT GiaNiemYet FROM GoiTap WHERE MaGoi = @MaGoi;";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@MaGoi", maGoi);
                        object result = command.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            TongTienTextBox.Text = result.ToString();
                        }
                        else
                        {
                            TongTienTextBox.Text = "0";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI TRA CỨU GÓI TẬP: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                TongTienTextBox.Text = "LỖI";
            }
        }

        private void MaPTTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LookupPTData(MaPTTextBox.Text.Trim());
        }

        private void LookupPTData(string maPT)
        {
            if (string.IsNullOrWhiteSpace(maPT) || string.IsNullOrEmpty(chuoiKetNoi))
            {
                PTTextBox.Text = string.Empty;
                return;
            }

            string sql = "SELECT HoTen FROM PT WHERE MaPT = @MaPT;";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@MaPT", maPT);
                        object result = command.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            PTTextBox.Text = result.ToString();
                        }
                        else
                        {
                            PTTextBox.Text = "[PT không tồn tại]";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI TRA CỨU PT: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                PTTextBox.Text = "LỖI";
            }
        }


        // =========================================================================
        // LOGIC LƯU DATABASE (CHỈ LƯU 7 TRƯỜNG YÊU CẦU)
        // =========================================================================
        private bool SaveContractToDatabase(
            string maHD, string maGoi, string maHV, string maPT,
            string ngayBatDau, string ngayKetThuc, string loaiHD, string ghiChu) // Chỉ còn 8 tham số cần lưu
        {
            if (string.IsNullOrEmpty(chuoiKetNoi)) return false;

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();

                    // SỬA LỖI VÀ THỰC HIỆN YÊU CẦU: Chỉ INSERT 7 cột chính
                    string sqlInsertHD = @"
                        INSERT INTO HopDong (MaHD, MaGoi, MaHV, MaPT, 
                                             NgayBatDau, NgayHetHan, LoaiHopDong, GhiChu)
                        VALUES (@MaHD, @MaGoi, @MaHV, @MaPT, 
                                @NgayBatDau, @NgayKetThuc, @LoaiHopDong, @GhiChu);
                    ";
                    using (var cmd = new SqliteCommand(sqlInsertHD, connection))
                    {
                        cmd.Parameters.AddWithValue("@MaHD", maHD);
                        cmd.Parameters.AddWithValue("@MaGoi", maGoi);
                        cmd.Parameters.AddWithValue("@MaHV", maHV);
                        cmd.Parameters.AddWithValue("@MaPT", maPT);
                        cmd.Parameters.AddWithValue("@NgayBatDau", ngayBatDau);
                        cmd.Parameters.AddWithValue("@NgayKetThuc", string.IsNullOrWhiteSpace(ngayKetThuc) ? DBNull.Value : (object)ngayKetThuc);
                        cmd.Parameters.AddWithValue("@LoaiHopDong", loaiHD);
                        cmd.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(ghiChu) ? DBNull.Value : (object)ghiChu);

                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI DATABASE: {ex.Message}", "Thêm Hợp Đồng Thất Bại", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        // =========================================================================
        // XỬ LÝ SỰ KIỆN NÚT
        // =========================================================================

        private void TaoHopDong_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MaHDTextBox.Text) || string.IsNullOrWhiteSpace(MaHVTextBox.Text) || NgayBatDauDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng nhập Mã HĐ, Mã HV và Ngày bắt đầu!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Thu thập các trường CẦN LƯU
            string maHD = MaHDTextBox.Text;
            string maGoi = MaGoiTextBox.Text;
            string maHV = MaHVTextBox.Text;
            string maPT = MaPTTextBox.Text;
            string loaiHD = LoaiHopDongComboBox.Text;
            string ghiChu = GhiChuTextBox.Text;

            string ngayBatDau = NgayBatDauDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd");
            string ngayKetThuc = NgayKetThucDatePicker.SelectedDate.HasValue ? NgayKetThucDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd") : null;

            // Các trường đã bị loại bỏ khỏi lưu: maCN, trangThai, phuongThuc, tongTien.

            if (SaveContractToDatabase(
                maHD, maGoi, maHV, maPT, ngayBatDau, ngayKetThuc, loaiHD, ghiChu)) // Chỉ truyền các tham số cần thiết
            {
                MessageBox.Show("Đã thêm hợp đồng mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                OnContractAdded?.Invoke();

                this.Close();
            }
        }

        private void InPhieu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Thực hiện chức năng In phiếu hợp đồng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}


