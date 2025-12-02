using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Data.Sqlite;

namespace TFitnessApp.Windows
{
    public partial class XemHopDongWindow : Window
    {
        private readonly string chuoiKetNoi;
        private readonly string maHopDongHienTai;
        private bool isEditing = false;

        public XemHopDongWindow(string maHD)
        {
            InitializeComponent();
            maHopDongHienTai = maHD;

            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            chuoiKetNoi = $"Data Source={duongDanDB};";

            LoadHopDongData(maHD);
            SetEditMode(false);
        }

        // =========================================================================
        // HÀM TÍNH TOÁN VÀ TRA CỨU HỌC VIÊN
        // =========================================================================

        private string TinhTrangThai(DateTime? ngayHetHan)
        {
            if (!ngayHetHan.HasValue || ngayHetHan.Value == DateTime.MinValue)
            {
                return "Không rõ";
            }
            DateTime ngayHienTai = DateTime.Today;
            if (ngayHienTai < ngayHetHan.Value)
            {
                return "Còn hiệu lực";
            }
            else
            {
                return "Hết hiệu lực";
            }
        }

        private void MaHVTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Chỉ tra cứu khi ở chế độ chỉnh sửa
            if (isEditing)
            {
                LookupHocVienData(MaHVTextBox.Text.Trim());
            }
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
                                    GioiTinhComboBox.SelectedIndex = 0;
                                else if (gioiTinhDB.Equals("Nữ", StringComparison.OrdinalIgnoreCase))
                                    GioiTinhComboBox.SelectedIndex = 1;
                                else
                                    GioiTinhComboBox.SelectedIndex = -1;
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
        // LOGIC TẢI DỮ LIỆU
        // =========================================================================

        private void LoadHopDongData(string maHD)
        {
            if (string.IsNullOrEmpty(chuoiKetNoi)) return;

            // Truy vấn dữ liệu cần thiết (Lấy GiaNiemYet từ GoiTap để hiển thị Thành tiền)
            string sql = @"
                SELECT 
                    hd.MaHD, hd.NgayBatDau, hd.NgayHetHan, hd.LoaiHopDong, hd.MaGoi, 
                    gt.GiaNiemYet AS TongTien, 
                    hd.MaCN, hd.MaPT, hd.GhiChu,
                    hv.MaHV, hv.HoTen AS TenHV, hv.GioiTinh, hv.SDT, hv.Email, 
                    pt.HoTen AS TenPT
                FROM HopDong hd
                LEFT JOIN HocVien hv ON hd.MaHV = hv.MaHV
                LEFT JOIN PT pt ON hd.MaPT = pt.MaPT
                INNER JOIN GoiTap gt ON hd.MaGoi = gt.MaGoi
                WHERE hd.MaHD = @MaHD;
            ";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@MaHD", maHD);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                DateTime? ngayHetHan = ParseDate(reader["NgayHetHan"].ToString());
                                string trangThai = TinhTrangThai(ngayHetHan);

