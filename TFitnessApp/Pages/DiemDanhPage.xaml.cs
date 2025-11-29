using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;

namespace TFitnessApp
{
    public partial class DiemDanhPage : Page
    {
        private string connectionString;
        private List<DiemDanhViewModel> allDiemDanh = new List<DiemDanhViewModel>();

        public DiemDanhPage()
        {
            try
            {
                // Khởi tạo connection string với đường dẫn đầy đủ
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitnessDB.db");
                connectionString = $"Data Source={dbPath};Version=3;";
                // Kiểm tra file database
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show($"CẢNH BÁO: Không tìm thấy file database tại:\n{dbPath}\n\nVui lòng kiểm tra đường dẫn.",
                        "Thiếu Database", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                InitializeComponent();

                // Khởi tạo giá trị mặc định
                dpNgayDiemDanh.SelectedDate = DateTime.Now;

                // Load dữ liệu
                LoadStatistics();
                LoadDiemDanhHistory();

                MessageBox.Show("Page DiemDanh đã load thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI KHỞI TẠO TRANG ĐIỂM DANH:\n\n{ex.Message}\n\nChi tiết:\n{ex.StackTrace}",
                    "Lỗi nghiêm trọng", MessageBoxButton.OK, MessageBoxImage.Error);

                // Set giá trị mặc định để tránh crash
                SetDefaultValues();
            }
        }

        private void SetDefaultValues()
        {
            try
            {
                if (txtSoDiemDanhHomNay != null) txtSoDiemDanhHomNay.Text = "0";
                if (txtDangTapLuyen != null) txtDangTapLuyen.Text = "0";
                if (txtDaHoanThanh != null) txtDaHoanThanh.Text = "0";
                if (txtThoiGianTB != null) txtThoiGianTB.Text = "0m";
            }
            catch { }
        }

        private void LoadStatistics()
        {
            try
            {
                // Kiểm tra connection string
                if (string.IsNullOrEmpty(connectionString))
                {
                    MessageBox.Show("Connection string chưa được khởi tạo!", "Lỗi");
                    SetDefaultValues();
                    return;
                }

                using (SqliteConnection conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    DateTime today = DateTime.Today;

                    // Điểm danh hôm nay
                    try
                    {
                        string sqlToday = @"SELECT COUNT(DISTINCT MaHocVien) 
                                           FROM DiemDanh 
                                           WHERE date(NgayDiemDanh) = date(@today)";
                        using (SqliteCommand cmd = new SqliteCommand(sqlToday, conn))
                        {
                            cmd.Parameters.AddWithValue("@today", today.ToString("yyyy-MM-dd"));
                            txtSoDiemDanhHomNay.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                        }
                    }
                    catch (Exception ex)
                    {
                        txtSoDiemDanhHomNay.Text = "0";
                        Console.WriteLine($"Lỗi query điểm danh hôm nay: {ex.Message}");
                    }

                    // Đang tập luyện
                    try
                    {
                        string sqlDangTap = @"SELECT COUNT(DISTINCT d1.MaHocVien) 
                                             FROM DiemDanh d1
                                             WHERE d1.TrangThai = 'Check-in'
                                             AND date(d1.NgayDiemDanh) = date(@today)
                                             AND NOT EXISTS (
                                                 SELECT 1 FROM DiemDanh d2 
                                                 WHERE d2.MaHocVien = d1.MaHocVien 
                                                 AND d2.TrangThai = 'Check-out'
                                                 AND date(d2.NgayDiemDanh) = date(@today)
                                                 AND d2.ThoiGianRa > d1.ThoiGianVao
                                             )";
                        using (SqliteCommand cmd = new SqliteCommand(sqlDangTap, conn))
                        {
                            cmd.Parameters.AddWithValue("@today", today.ToString("yyyy-MM-dd"));
                            txtDangTapLuyen.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                        }
                    }
                    catch (Exception ex)
                    {
                        txtDangTapLuyen.Text = "0";
                        Console.WriteLine($"Lỗi query đang tập luyện: {ex.Message}");
                    }

                    // Đã hoàn thành
                    try
                    {
                        string sqlHoanThanh = @"SELECT COUNT(*) 
                                               FROM LichTap 
                                               WHERE TrangThai = 'Đã Hoàn Thành'";
                        using (SqliteCommand cmd = new SqliteCommand(sqlHoanThanh, conn))
                        {
                            txtDaHoanThanh.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                        }
                    }
                    catch (Exception ex)
                    {
                        txtDaHoanThanh.Text = "0";
                        Console.WriteLine($"Lỗi query hoàn thành: {ex.Message}");
                    }

                    // Thời gian trung bình
                    try
                    {
                        string sqlAvgTime = @"SELECT AVG(
                                                CAST((julianday(datetime(NgayDiemDanh || ' ' || ThoiGianRa)) - 
                                                      julianday(datetime(NgayDiemDanh || ' ' || ThoiGianVao))) * 24 * 60 AS INTEGER)
                                              ) 
                                              FROM DiemDanh 
                                              WHERE TrangThai = 'Check-out' 
                                              AND ThoiGianRa IS NOT NULL 
                                              AND ThoiGianVao IS NOT NULL";
                        using (SqliteCommand cmd = new SqliteCommand(sqlAvgTime, conn))
                        {
                            var result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                int avgMinutes = Convert.ToInt32(result);
                                txtThoiGianTB.Text = $"{avgMinutes}m";
                            }
                            else
                            {
                                txtThoiGianTB.Text = "0m";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        txtThoiGianTB.Text = "0m";
                        Console.WriteLine($"Lỗi query thời gian TB: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thống kê:\n{ex.Message}\n\nCó thể database chưa được tạo hoặc thiếu bảng.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                SetDefaultValues();
            }
        }

        private void LoadDiemDanhHistory()
        {
            try
            {
                allDiemDanh.Clear();

                if (string.IsNullOrEmpty(connectionString))
                {
                    MessageBox.Show("Connection string chưa được khởi tạo!", "Lỗi");
                    return;
                }

                using (SqliteConnection conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT d.MaDiemDanh, d.MaHocVien, h.HoTen, d.TrangThai, 
                                         d.ThoiGianVao, d.ThoiGianRa, d.NgayDiemDanh
                                  FROM DiemDanh d
                                  INNER JOIN HocVien h ON d.MaHocVien = h.MaHocVien
                                  ORDER BY d.NgayDiemDanh DESC, d.ThoiGianVao DESC";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new DiemDanhViewModel
                            {
                                MaDiemDanh = reader["MaDiemDanh"].ToString(),
                                MaHocVien = reader["MaHocVien"].ToString(),
                                HoTen = reader["HoTen"].ToString(),
                                TrangThai = reader["TrangThai"].ToString(),
                                ThoiGianVao = reader["ThoiGianVao"]?.ToString() ?? "",
                                ThoiGianRa = reader["ThoiGianRa"]?.ToString() ?? "",
                                NgayDiemDanh = Convert.ToDateTime(reader["NgayDiemDanh"]).ToString("dd/MM/yyyy")
                            };

                            item.Avatar = string.IsNullOrEmpty(item.HoTen) ? "?" : item.HoTen.Substring(0, 1).ToUpper();
                            item.TrangThaiBadgeColor = item.TrangThai == "Check-in" ? "#51E689" : "#FF974E";

                            allDiemDanh.Add(item);
                        }
                    }
                }

                lvLichSuDiemDanh.ItemsSource = allDiemDanh;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải lịch sử:\n{ex.Message}\n\nCó thể bảng DiemDanh hoặc HocVien chưa tồn tại.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cboTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboTrangThai.SelectedItem == null) return;

            var selectedItem = cboTrangThai.SelectedItem as ComboBoxItem;
            string tag = selectedItem?.Tag?.ToString() ?? "";

            if (tag == "checkin")
            {
                txtThoiGianVao.IsEnabled = true;
                txtThoiGianVao.Background = Brushes.White;
                txtThoiGianRa.IsEnabled = false;
                txtThoiGianRa.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                txtThoiGianRa.Text = "";
            }
            else if (tag == "checkout")
            {
                txtThoiGianVao.IsEnabled = false;
                txtThoiGianVao.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                txtThoiGianVao.Text = "";
                txtThoiGianRa.IsEnabled = true;
                txtThoiGianRa.Background = Brushes.White;
            }
            else
            {
                txtThoiGianVao.IsEnabled = true;
                txtThoiGianVao.Background = Brushes.White;
                txtThoiGianRa.IsEnabled = true;
                txtThoiGianRa.Background = Brushes.White;
            }

            txtErrorTrangThai.Visibility = Visibility.Collapsed;
            cboTrangThai.Style = (Style)FindResource("ComboBoxStyle");
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            ResetErrorIndicators();

            bool isValid = true;
            List<string> errors = new List<string>();

            string maHocVien = txtMaHocVien.Text.Trim();
            if (string.IsNullOrEmpty(maHocVien))
            {
                isValid = false;
                errors.Add("Vui lòng nhập mã học viên");
                ShowError(txtErrorMaHocVien, txtMaHocVien);
            }
            else if (!CheckMaHocVienExists(maHocVien))
            {
                isValid = false;
                errors.Add("Mã học viên không tồn tại trong hệ thống");
                ShowError(txtErrorMaHocVien, txtMaHocVien);
            }

            var selectedItem = cboTrangThai.SelectedItem as ComboBoxItem;
            string trangThaiTag = selectedItem?.Tag?.ToString() ?? "";
            string trangThai = "";

            if (trangThaiTag == "placeholder" || string.IsNullOrEmpty(trangThaiTag))
            {
                isValid = false;
                errors.Add("Vui lòng chọn trạng thái");
                ShowError(txtErrorTrangThai, cboTrangThai);
            }
            else
            {
                trangThai = trangThaiTag == "checkin" ? "Check-in" : "Check-out";
            }

            TimeSpan? thoiGianVao = null;
            TimeSpan? thoiGianRa = null;

            if (trangThai == "Check-in")
            {
                if (string.IsNullOrEmpty(txtThoiGianVao.Text.Trim()))
                {
                    isValid = false;
                    errors.Add("Vui lòng nhập thời gian vào");
                    ShowError(txtErrorThoiGianVao, txtThoiGianVao);
                }
                else if (!TimeSpan.TryParse(txtThoiGianVao.Text.Trim(), out TimeSpan tempVao))
                {
                    isValid = false;
                    errors.Add("Thời gian vào không đúng định dạng (HH:mm)");
                    ShowError(txtErrorThoiGianVao, txtThoiGianVao);
                }
                else
                {
                    thoiGianVao = tempVao;
                }
            }
            else if (trangThai == "Check-out")
            {
                if (string.IsNullOrEmpty(txtThoiGianRa.Text.Trim()))
                {
                    isValid = false;
                    errors.Add("Vui lòng nhập thời gian ra");
                    ShowError(txtErrorThoiGianRa, txtThoiGianRa);
                }
                else if (!TimeSpan.TryParse(txtThoiGianRa.Text.Trim(), out TimeSpan tempRa))
                {
                    isValid = false;
                    errors.Add("Thời gian ra không đúng định dạng (HH:mm)");
                    ShowError(txtErrorThoiGianRa, txtThoiGianRa);
                }
                else
                {
                    thoiGianRa = tempRa;
                }
            }

            if (!dpNgayDiemDanh.SelectedDate.HasValue)
            {
                isValid = false;
                errors.Add("Vui lòng chọn ngày điểm danh");
                ShowError(txtErrorNgayDiemDanh, dpNgayDiemDanh);
            }

            DateTime ngayDiemDanh = dpNgayDiemDanh.SelectedDate ?? DateTime.Now;

            if (isValid && thoiGianVao.HasValue)
            {
                DateTime thoiDiemVao = ngayDiemDanh.Date.Add(thoiGianVao.Value);
                if (!CheckLichTap(maHocVien, thoiDiemVao, trangThai))
                {
                    isValid = false;
                    errors.Add("Thời gian điểm danh không khớp với lịch tập (sai lệch > 30 phút)");
                    ShowError(txtErrorNgayDiemDanh, dpNgayDiemDanh);
                    ShowError(txtErrorThoiGianVao, txtThoiGianVao);
                }
            }

            if (isValid && thoiGianRa.HasValue)
            {
                DateTime thoiDiemRa = ngayDiemDanh.Date.Add(thoiGianRa.Value);
                if (!CheckLichTap(maHocVien, thoiDiemRa, trangThai))
                {
                    isValid = false;
                    errors.Add("Thời gian điểm danh không khớp với lịch tập (sai lệch > 30 phút)");
                    ShowError(txtErrorNgayDiemDanh, dpNgayDiemDanh);
                    ShowError(txtErrorThoiGianRa, txtThoiGianRa);
                }
            }

            if (!isValid)
            {
                string errorMessage = string.Join("\n• ", errors);
                MessageBox.Show($"Lưu không thành công:\n\n• {errorMessage}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                SaveDiemDanh(maHocVien, trangThai, thoiGianVao, thoiGianRa, ngayDiemDanh);
                MessageBox.Show("Lưu điểm danh thành công!", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                LoadStatistics();
                LoadDiemDanhHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CheckMaHocVienExists(string maHocVien)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM HocVien WHERE MaHocVien = @maHocVien";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@maHocVien", maHocVien);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private bool CheckLichTap(string maHocVien, DateTime thoiDiemDiemDanh, string trangThai)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT ThoiGianBatDau, ThoiGianKetThuc, MaLich
                                  FROM LichTap 
                                  WHERE MaHocVien = @maHocVien 
                                  AND date(ThoiGianBatDau) = date(@ngay)
                                  AND TrangThai != 'Đã Hủy'";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@maHocVien", maHocVien);
                        cmd.Parameters.AddWithValue("@ngay", thoiDiemDiemDanh.Date.ToString("yyyy-MM-dd"));

                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                DateTime thoiGianBatDau = Convert.ToDateTime(reader["ThoiGianBatDau"]);
                                DateTime thoiGianKetThuc = Convert.ToDateTime(reader["ThoiGianKetThuc"]);

                                TimeSpan saiLech;
                                if (trangThai == "Check-in")
                                {
                                    saiLech = (thoiDiemDiemDanh - thoiGianBatDau).Duration();
                                }
                                else
                                {
                                    saiLech = (thoiDiemDiemDanh - thoiGianKetThuc).Duration();
                                }

                                return saiLech.TotalMinutes <= 30;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private void SaveDiemDanh(string maHocVien, string trangThai, TimeSpan? thoiGianVao,
            TimeSpan? thoiGianRa, DateTime ngayDiemDanh)
        {
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (SqliteTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string maDiemDanh = "DD" + DateTime.Now.ToString("yyyyMMddHHmmss");
                        string sqlInsert = @"INSERT INTO DiemDanh 
                                           (MaDiemDanh, MaHocVien, TrangThai, ThoiGianVao, ThoiGianRa, NgayDiemDanh) 
                                           VALUES (@maDiemDanh, @maHocVien, @trangThai, @thoiGianVao, @thoiGianRa, @ngayDiemDanh)";

                        using (SqliteCommand cmd = new SqliteCommand(sqlInsert, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@maDiemDanh", maDiemDanh);
                            cmd.Parameters.AddWithValue("@maHocVien", maHocVien);
                            cmd.Parameters.AddWithValue("@trangThai", trangThai);
                            cmd.Parameters.AddWithValue("@thoiGianVao",
                                thoiGianVao.HasValue ? thoiGianVao.Value.ToString(@"hh\:mm") : (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@thoiGianRa",
                                thoiGianRa.HasValue ? thoiGianRa.Value.ToString(@"hh\:mm") : (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ngayDiemDanh", ngayDiemDanh.ToString("yyyy-MM-dd"));
                            cmd.ExecuteNonQuery();
                        }

                        if (trangThai == "Check-out")
                        {
                            string sqlUpdate = @"UPDATE LichTap 
                                               SET TrangThai = 'Đã Hoàn Thành' 
                                               WHERE MaHocVien = @maHocVien 
                                               AND date(ThoiGianBatDau) = date(@ngay)
                                               AND TrangThai = 'Chưa Hoàn Thành'";

                            using (SqliteCommand cmd = new SqliteCommand(sqlUpdate, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@maHocVien", maHocVien);
                                cmd.Parameters.AddWithValue("@ngay", ngayDiemDanh.ToString("yyyy-MM-dd"));
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else if (trangThai == "Check-in")
                        {
                            string sqlUpdate = @"UPDATE LichTap 
                                               SET TrangThai = 'Chưa Hoàn Thành' 
                                               WHERE MaHocVien = @maHocVien 
                                               AND date(ThoiGianBatDau) = date(@ngay)
                                               AND TrangThai NOT IN ('Đã Hoàn Thành', 'Đã Hủy')";

                            using (SqliteCommand cmd = new SqliteCommand(sqlUpdate, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@maHocVien", maHocVien);
                                cmd.Parameters.AddWithValue("@ngay", ngayDiemDanh.ToString("yyyy-MM-dd"));
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private void ShowError(TextBlock errorIcon, Control input)
        {
            errorIcon.Visibility = Visibility.Visible;

            if (input is TextBox textBox)
            {
                textBox.Style = (Style)FindResource("ErrorTextBoxStyle");
            }
            else if (input is ComboBox comboBox)
            {
                comboBox.Style = (Style)FindResource("ErrorComboBoxStyle");
            }
            else if (input is DatePicker datePicker)
            {
                datePicker.Style = (Style)FindResource("ErrorDatePickerStyle");
            }
        }

        private void ResetErrorIndicators()
        {
            txtErrorMaHocVien.Visibility = Visibility.Collapsed;
            txtErrorTrangThai.Visibility = Visibility.Collapsed;
            txtErrorThoiGianVao.Visibility = Visibility.Collapsed;
            txtErrorThoiGianRa.Visibility = Visibility.Collapsed;
            txtErrorNgayDiemDanh.Visibility = Visibility.Collapsed;

            txtMaHocVien.Style = (Style)FindResource("PlaceholderTextBoxStyle");
            cboTrangThai.Style = (Style)FindResource("ComboBoxStyle");
            txtThoiGianVao.Style = null;
            txtThoiGianRa.Style = null;
            dpNgayDiemDanh.Style = (Style)FindResource("DatePickerStyle");
        }

        private void ClearForm()
        {
            txtMaHocVien.Text = "";
            cboTrangThai.SelectedIndex = 0;
            txtThoiGianVao.Text = "";
            txtThoiGianRa.Text = "";
            dpNgayDiemDanh.SelectedDate = DateTime.Now;
            txtThoiGianVao.IsEnabled = true;
            txtThoiGianRa.IsEnabled = true;
            txtThoiGianVao.Background = Brushes.White;
            txtThoiGianRa.Background = Brushes.White;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.ToLower().Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                lvLichSuDiemDanh.ItemsSource = allDiemDanh;
            }
            else
            {
                var filtered = allDiemDanh.Where(d =>
                    d.HoTen.ToLower().Contains(searchText) ||
                    d.MaHocVien.ToLower().Contains(searchText)).ToList();
                lvLichSuDiemDanh.ItemsSource = filtered;
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.Tag as DiemDanhViewModel;
            if (item != null)
            {
                txtMaHocVien.Text = item.MaHocVien;
                txtThoiGianVao.Text = item.ThoiGianVao;
                txtThoiGianRa.Text = item.ThoiGianRa;

                DateTime ngay;
                if (DateTime.TryParseExact(item.NgayDiemDanh, "dd/MM/yyyy", null,
                    System.Globalization.DateTimeStyles.None, out ngay))
                {
                    dpNgayDiemDanh.SelectedDate = ngay;
                }

                if (item.TrangThai == "Check-in")
                {
                    cboTrangThai.SelectedIndex = 1;
                }
                else
                {
                    cboTrangThai.SelectedIndex = 2;
                }
            }
        }
    }

    public class DiemDanhViewModel
    {
        public string MaDiemDanh { get; set; }
        public string MaHocVien { get; set; }
        public string HoTen { get; set; }
        public string Avatar { get; set; }
        public string TrangThai { get; set; }
        public string ThoiGianVao { get; set; }
        public string ThoiGianRa { get; set; }
        public string NgayDiemDanh { get; set; }
        public string TrangThaiBadgeColor { get; set; }
    }
}