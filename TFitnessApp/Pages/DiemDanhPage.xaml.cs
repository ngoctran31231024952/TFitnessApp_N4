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

        #region Logic Helper Functions

        /// <summary>
        /// Tính toán trạng thái lịch tập dựa trên thời gian hiện tại
        /// </summary>
        private string TinhTrangThaiLichTap(DateTime thoiGianBatDau, DateTime thoiGianKetThuc)
        {
            DateTime now = DateTime.Now;

            if (now < thoiGianBatDau)
            {
                return "Chưa bắt đầu";
            }

            if (now >= thoiGianBatDau && now < thoiGianKetThuc)
            {
                return "Đang tập";
            }

            if (now >= thoiGianKetThuc)
            {
                return "Đã hoàn thành";
            }

            return "Không xác định";
        }

        /// <summary>
        /// Cập nhật trạng thái của một lịch tập trong CSDL
        /// </summary>
        private void CapNhatTrangThaiLichTap(string maHV, string tenBuoiTap, string trangThaiMoi)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    // Cập nhật trạng thái cho Lịch tập của học viên đó trong ngày hôm nay
                    string sql = @"UPDATE LichTap 
                                   SET TrangThai = @trangThaiMoi 
                                   WHERE MaHV = @maHV AND TenBuoiTap = @tenBuoiTap 
                                   AND date(ThoiGianBatDau) = date('now', 'localtime')";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@trangThaiMoi", trangThaiMoi);
                        cmd.Parameters.AddWithValue("@maHV", maHV);
                        cmd.Parameters.AddWithValue("@tenBuoiTap", tenBuoiTap);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật trạng thái LichTap: {ex.Message}", "Lỗi Cập nhật", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Kiểm tra xem học viên có lịch tập phù hợp tại thời điểm điểm danh hay không (sai lệch tối đa 1 tiếng)
        /// </summary>
        private bool KiemTraLichTapHopLe(string maHV, string tenBuoiTap, string maChiNhanh, DateTime thoiGianDiemDanh, string trangThaiDiemDanh, out DateTime thoiGianBatDau, out DateTime thoiGianKetThuc)
        {
            thoiGianBatDau = DateTime.MinValue;
            thoiGianKetThuc = DateTime.MaxValue;

            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string ngayDiemDanh = thoiGianDiemDanh.ToString("yyyy-MM-dd");

                    // Truy vấn để tìm lịch tập phù hợp với ngày, mã HV, tên buổi và chi nhánh
                    string sql = @"
                        SELECT ThoiGianBatDau, ThoiGianKetThuc
                        FROM LichTap
                        WHERE MaHV = @maHV
                          AND TenBuoiTap = @tenBuoiTap
                          AND MaCN = @maCN
                          AND date(ThoiGianBatDau) = date(@ngayDiemDanh)
                          LIMIT 1";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@maHV", maHV);
                        cmd.Parameters.AddWithValue("@tenBuoiTap", tenBuoiTap);
                        cmd.Parameters.AddWithValue("@maCN", maChiNhanh);
                        cmd.Parameters.AddWithValue("@ngayDiemDanh", ngayDiemDanh);

                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                thoiGianBatDau = DateTime.Parse(reader["ThoiGianBatDau"].ToString());
                                thoiGianKetThuc = DateTime.Parse(reader["ThoiGianKetThuc"].ToString());

                                // Logic kiểm tra sai lệch tối đa 1 tiếng
                                if (trangThaiDiemDanh == "Check-in")
                                {
                                    DateTime checkinMin = thoiGianBatDau.AddHours(-1);
                                    DateTime checkinMax = thoiGianBatDau.AddHours(1);

                                    // Check-in hợp lệ nếu nằm trong khoảng [TGBatDau - 1h, TGBatDau + 1h]
                                    if (thoiGianDiemDanh >= checkinMin && thoiGianDiemDanh <= checkinMax)
                                    {
                                        return true;
                                    }
                                }
                                else if (trangThaiDiemDanh == "Check-out")
                                {
                                    // Check-out hợp lệ nếu diễn ra sau thời gian bắt đầu lớp
                                    if (thoiGianDiemDanh >= thoiGianBatDau)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kiểm tra lịch tập: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return false;
        }

        #endregion

        #region Initialization and Data Loading

        public DiemDanhPage()
        {
            InitializeComponent();
            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            chuoiKetNoi = $"Data Source={duongDanDB};";

            if (!File.Exists(duongDanDB))
            {
                MessageBox.Show($"Không tìm thấy cơ sở dữ liệu tại:\n{duongDanDB}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            KhoiTaoComboBoxThoiGian();
            dpNgayDiemDanh.SelectedDate = DateTime.Now;

            if (txtTimKiem != null)
            {
                txtTimKiem.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                txtTimKiem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }

            TaiThongKe();
            TaiDanhSachDiemDanh();

            DatThoiGianMacDinh();
        }

        private void DatThoiGianMacDinh()
        {
            DateTime now = DateTime.Now;
            cmbGioVao.SelectedValue = now.Hour.ToString("00");
            cmbPhutVao.SelectedValue = now.Minute.ToString("00");
            cmbGioRa.SelectedValue = now.Hour.ToString("00");
            cmbPhutRa.SelectedValue = now.Minute.ToString("00");
        }

        private void KhoiTaoComboBoxThoiGian()
        {
            var danhSachGio = Enumerable.Range(0, 24).Select(h => h.ToString("00")).ToList();
            danhSachGio.Insert(0, VAN_BAN_CHON_GIO);

            var danhSachPhut = Enumerable.Range(0, 60).Select(m => m.ToString("00")).ToList();
            danhSachPhut.Insert(0, VAN_BAN_CHON_PHUT);

            cmbGioVao.ItemsSource = danhSachGio;
            cmbGioRa.ItemsSource = danhSachGio;
            cmbPhutVao.ItemsSource = danhSachPhut;
            cmbPhutRa.ItemsSource = danhSachPhut;

            cmbGioVao.SelectedIndex = 0;
            cmbGioRa.SelectedIndex = 0;
            cmbPhutVao.SelectedIndex = 0;
            cmbPhutRa.SelectedIndex = 0;
        }

        private void ComboBox_NhanFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                comboBox.IsDropDownOpen = true;
            }
        }

        private void DatMacDinhThongKe()
        {
            txtSoDiemDanhHomNay.Text = "0";
            txtDangTapLuyen.Text = "0";
            txtDaHoanThanh.Text = "0";
            txtThoiGianTB.Text = "0h";
        }

        private void TaiThongKe()
        {
            // (Giữ nguyên)
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

        private void TaiDanhSachDiemDanh()
        {
            if (lvLichSuDiemDanh == null) return;

            try
            {
                tatCaDiemDanh.Clear();

                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();

                    // TRẠNG THÁI MỚI: Truy vấn LichTap để xác định trạng thái thực tế
                    string sql = @"
                        SELECT d.MaDD, d.MaHV, h.HoTen, d.NgayDD, d.ThoiGianVao, d.ThoiGianRa,
                               lt.TrangThai AS LichTapTrangThai, lt.ThoiGianBatDau, lt.ThoiGianKetThuc
                        FROM DiemDanh d
                        INNER JOIN HocVien h ON d.MaHV = h.MaHV
                        LEFT JOIN LichTap lt ON d.MaHV = lt.MaHV AND date(d.NgayDD) = date(lt.ThoiGianBatDau)
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
                                TrangThai = reader["LichTapTrangThai"]?.ToString() ?? "" // Mặc định lấy trạng thái từ LichTap
                            };

                            // --- LOGIC XÁC ĐỊNH TRẠNG THÁI HIỂN THỊ (YÊU CẦU 0) ---
                            // Nếu LichTap không có hoặc trạng thái không rõ ràng, cố gắng suy luận:
                            if (string.IsNullOrEmpty(item.TrangThai) || item.TrangThai == "Đang tập")
                            {
                                if (reader["ThoiGianBatDau"] != DBNull.Value && reader["ThoiGianKetThuc"] != DBNull.Value)
                                {
                                    DateTime tgBatDau = DateTime.Parse(reader["ThoiGianBatDau"].ToString());
                                    DateTime tgKetThuc = DateTime.Parse(reader["ThoiGianKetThuc"].ToString());
                                    item.TrangThai = TinhTrangThaiLichTap(tgBatDau, tgKetThuc);
                                }
                                else if (item.ThoiGianVao != "--:--" && item.ThoiGianRa == "--:--")
                                {
                                    item.TrangThai = "Đang tập";
                                }
                                else if (item.ThoiGianVao != "--:--" && item.ThoiGianRa != "--:--")
                                {
                                    item.TrangThai = "Đã hoàn thành";
                                }
                            }

                            item.ChuCaiDau = string.IsNullOrEmpty(item.HoTen) ? "?" : item.HoTen[0].ToString().ToUpper();

                            // Set màu badge dựa trên trạng thái
                            if (item.TrangThai == "Đã Hoàn Thành" || item.TrangThai == "Đã hoàn thành")
                            {
                                item.MauTrangThai = "#51E689"; // Xanh lá
                            }
                            else if (item.TrangThai == "Đang tập" || item.TrangThai == "Chưa hoàn thành")
                            {
                                item.MauTrangThai = "#FF974E"; // Cam
                            }
                            else if (item.TrangThai == "Chưa bắt đầu")
                            {
                                item.MauTrangThai = "#7AAEFF"; // Xanh dương nhạt
                            }
                            else if (item.TrangThai == "Đã hủy")
                            {
                                item.MauTrangThai = "#DC3545"; // Đỏ
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
            // Tự động cập nhật thời gian hiện tại vào form trước khi lưu (YÊU CẦU 2)
            DatThoiGianMacDinh();

            try
            {
                // 1. Thu thập và kiểm tra dữ liệu nhập cơ bản
                string maHV = txtMaHocVien.Text.Trim();
                string tenBuoiTap = txtTenBuoiTap.Text.Trim();
                string maChiNhanh = txtMaChiNhanh.Text.Trim();

                if (!dpNgayDiemDanh.SelectedDate.HasValue)
                {
                    MessageBox.Show("Vui lòng chọn ngày điểm danh!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string ngayDD_DB = dpNgayDiemDanh.SelectedDate.Value.ToString("yyyy-MM-dd");

                if (string.IsNullOrWhiteSpace(maHV) || string.IsNullOrWhiteSpace(tenBuoiTap) || string.IsNullOrWhiteSpace(maChiNhanh))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ Mã học viên, Tên buổi tập và Mã chi nhánh!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string trangThaiDiemDanh = (cmbTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (trangThaiDiemDanh == "Chọn trạng thái" || trangThaiDiemDanh == null)
                {
                    MessageBox.Show("Vui lòng chọn trạng thái!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime thoiGianDiemDanh = DateTime.Now;
                string tgDiemDanhGioPhut = thoiGianDiemDanh.ToString("HH:mm");

                // Chuẩn bị thời gian vào/ra
                string thoiGianVao = (trangThaiDiemDanh == "Check-in") ? tgDiemDanhGioPhut : null;
                string thoiGianRa = (trangThaiDiemDanh == "Check-out") ? tgDiemDanhGioPhut : null;

                // 2. Kiểm tra lịch tập hợp lệ (YÊU CẦU 3)
                DateTime thoiGianBatDauLich;
                DateTime thoiGianKetThucLich;

                // GỌI HÀM ĐÃ SỬA: Truyền trangThaiDiemDanh vào
                if (!KiemTraLichTapHopLe(maHV, tenBuoiTap, maChiNhanh, thoiGianDiemDanh, trangThaiDiemDanh, out thoiGianBatDauLich, out thoiGianKetThucLich))
                {
                    MessageBox.Show("Lưu không thành công! Học viên không có lịch tập phù hợp tại thời điểm này (sai lệch tối đa 1 tiếng).",
                                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 3. Lưu dữ liệu vào database
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();

                    if (trangThaiDiemDanh == "Check-in")
                    {
                        // Kiểm tra đã check-in chưa check-out trong ngày này
                        string sqlCheck = @"SELECT MaDD FROM DiemDanh 
                                             WHERE MaHV = @maHV AND date(NgayDD) = date(@ngayDD)
                                             AND (ThoiGianRa IS NULL OR ThoiGianRa = '') 
                                             ORDER BY ThoiGianVao DESC LIMIT 1";
                        using (SqliteCommand cmdCheck = new SqliteCommand(sqlCheck, conn))
                        {
                            cmdCheck.Parameters.AddWithValue("@maHV", maHV);
                            cmdCheck.Parameters.AddWithValue("@ngayDD", ngayDD_DB);
                            if (cmdCheck.ExecuteScalar() != null)
                            {
                                MessageBox.Show($"Học viên {maHV} đã Check-in và chưa Check-out trong ngày này.",
                                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                        }

                        // Thực hiện Check-in mới
                        string sqlInsert = @"
                            INSERT INTO DiemDanh 
                            (MaHV, NgayDD, ThoiGianVao, TenBuoiTap, MaChiNhanh) 
                            VALUES (@maHV, @ngayDD, @tgVao, @tenBuoiTap, @maChiNhanh)";
                        using (SqliteCommand cmd = new SqliteCommand(sqlInsert, conn))
                        {
                            cmd.Parameters.AddWithValue("@maHV", maHV);
                            cmd.Parameters.AddWithValue("@ngayDD", ngayDD_DB);
                            cmd.Parameters.AddWithValue("@tgVao", thoiGianVao);
                            cmd.Parameters.AddWithValue("@tenBuoiTap", tenBuoiTap);
                            cmd.Parameters.AddWithValue("@maChiNhanh", maChiNhanh);
                            cmd.ExecuteNonQuery();
                        }

                        // Cập nhật trạng thái LichTap (YÊU CẦU 3)
                        CapNhatTrangThaiLichTap(maHV, tenBuoiTap, "Đang tập");

                    }
                    else if (trangThaiDiemDanh == "Check-out")
                    {
                        // Cập nhật Check-out cho lần Check-in gần nhất chưa Check-out
                        string sqlUpdate = @"UPDATE DiemDanh 
                                             SET ThoiGianRa = @tgRa 
                                             WHERE MaHV = @maHV AND date(NgayDD) = date(@ngayDD) 
                                             AND (ThoiGianRa IS NULL OR ThoiGianRa = '')
                                             AND TenBuoiTap = @tenBuoiTap
                                             ORDER BY ThoiGianVao DESC LIMIT 1";
                        using (SqliteCommand cmd = new SqliteCommand(sqlUpdate, conn))
                        {
                            cmd.Parameters.AddWithValue("@tgRa", thoiGianRa);
                            cmd.Parameters.AddWithValue("@maHV", maHV);
                            cmd.Parameters.AddWithValue("@ngayDD", ngayDD_DB);
                            cmd.Parameters.AddWithValue("@tenBuoiTap", tenBuoiTap); // Thêm điều kiện Tên buổi tập
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected == 0)
                            {
                                MessageBox.Show($"Không tìm thấy lần Check-in chưa hoàn thành nào của học viên {maHV} (Buổi tập: {tenBuoiTap}) trong ngày này.",
                                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }

                        // Cập nhật trạng thái LichTap (YÊU CẦU 3)
                        CapNhatTrangThaiLichTap(maHV, tenBuoiTap, "Đã hoàn thành");
                    }
                }

                MessageBox.Show($"{trangThaiDiemDanh} thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reset form
                txtMaHocVien.Clear();
                txtTenBuoiTap.Clear();
                txtMaChiNhanh.Clear();
                cmbTrangThai.SelectedIndex = 0;

                // Đặt lại thời gian mặc định hiện tại
                DatThoiGianMacDinh();
                dpNgayDiemDanh.SelectedDate = DateTime.Now;

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
        private void TxtTimKiem_NhanFocus(object sender, RoutedEventArgs e)
        {
            if (txtTimKiem.Text == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                txtTimKiem.Text = "";
                txtTimKiem.Foreground = Brushes.Black;
            }
        }

        private void TxtTimKiem_MatFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTimKiem.Text))
            {
                txtTimKiem.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                txtTimKiem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
        }

        private void TxtTimKiem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ThucHienTimKiem();
            }
        }

        private void BtnTimKiem_Click(object sender, RoutedEventArgs e)
        {
            ThucHienTimKiem();
        }

        private void ThucHienTimKiem()
        {
            if (txtTimKiem == null || lvLichSuDiemDanh == null) return;

            string tuKhoa = txtTimKiem.Text.Trim();

            if (string.IsNullOrEmpty(tuKhoa) || tuKhoa == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                lvLichSuDiemDanh.ItemsSource = tatCaDiemDanh;
                return;
            }

            string tuKhoaKhongDau = BoQuyenDau(tuKhoa).ToLower();
            var ketQuaTimKiem = tatCaDiemDanh.Where(d =>
                BoQuyenDau(d.HoTen).ToLower().Contains(tuKhoaKhongDau) ||
                d.MaHocVien.ToLower().Contains(tuKhoa.ToLower())
            ).ToList();

            lvLichSuDiemDanh.ItemsSource = ketQuaTimKiem;

            if (ketQuaTimKiem.Count == 0)
            {
                MessageBox.Show($"Không tìm thấy học viên nào với từ khóa: {tuKhoa}",
                    "Kết quả tìm kiếm", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

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

        private void BtnMoBoLoc_Click(object sender, RoutedEventArgs e)
        {
            popupBoLoc.Visibility = Visibility.Visible;
        }

        private void BtnDongPopup_Click(object sender, RoutedEventArgs e)
        {
            popupBoLoc.Visibility = Visibility.Collapsed;
        }

        private void BtnXoaBoLoc_Click(object sender, RoutedEventArgs e)
        {
            txtLocHoTen.Clear();
            txtLocMaHV.Clear();
            dpLocTuNgay.SelectedDate = null;
            dpLocDenNgay.SelectedDate = null;
            cmbLocTrangThai.SelectedIndex = 0;

            lvLichSuDiemDanh.ItemsSource = tatCaDiemDanh;

            MessageBox.Show("Đã xóa bộ lọc!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnApDungBoLoc_Click(object sender, RoutedEventArgs e)
        {
            var ketQuaLoc = tatCaDiemDanh.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(txtLocHoTen.Text))
            {
                string hoTenLoc = txtLocHoTen.Text.Trim();
                ketQuaLoc = ketQuaLoc.Where(d =>
                    BoQuyenDau(d.HoTen).ToLower().Contains(BoQuyenDau(hoTenLoc).ToLower())
                );
            }

            if (!string.IsNullOrWhiteSpace(txtLocMaHV.Text))
            {
                string maHVLoc = txtLocMaHV.Text.Trim().ToLower();
                ketQuaLoc = ketQuaLoc.Where(d =>
                    d.MaHocVien.ToLower().Contains(maHVLoc)
                );
            }

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

            string trangThaiLoc = (cmbLocTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (trangThaiLoc != "Tất cả" && !string.IsNullOrEmpty(trangThaiLoc))
            {
                ketQuaLoc = ketQuaLoc.Where(d =>
                    d.TrangThai.Equals(trangThaiLoc, StringComparison.OrdinalIgnoreCase)
                );
            }

            var danhSachLoc = ketQuaLoc.ToList();
            lvLichSuDiemDanh.ItemsSource = danhSachLoc;

            popupBoLoc.Visibility = Visibility.Collapsed;

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