                                MaHDTextBox.Text = reader["MaHD"].ToString();
                                NgayBatDauDatePicker.SelectedDate = ParseDate(reader["NgayBatDau"].ToString());
                                NgayKetThucDatePicker.SelectedDate = ngayHetHan;
                                SetComboBoxValue(LoaiHopDongComboBox, reader["LoaiHopDong"].ToString());
                                SetComboBoxValue(TrangThaiComboBox, trangThai);
                                MaGoiTextBox.Text = reader["MaGoi"].ToString();
                                TongTienTextBox.Text = reader["TongTien"].ToString();
                                SetComboBoxValue(MaCNComboBox, reader["MaCN"].ToString());
                                MaPTTextBox.Text = reader["MaPT"].ToString();
                                PTTextBox.Text = reader["TenPT"].ToString();
                                MaHVTextBox.Text = reader["MaHV"].ToString();
                                HoTenHocVienTextBox.Text = reader["TenHV"].ToString();
                                SetComboBoxValue(GioiTinhComboBox, reader["GioiTinh"].ToString());
                                SDTTextBox.Text = reader["SDT"].ToString();
                                EmailTextBox.Text = reader["Email"].ToString();
                                GhiChuTextBox.Text = reader["GhiChu"].ToString();
                            }
                            else
                            {
                                MessageBox.Show("Không tìm thấy dữ liệu hợp đồng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // =========================================================================
        // LOGIC CHỈNH SỬA VÀ LƯU
        // =========================================================================

        private void SetEditMode(bool enable)
        {
            isEditing = enable;
            SaveButton.IsEnabled = enable;

            // Các trường đã nhập lúc tạo hợp đồng (CHO PHÉP CHỈNH SỬA)
            MaHDTextBox.IsReadOnly = !enable;
            NgayBatDauDatePicker.IsEnabled = enable;
            NgayKetThucDatePicker.IsEnabled = enable;
            LoaiHopDongComboBox.IsEnabled = enable;
            TrangThaiComboBox.IsEnabled = false; // Luôn là ReadOnly (tính toán)
            MaGoiTextBox.IsReadOnly = !enable;
            MaCNComboBox.IsEnabled = enable;
            MaPTTextBox.IsReadOnly = !enable;
            GhiChuTextBox.IsReadOnly = !enable;

            // Mã Học viên: Cho phép chỉnh sửa (và kích hoạt tra cứu)
            MaHVTextBox.IsReadOnly = !enable;

            // Các trường tra cứu (VẪN GIỮ ReadOnly=True)
            TongTienTextBox.IsReadOnly = true;

            UpdateControlBackgrounds(enable);
        }

        private void UpdateControlBackgrounds(bool isEditable)
        {
            SolidColorBrush editableColor = Brushes.White;
            SolidColorBrush readOnlyColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#F0F0F0");

            Control[] allControls = {
                MaHDTextBox, NgayBatDauDatePicker, NgayKetThucDatePicker, LoaiHopDongComboBox, TrangThaiComboBox,
                MaGoiTextBox, TongTienTextBox, MaCNComboBox, MaPTTextBox, GhiChuTextBox,
                MaHVTextBox, HoTenHocVienTextBox, GioiTinhComboBox, SDTTextBox, EmailTextBox, PTTextBox
            };

            foreach (var control in allControls)
            {
                // Xác định trạng thái ReadOnly/Disabled VĨNH VIỄN
                bool isAlwaysReadOnly = (control == TongTienTextBox || control == TrangThaiComboBox || control == HoTenHocVienTextBox || control == SDTTextBox || control == EmailTextBox || control == GioiTinhComboBox || control == PTTextBox);

                if (control is TextBox textBox)
                {
                    textBox.Background = isEditable && !isAlwaysReadOnly ? editableColor : readOnlyColor;
                    textBox.IsReadOnly = isAlwaysReadOnly || !isEditable;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.Background = isEditable && !isAlwaysReadOnly ? editableColor : readOnlyColor;
                    comboBox.IsEnabled = isEditable && !isAlwaysReadOnly;
                }
                else if (control is DatePicker datePicker)
                {
                    datePicker.Background = isEditable && !isAlwaysReadOnly ? editableColor : readOnlyColor;
                    datePicker.IsEnabled = isEditable && !isAlwaysReadOnly;
                }
            }
        }

        private void UpdateHopDongData()
        {
            // Lấy dữ liệu từ Form (Chỉ các trường HopDong)
            string maHD = MaHDTextBox.Text;
            string ngayBatDau = NgayBatDauDatePicker.SelectedDate.HasValue ? NgayBatDauDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd") : null;
            string ngayKetThuc = NgayKetThucDatePicker.SelectedDate.HasValue ? NgayKetThucDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd") : null;

            string loaiHopDong = LoaiHopDongComboBox.Text;
            if (string.IsNullOrWhiteSpace(loaiHopDong))
            {
                MessageBox.Show("Vui lòng chọn một giá trị cho 'Loại hợp đồng'.", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string maGoi = MaGoiTextBox.Text;
            string maCN = MaCNComboBox.Text;
            string maPT = MaPTTextBox.Text;
            string ghiChu = GhiChuTextBox.Text;
            string maHV = MaHVTextBox.Text;

            // Bắt đầu UPDATE SQL
            string sql = @"
                UPDATE HopDong SET 
                    NgayBatDau = @NgayBatDau, NgayHetHan = @NgayHetHan, LoaiHopDong = @LoaiHopDong, 
                    MaGoi = @MaGoi, MaCN = @MaCN, MaPT = @MaPT, GhiChu = @GhiChu, MaHV = @MaHV
                WHERE MaHD = @MaHDHienTai;
            ";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        // Parameters
                        command.Parameters.AddWithValue("@NgayBatDau", ngayBatDau);
                        command.Parameters.AddWithValue("@NgayHetHan", ngayKetThuc ?? DBNull.Value.ToString());
                        command.Parameters.AddWithValue("@LoaiHopDong", loaiHopDong);
                        command.Parameters.AddWithValue("@MaGoi", maGoi);
                        command.Parameters.AddWithValue("@MaCN", maCN);
                        command.Parameters.AddWithValue("@MaPT", maPT);
                        command.Parameters.AddWithValue("@GhiChu", ghiChu);
                        command.Parameters.AddWithValue("@MaHV", maHV);
                        command.Parameters.AddWithValue("@MaHDHienTai", maHopDongHienTai);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Cập nhật hợp đồng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Không có thay đổi nào được lưu.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("CHECK constraint failed"))
                {
                    MessageBox.Show($"LỖI CẬP NHẬT: Giá trị '{loaiHopDong}' không hợp lệ cho cột 'Loại hợp đồng'. Vui lòng chọn 'Mới' hoặc 'Gia hạn' chính xác.", "Lỗi Ràng buộc", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (ex.Message.Contains("FOREIGN KEY constraint failed"))
                {
                    MessageBox.Show("LỖI CẬP NHẬT: Mã Gói tập, Mã PT, Mã Học viên, hoặc Mã Chi nhánh không tồn tại.", "Lỗi Khóa ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"LỖI CẬP NHẬT: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // =========================================================================
        // HÀM HỖ TRỢ VÀ SỰ KIỆN GIAO DIỆN
        // =========================================================================

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode(true);
            MaHDTextBox.IsReadOnly = true;
            MaHDTextBox.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#F0F0F0");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateHopDongData();
            LoadHopDongData(maHopDongHienTai); // Tải lại data để cập nhật trạng thái
            SetEditMode(false);
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Thực hiện chức năng In phiếu hợp đồng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private DateTime? ParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString)) return null;

            string[] formats = { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "d-M-yyyy", "d/M/yyyy" };
            if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                if (result.Year > 1900) return result;
            }
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                if (result.Year > 1900) return result;
            }
            return null;
        }

        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                comboBox.SelectedIndex = -1;
                return;
            }

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is ComboBoxItem item && item.Content.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
            comboBox.Text = value;
        }
    }
}

