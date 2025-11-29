using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media;
using System.Windows.Input;
using System.Globalization;
using System.Text;

namespace TFitnessApp
{
    public partial class DiemDanhPage : Page
    {
        private string chuoiKetNoi;
        private List<MoDonDuLieuDiemDanh> tatCaDiemDanh = new List<MoDonDuLieuDiemDanh>();

        // Khai báo hằng số cho placeholder và ComboBox
        private const string VAN_BAN_TIM_KIEM_MAC_DINH = "Tìm kiếm học viên...";
        private const string VAN_BAN_CHON_GIO = "Giờ";
        private const string VAN_BAN_CHON_PHUT = "Phút";

        public DiemDanhPage()
        {
            InitializeComponent();

            // Khởi tạo chuỗi kết nối
            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            chuoiKetNoi = $"Data Source={duongDanDB};";

            // Kiểm tra file database
            if (!File.Exists(duongDanDB))
            {
                MessageBox.Show($"Không tìm thấy cơ sở dữ liệu tại:\n{duongDanDB}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 1. Khởi tạo ComboBox cho Giờ/Phút
            KhoiTaoComboBoxThoiGian();

            // 2. Khởi tạo giá trị mặc định cho DatePicker và Placeholder
            dpNgayDiemDanh.SelectedDate = DateTime.Now;

            // Đặt mặc định text tìm kiếm
            if (txtTimKiem != null)
            {
                txtTimKiem.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                txtTimKiem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }

            // 3. Tải dữ liệu
            TaiThongKe();
            TaiDanhSachDiemDanh();
        }

        /// <summary>
        /// Khởi tạo dữ liệu cho các ComboBox chọn giờ và phút
        /// </summary>
        private void KhoiTaoComboBoxThoiGian()
        {
            // Tạo danh sách giờ (00-23) với placeholder
            var danhSachGio = Enumerable.Range(0, 24).Select(h => h.ToString("00")).ToList();
            danhSachGio.Insert(0, VAN_BAN_CHON_GIO);

            // Tạo danh sách phút (00-59) với placeholder
            var danhSachPhut = Enumerable.Range(0, 60).Select(m => m.ToString("00")).ToList();
            danhSachPhut.Insert(0, VAN_BAN_CHON_PHUT);

            cmbGioVao.ItemsSource = danhSachGio;
            cmbGioRa.ItemsSource = danhSachGio;
            cmbPhutVao.ItemsSource = danhSachPhut;
            cmbPhutRa.ItemsSource = danhSachPhut;

            // Chọn placeholder mặc định
            cmbGioVao.SelectedIndex = 0;
            cmbGioRa.SelectedIndex = 0;
            cmbPhutVao.SelectedIndex = 0;
            cmbPhutRa.SelectedIndex = 0;
        }

        /// <summary>
        /// Xử lý khi ComboBox nhận focus - tự động mở dropdown
        /// </summary>
        private void ComboBox_NhanFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                comboBox.IsDropDownOpen = true;
            }
        }

        #region Database Functions

        private void TaiThongKe()
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string homNay = DateTime.Today.ToString("yyyy-MM-dd");

                    // 1. Điểm danh hôm nay
                    string sql1 = "SELECT COUNT(DISTINCT MaHV) FROM DiemDanh WHERE date(NgayDD) = date(@homNay)";
                    using (SqliteCommand cmd = new SqliteCommand(sql1, conn))
                    {
                        cmd.Parameters.AddWithValue("@homNay", homNay);
                        txtSoDiemDanhHomNay.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // 2. Đang tập luyện
                    string sql2 = @"SELECT COUNT(DISTINCT MaHV) FROM DiemDanh 
                                     WHERE date(NgayDD) = date(@homNay) 
                                     AND ThoiGianVao IS NOT NULL 
                                     AND (ThoiGianRa IS NULL OR ThoiGianRa = '')";
                    using (SqliteCommand cmd = new SqliteCommand(sql2, conn))
                    {
                        cmd.Parameters.AddWithValue("@homNay", homNay);
                        txtDangTapLuyen.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // 3. Đã hoàn thành 
                    string sql3 = "SELECT COUNT(*) FROM LichTap WHERE TrangThai = 'Đã Hoàn Thành'";
                    using (SqliteCommand cmd = new SqliteCommand(sql3, conn))
                    {
                        txtDaHoanThanh.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // 4. Thời gian trung bình
                    string sql4 = @"SELECT AVG(
                                         CAST((julianday(datetime(NgayDD || ' ' || ThoiGianRa)) - 
                                             julianday(datetime(NgayDD || ' ' || ThoiGianVao))) * 24 AS REAL)
                                         ) 
                                         FROM DiemDanh 
                                         WHERE ThoiGianRa IS NOT NULL 
                                         AND ThoiGianRa != '' 
                                         AND ThoiGianVao IS NOT NULL";
                    using (SqliteCommand cmd = new SqliteCommand(sql4, conn))
                    {
                        var ketQua = cmd.ExecuteScalar();
                        if (ketQua != null && ketQua != DBNull.Value)
                        {
                            double gioTrungBinh = Convert.ToDouble(ketQua);
                            txtThoiGianTB.Text = $"{gioTrungBinh:F1}h";
                        }
                        else
                        {
                            txtThoiGianTB.Text = "0h";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thống kê: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DatMacDinhThongKe();
            }
        }

        private void DatMacDinhThongKe()
        {
            txtSoDiemDanhHomNay.Text = "0";
            txtDangTapLuyen.Text = "0";
            txtDaHoanThanh.Text = "0";
            txtThoiGianTB.Text = "0h";
        }

        private void TaiDanhSachDiemDanh()
        {
            if (lvLichSuDiemDanh == null) return;

            try
            {
                tatCaDiemDanh.Clear();

                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();

                    string sql = @"SELECT 
                                     d.MaDD,
                                     d.MaHV,
                                     h.HoTen,
                                     d.NgayDD,
                                     d.ThoiGianVao,
                                     d.ThoiGianRa,
                                     COALESCE(lt.TrangThai, 
                                         CASE 
                                             WHEN d.ThoiGianRa IS NULL OR d.ThoiGianRa = '' THEN 'Đang tập'
                                             ELSE 'Đã hoàn thành'
                                         END
                                     ) as TrangThai
                                    FROM DiemDanh d
                                    INNER JOIN HocVien h ON d.MaHV = h.MaHV
                                    LEFT JOIN LichTap lt ON d.MaHV = lt.MaHV 
                                     AND date(d.NgayDD) = date(lt.ThoiGianBatDau)
                                    ORDER BY d.NgayDD DESC, d.ThoiGianVao DESC";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new MoDonDuLieuDiemDanh
                            {
                                MaDiemDanh = reader["MaDD"].ToString(),
                                MaHocVien = reader["MaHV"].ToString(),
                                HoTen = reader["HoTen"].ToString(),
                                NgayDiemDanh = DateTime.Parse(reader["NgayDD"].ToString()).ToString("dd/MM/yyyy"),
                                ThoiGianVao = reader["ThoiGianVao"]?.ToString() ?? "--:--",
                                ThoiGianRa = reader["ThoiGianRa"]?.ToString() ?? "--:--",
                                TrangThai = reader["TrangThai"].ToString()
                            };

                            item.ChuCaiDau = string.IsNullOrEmpty(item.HoTen) ? "?" : item.HoTen[0].ToString().ToUpper();

                            // Set màu badge dựa trên trạng thái
                            if (item.TrangThai == "Đã Hoàn Thành" || item.TrangThai == "Đã hoàn thành")
                            {
                                item.MauTrangThai = "#51E689";
                            }
                            else if (item.TrangThai == "Chưa Hoàn Thành" || item.TrangThai == "Đang tập")
                            {
                                item.MauTrangThai = "#FF974E";
                            }
                            else if (item.TrangThai == "Đã Hủy")
                            {
                                item.MauTrangThai = "#DC3545";
                            }
                            else
                            {
                                item.MauTrangThai = "#7AAEFF";
                            }

                            tatCaDiemDanh.Add(item);
                        }
                    }
                }

                lvLichSuDiemDanh.ItemsSource = tatCaDiemDanh;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách:\n{ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Save Data

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Kiểm tra dữ liệu nhập cơ bản
                if (string.IsNullOrWhiteSpace(txtMaHocVien.Text))
                {
                    MessageBox.Show("Vui lòng nhập mã học viên!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtMaHocVien.Focus();
                    return;
                }

                string trangThai = (cmbTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (trangThai == "Chọn trạng thái" || trangThai == null)
                {
                    MessageBox.Show("Vui lòng chọn trạng thái!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbTrangThai.Focus();
                    return;
                }

                if (!dpNgayDiemDanh.SelectedDate.HasValue)
                {
                    MessageBox.Show("Vui lòng chọn ngày điểm danh!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    dpNgayDiemDanh.Focus();
                    return;
                }

                // 2. Lấy và kiểm tra thời gian vào
                string gioVao = cmbGioVao.SelectedValue?.ToString();
                string phutVao = cmbPhutVao.SelectedValue?.ToString();

                if (gioVao == VAN_BAN_CHON_GIO || phutVao == VAN_BAN_CHON_PHUT)
                {
                    MessageBox.Show("Vui lòng chọn đầy đủ thời gian vào (giờ và phút)!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbGioVao.Focus();
                    return;
                }
                string thoiGianVao = $"{gioVao}:{phutVao}";
                string thoiGianRa = "";

                // 3. Kiểm tra thời gian ra (chỉ khi là Check-out)
                if (trangThai == "Check-out")
                {
                    string gioRa = cmbGioRa.SelectedValue?.ToString();
                    string phutRa = cmbPhutRa.SelectedValue?.ToString();

                    if (gioRa == VAN_BAN_CHON_GIO || phutRa == VAN_BAN_CHON_PHUT)
                    {
                        MessageBox.Show("Vui lòng chọn đầy đủ thời gian ra (giờ và phút)!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        cmbGioRa.Focus();
                        return;
                    }
                    thoiGianRa = $"{gioRa}:{phutRa}";
                }

                // 4. Lưu dữ liệu vào database
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string maHV = txtMaHocVien.Text.Trim();
                    string ngayDD_DB = dpNgayDiemDanh.SelectedDate.Value.ToString("yyyy-MM-dd");

                    if (trangThai == "Check-in")
                    {
                        // Kiểm tra đã check-in chưa check-out
                        string sqlCheck = @"SELECT MaDD FROM DiemDanh 
                                             WHERE MaHV = @maHV AND date(NgayDD) = date(@ngayDD) 
                                             AND (ThoiGianRa IS NULL OR ThoiGianRa = '') 
                                             ORDER BY ThoiGianVao DESC LIMIT 1";
                        using (SqliteCommand cmdCheck = new SqliteCommand(sqlCheck, conn))
                        {
                            cmdCheck.Parameters.AddWithValue("@maHV", maHV);
                            cmdCheck.Parameters.AddWithValue("@ngayDD", ngayDD_DB);
                            var existingMaDD = cmdCheck.ExecuteScalar();

                            if (existingMaDD != null)
                            {
                                MessageBox.Show($"Học viên {maHV} đã Check-in và chưa Check-out trong ngày này.",
                                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                        }

                        // Thực hiện Check-in mới
                        string sqlInsert = "INSERT INTO DiemDanh (MaHV, NgayDD, ThoiGianVao) VALUES (@maHV, @ngayDD, @tgVao)";
                        using (SqliteCommand cmd = new SqliteCommand(sqlInsert, conn))
                        {
                            cmd.Parameters.AddWithValue("@maHV", maHV);
                            cmd.Parameters.AddWithValue("@ngayDD", ngayDD_DB);
                            cmd.Parameters.AddWithValue("@tgVao", thoiGianVao);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else if (trangThai == "Check-out")
                    {
                        // Cập nhật Check-out
                        string sqlUpdate = @"UPDATE DiemDanh 
                                             SET ThoiGianRa = @tgRa 
                                             WHERE MaHV = @maHV AND date(NgayDD) = date(@ngayDD) 
                                             AND (ThoiGianRa IS NULL OR ThoiGianRa = '') 
                                             ORDER BY ThoiGianVao DESC LIMIT 1";
                        using (SqliteCommand cmd = new SqliteCommand(sqlUpdate, conn))
                        {
                            cmd.Parameters.AddWithValue("@tgRa", thoiGianRa);
                            cmd.Parameters.AddWithValue("@maHV", maHV);
                            cmd.Parameters.AddWithValue("@ngayDD", ngayDD_DB);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected == 0)
                            {
                                MessageBox.Show($"Không tìm thấy lần Check-in chưa hoàn thành nào của học viên {maHV} trong ngày này.",
                                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }
                }

                MessageBox.Show($"{trangThai} thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reset form
                txtMaHocVien.Clear();
                cmbTrangThai.SelectedIndex = 0;
                cmbGioVao.SelectedIndex = 0;
                cmbPhutVao.SelectedIndex = 0;
                cmbGioRa.SelectedIndex = 0;
                cmbPhutRa.SelectedIndex = 0;
                dpNgayDiemDanh.SelectedDate = DateTime.Now;

                // Cập nhật placeholder tìm kiếm
                if (txtTimKiem != null)
                {
                    txtTimKiem.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                    txtTimKiem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
                }

                // Tải lại dữ liệu
                TaiThongKe();
                TaiDanhSachDiemDanh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu dữ liệu:\n{ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Search Functions

        /// <summary>
        /// Xử lý placeholder cho TextBox tìm kiếm khi nhận focus
        /// </summary>
        private void TxtTimKiem_NhanFocus(object sender, RoutedEventArgs e)
        {
            if (txtTimKiem.Text == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                txtTimKiem.Text = "";
                txtTimKiem.Foreground = Brushes.Black;
            }
        }

        /// <summary>
        /// Xử lý placeholder cho TextBox tìm kiếm khi mất focus
        /// </summary>
        private void TxtTimKiem_MatFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTimKiem.Text))
            {
                txtTimKiem.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                txtTimKiem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn phím Enter trong ô tìm kiếm
        /// </summary>
        private void TxtTimKiem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ThucHienTimKiem();
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng click vào nút Tìm
        /// </summary>
        private void BtnTimKiem_Click(object sender, RoutedEventArgs e)
        {
            ThucHienTimKiem();
        }

        /// <summary>
        /// Thực hiện tìm kiếm theo tên (có hoặc không dấu) hoặc mã học viên
        /// </summary>
        private void ThucHienTimKiem()
        {
            if (txtTimKiem == null || lvLichSuDiemDanh == null) return;

            string tuKhoa = txtTimKiem.Text.Trim();

            // Bỏ qua nếu đang hiển thị placeholder hoặc rỗng
            if (string.IsNullOrEmpty(tuKhoa) || tuKhoa == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                lvLichSuDiemDanh.ItemsSource = tatCaDiemDanh;
                return;
            }

            // Tìm kiếm không phân biệt dấu
            string tuKhoaKhongDau = BoQuyenDau(tuKhoa).ToLower();
            var ketQuaTimKiem = tatCaDiemDanh.Where(d =>
                BoQuyenDau(d.HoTen).ToLower().Contains(tuKhoaKhongDau) ||
                d.MaHocVien.ToLower().Contains(tuKhoa.ToLower())
            ).ToList();

            lvLichSuDiemDanh.ItemsSource = ketQuaTimKiem;

            // Hiển thị thông báo nếu không tìm thấy
            if (ketQuaTimKiem.Count == 0)
            {
                MessageBox.Show($"Không tìm thấy học viên nào với từ khóa: {tuKhoa}",
                    "Kết quả tìm kiếm", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Hàm bỏ dấu tiếng Việt
        /// </summary>
        private string BoQuyenDau(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder result = new StringBuilder();

            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            return result.ToString().Normalize(NormalizationForm.FormC);
        }

        #endregion

        #region Filter Functions

        /// <summary>
        /// Mở popup bộ lọc
        /// </summary>
        private void BtnMoBoLoc_Click(object sender, RoutedEventArgs e)
        {
            popupBoLoc.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Đóng popup bộ lọc
        /// </summary>
        private void BtnDongPopup_Click(object sender, RoutedEventArgs e)
        {
            popupBoLoc.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Xóa tất cả các tiêu chí lọc
        /// </summary>
        private void BtnXoaBoLoc_Click(object sender, RoutedEventArgs e)
        {
            txtLocHoTen.Clear();
            txtLocMaHV.Clear();
            dpLocTuNgay.SelectedDate = null;
            dpLocDenNgay.SelectedDate = null;
            cmbLocTrangThai.SelectedIndex = 0;

            // Hiển thị lại toàn bộ danh sách
            lvLichSuDiemDanh.ItemsSource = tatCaDiemDanh;

            MessageBox.Show("Đã xóa bộ lọc!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Áp dụng bộ lọc
        /// </summary>
        private void BtnApDungBoLoc_Click(object sender, RoutedEventArgs e)
        {
            var ketQuaLoc = tatCaDiemDanh.AsEnumerable();

            // Lọc theo họ tên
            if (!string.IsNullOrWhiteSpace(txtLocHoTen.Text))
            {
                string hoTenLoc = txtLocHoTen.Text.Trim();
                ketQuaLoc = ketQuaLoc.Where(d =>
                    BoQuyenDau(d.HoTen).ToLower().Contains(BoQuyenDau(hoTenLoc).ToLower())
                );
            }

            // Lọc theo mã học viên
            if (!string.IsNullOrWhiteSpace(txtLocMaHV.Text))
            {
                string maHVLoc = txtLocMaHV.Text.Trim().ToLower(); // 1. Chuẩn hóa chuỗi tìm kiếm
                ketQuaLoc = ketQuaLoc.Where(d =>
                    d.MaHocVien.ToLower().Contains(maHVLoc) // 2. So sánh bằng cách chuyển về chữ thường
                );
            }

            // Lọc theo khoảng ngày
            if (dpLocTuNgay.SelectedDate.HasValue)
            {
                DateTime tuNgay = dpLocTuNgay.SelectedDate.Value.Date;
                ketQuaLoc = ketQuaLoc.Where(d =>
                {
                    if (DateTime.TryParseExact(d.NgayDiemDanh, "dd/MM/yyyy",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ngayDD))
                    {
                        return ngayDD >= tuNgay;
                    }
                    return false;
                });
            }

            if (dpLocDenNgay.SelectedDate.HasValue)
            {
                DateTime denNgay = dpLocDenNgay.SelectedDate.Value.Date;
                ketQuaLoc = ketQuaLoc.Where(d =>
                {
                    if (DateTime.TryParseExact(d.NgayDiemDanh, "dd/MM/yyyy",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ngayDD))
                    {
                        return ngayDD <= denNgay;
                    }
                    return false;
                });
            }

            // Lọc theo trạng thái
            string trangThaiLoc = (cmbLocTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (trangThaiLoc != "Tất cả" && !string.IsNullOrEmpty(trangThaiLoc))
            {
                ketQuaLoc = ketQuaLoc.Where(d =>
                    d.TrangThai.Equals(trangThaiLoc, StringComparison.OrdinalIgnoreCase)
                );
            }

            // Chuyển về List và hiển thị
            var danhSachLoc = ketQuaLoc.ToList();
            lvLichSuDiemDanh.ItemsSource = danhSachLoc;

            // Đóng popup
            popupBoLoc.Visibility = Visibility.Collapsed;

            // Thông báo kết quả
            MessageBox.Show($"Đã lọc được {danhSachLoc.Count} kết quả!",
                "Kết quả lọc", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        private void lvLichSuDiemDanh_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Thêm logic hiển thị chi tiết điểm danh đã chọn nếu cần
        }
    }

    public class MoDonDuLieuDiemDanh
    {
        public string MaDiemDanh { get; set; }
        public string MaHocVien { get; set; }
        public string HoTen { get; set; }
        public string ChuCaiDau { get; set; }
        public string NgayDiemDanh { get; set; }
        public string ThoiGianVao { get; set; }
        public string ThoiGianRa { get; set; }
        public string TrangThai { get; set; }
        public string MauTrangThai { get; set; }
    }
}