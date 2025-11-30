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
        private List<string> danhSachChiNhanh = new List<string>();

        // Theo dõi Mã điểm danh đang được chỉnh sửa. Null nếu là Thêm mới.
        private string _maDiemDanhDangChinhSua = null;

        // Khai báo hằng số cho placeholder
        private const string VAN_BAN_TIM_KIEM_MAC_DINH = "Tìm kiếm học viên...";
        private const string VAN_BAN_CHON_GIO = "Giờ";
        private const string VAN_BAN_CHON_PHUT = "Phút";
        private const string VAN_BAN_CN_DEFAULT = "Chọn chi nhánh";

        // Lưu trữ Mã CN gốc của lịch tập (cho chức năng chỉnh sửa)
        private string _maCNDangChinhSua = null;

        // Biến private để lưu trữ tạm thời dữ liệu tự động điền (Họ tên, Mã CN, Tên buổi tập)
        private string _hoTenThemMoi = null;
        private string _maCNThemMoi = null;
        private string _tenBuoiTapThemMoi = null;

        // Biến tạm thời cho form Bộ lọc
        private string _hoTenLoc = null;
        private string _maCNLoc = null;


        #region Logic Helper Functions

        // Truy vấn Họ tên từ Mã HV
        private string LayHoTenHocVien(string maHV)
        {
            if (string.IsNullOrWhiteSpace(maHV)) return null;

            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string sql = @"SELECT HoTen FROM HocVien WHERE MaHV = @maHV";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@maHV", maHV);
                        var hoTen = cmd.ExecuteScalar();
                        return hoTen?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi truy vấn Họ tên: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // Tạo Mã Điểm Danh (MaDD) theo format DDXXXXX (tăng tự động).
        private string TaoMaDiemDanhMoi()
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string sql = @"SELECT MAX(MaDD) FROM DiemDanh WHERE MaDD LIKE 'DD%'";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        var maxMaDD = cmd.ExecuteScalar()?.ToString();
                        int nextNumber = 1;

                        if (!string.IsNullOrEmpty(maxMaDD) && maxMaDD.Length > 2 && maxMaDD.StartsWith("DD"))
                        {
                            string numPart = maxMaDD.Substring(2);
                            if (int.TryParse(numPart, out int currentMax))
                            {
                                nextNumber = currentMax + 1;
                            }
                        }
                        return "DD" + nextNumber.ToString("D5");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo Mã Điểm danh: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                return "DD00001";
            }
        }

        private (string MaCN, string TenBuoiTap) LayMaCNVaTenBuoiTap(string maHV)
        {
            (string MaCN, string TenBuoiTap) ketQua = (null, null);

            if (string.IsNullOrWhiteSpace(maHV)) return ketQua;

            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string ngayHienTai = DateTime.Today.ToString("yyyy-MM-dd");

                    // 1. Ưu tiên tra cứu thông tin từ LichTap (để lấy Buổi tập và Chi nhánh)
                    string sqlLichTap = @"
                        SELECT MaCN, TenBuoiTap
                        FROM LichTap
                        WHERE MaHV = @maHV
                          AND date(ThoiGianBatDau) = date(@ngayHienTai)
                        ORDER BY ThoiGianBatDau ASC
                        LIMIT 1";

                    using (SqliteCommand cmd = new SqliteCommand(sqlLichTap, conn))
                    {
                        cmd.Parameters.AddWithValue("@maHV", maHV);
                        cmd.Parameters.AddWithValue("@ngayHienTai", ngayHienTai);

                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ketQua.MaCN = reader["MaCN"]?.ToString() ?? null;
                                ketQua.TenBuoiTap = reader["TenBuoiTap"]?.ToString() ?? null;
                            }
                        }
                    }

                    // 2. Nếu MaCN vẫn chưa có, lấy MaCN từ HopDong gần nhất
                    if (string.IsNullOrWhiteSpace(ketQua.MaCN))
                    {
                        string sqlHopDong = @"
                            SELECT MaCN
                            FROM HopDong
                            WHERE MaHV = @maHV
                            ORDER BY NgayHetHan DESC
                            LIMIT 1";

                        using (SqliteCommand cmdHopDong = new SqliteCommand(sqlHopDong, conn))
                        {
                            cmdHopDong.Parameters.AddWithValue("@maHV", maHV);
                            var maCNFromHopDong = cmdHopDong.ExecuteScalar();
                            if (maCNFromHopDong != null && maCNFromHopDong != DBNull.Value)
                            {
                                ketQua.MaCN = maCNFromHopDong.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tra cứu CSDL: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return ketQua;
        }

        // Kiểm tra xem hợp đồng còn hiệu lực
        private bool KiemTraHopDongHopLe(string maHV, DateTime ngayDiemDanh)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string sql = @"
                        SELECT NgayHetHan
                        FROM HopDong
                        WHERE MaHV = @maHV
                        ORDER BY NgayHetHan DESC
                        LIMIT 1";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@maHV", maHV);
                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            if (DateTime.TryParse(result.ToString(), out DateTime ngayHetHan))
                            {
                                return ngayDiemDanh.Date <= ngayHetHan.Date;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kiểm tra hợp đồng: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return false;
        }

        // Tính toán trạng thái lịch tập dựa trên thời gian hiện tại
        private string TinhTrangThaiLichTap(DateTime thoiGianBatDau, DateTime thoiGianKetThuc)
        {
            DateTime now = DateTime.Now;

            if (now < thoiGianBatDau) return "Chưa bắt đầu";
            if (now >= thoiGianBatDau && now < thoiGianKetThuc) return "Đang tập";
            if (now >= thoiGianKetThuc) return "Đã hoàn thành";

            return "Không xác định";
        }

        // Cập nhật trạng thái của một lịch tập trong CSDL (dùng cho Check-in/Check-out hôm nay)
        private void CapNhatTrangThaiLichTap(string maHV, string tenBuoiTap, string trangThaiMoi)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
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

        // Overload: Cập nhật trạng thái của một lịch tập trong CSDL dựa trên ngày (dùng cho Chỉnh sửa)
        private void CapNhatTrangThaiLichTap(string maHV, string tenBuoiTap, string ngayDD_DB, string trangThaiMoi)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string sql = @"UPDATE LichTap 
                                     SET TrangThai = @trangThaiMoi 
                                     WHERE MaHV = @maHV AND TenBuoiTap = @tenBuoiTap 
                                     AND date(ThoiGianBatDau) = date(@ngayDD_DB)";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@trangThaiMoi", trangThaiMoi);
                        cmd.Parameters.AddWithValue("@maHV", maHV);
                        cmd.Parameters.AddWithValue("@tenBuoiTap", tenBuoiTap);
                        cmd.Parameters.AddWithValue("@ngayDD_DB", ngayDD_DB);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật trạng thái Lịch tập (chỉnh sửa): {ex.Message}", "Lỗi Cập nhật", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Kiểm tra lịch tập hợp lệ (sai lệch tối đa 1 tiếng)
        private bool KiemTraLichTapHopLe(string maHV, string tenBuoiTap, string MaCN, DateTime thoiGianDiemDanh, string trangThaiDiemDanh, out DateTime thoiGianBatDau, out DateTime thoiGianKetThuc)
        {
            thoiGianBatDau = DateTime.MinValue;
            thoiGianKetThuc = DateTime.MaxValue;

            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string ngayDiemDanh = thoiGianDiemDanh.ToString("yyyy-MM-dd");

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
                        cmd.Parameters.AddWithValue("@maCN", MaCN);
                        cmd.Parameters.AddWithValue("@ngayDiemDanh", ngayDiemDanh);

                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                thoiGianBatDau = DateTime.Parse(reader["ThoiGianBatDau"].ToString());
                                thoiGianKetThuc = DateTime.Parse(reader["ThoiGianKetThuc"].ToString());

                                if (trangThaiDiemDanh == "Check-in")
                                {
                                    DateTime checkinMin = thoiGianBatDau.AddHours(-1);
                                    DateTime checkinMax = thoiGianBatDau.AddHours(1);

                                    if (thoiGianDiemDanh >= checkinMin && thoiGianDiemDanh <= checkinMax)
                                    {
                                        return true;
                                    }
                                }
                                else if (trangThaiDiemDanh == "Check-out")
                                {
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

        // Tra cứu MaHV, NgayDD, MaCN, TenBT dựa trên MaDD
        private (string MaHV, string NgayDD, string MaCN, string TenBT) LayChiTietDiemDanh(string maDD)
        {
            (string MaHV, string NgayDD, string MaCN, string TenBT) ketQua = (null, null, null, null);

            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();

                    string sql = @"
                SELECT 
                    d.MaHV, 
                    d.NgayDD, 
                    lt.MaCN, 
                    lt.TenBuoiTap 
                FROM DiemDanh d
                INNER JOIN LichTap lt ON 
                    d.MaHV = lt.MaHV AND 
                    date(d.NgayDD) = date(lt.ThoiGianBatDau)
                WHERE d.MaDD = @maDD
                LIMIT 1";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@maDD", maDD);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ketQua.MaHV = reader["MaHV"]?.ToString();
                                ketQua.NgayDD = reader["NgayDD"]?.ToString();
                                ketQua.MaCN = reader["MaCN"]?.ToString();
                                ketQua.TenBT = reader["TenBuoiTap"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tra cứu chi tiết điểm danh: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return ketQua;
        }

        #endregion

        #region Initialization and Data Loading

        // Tải danh sách tất cả các Mã Chi Nhánh.
        private void TaiDanhSachChiNhanh()
        {
            danhSachChiNhanh.Clear();
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string sql = "SELECT DISTINCT MaCN FROM ChiNhanh ORDER BY MaCN ASC";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            danhSachChiNhanh.Add(reader["MaCN"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách chi nhánh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

            TaiDanhSachChiNhanh();
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
            CapNhatTrangThaiThoiGian(null);
            CapNhatTrangThaiThoiGianChinhSua(null);
        }

        // Đặt giá trị ComboBox giờ/phút về thời gian hiện tại.
        private void DatThoiGianMacDinh()
        {
            DateTime now = DateTime.Now;
            string currentHour = now.Hour.ToString("00");
            string currentMinute = now.Minute.ToString("00");

            // Cần bọc logic này trong kiểm tra để tránh lỗi khi ItemsSource chưa được gán (mặc dù đã gọi KhoiTaoComboBoxThoiGian trước đó)
            if (cmbGioVao != null && cmbGioVao.Items.Count > 0) cmbGioVao.SelectedValue = currentHour;
            if (cmbPhutVao != null && cmbPhutVao.Items.Count > 0) cmbPhutVao.SelectedValue = currentMinute;
            if (cmbGioRa != null && cmbGioRa.Items.Count > 0) cmbGioRa.SelectedValue = currentHour;
            if (cmbPhutRa != null && cmbPhutRa.Items.Count > 0) cmbPhutRa.SelectedValue = currentMinute;

            if (cmbGioVao_ChinhSua != null && cmbGioVao_ChinhSua.Items.Count > 0) cmbGioVao_ChinhSua.SelectedValue = currentHour;
            if (cmbPhutVao_ChinhSua != null && cmbPhutVao_ChinhSua.Items.Count > 0) cmbPhutVao_ChinhSua.SelectedValue = currentMinute;
            if (cmbGioRa_ChinhSua != null && cmbGioRa_ChinhSua.Items.Count > 0) cmbGioRa_ChinhSua.SelectedValue = currentHour;
            if (cmbPhutRa_ChinhSua != null && cmbPhutRa_ChinhSua.Items.Count > 0) cmbPhutRa_ChinhSua.SelectedValue = currentMinute;
        }

        private void KhoiTaoComboBoxThoiGian()
        {
            var danhSachGio = Enumerable.Range(0, 24).Select(h => h.ToString("00")).ToList();
            danhSachGio.Insert(0, VAN_BAN_CHON_GIO);

            var danhSachPhut = Enumerable.Range(0, 60).Select(m => m.ToString("00")).ToList();
            danhSachPhut.Insert(0, VAN_BAN_CHON_PHUT);

            // Cho Form Thêm mới
            if (cmbGioVao != null) cmbGioVao.ItemsSource = danhSachGio;
            if (cmbGioRa != null) cmbGioRa.ItemsSource = danhSachGio;
            if (cmbPhutVao != null) cmbPhutVao.ItemsSource = danhSachPhut;
            if (cmbPhutRa != null) cmbPhutRa.ItemsSource = danhSachPhut;

            // Đặt SelectedIndex cần kiểm tra ItemsSource đã được gán chưa, 
            // nhưng vì nó vừa được gán ở trên nên ta có thể kiểm tra items count
            if (cmbGioVao != null && cmbGioVao.Items.Count > 0) cmbGioVao.SelectedIndex = 0;
            if (cmbGioRa != null && cmbGioRa.Items.Count > 0) cmbGioRa.SelectedIndex = 0;
            if (cmbPhutVao != null && cmbPhutVao.Items.Count > 0) cmbPhutVao.SelectedIndex = 0;
            if (cmbPhutRa != null && cmbPhutRa.Items.Count > 0) cmbPhutRa.SelectedIndex = 0;

            // Cho Popup Chỉnh sửa
            if (cmbGioVao_ChinhSua != null) cmbGioVao_ChinhSua.ItemsSource = danhSachGio;
            if (cmbGioRa_ChinhSua != null) cmbGioRa_ChinhSua.ItemsSource = danhSachGio;
            if (cmbPhutVao_ChinhSua != null) cmbPhutVao_ChinhSua.ItemsSource = danhSachPhut;
            if (cmbPhutRa_ChinhSua != null) cmbPhutRa_ChinhSua.ItemsSource = danhSachPhut;

            if (cmbGioVao_ChinhSua != null && cmbGioVao_ChinhSua.Items.Count > 0) cmbGioVao_ChinhSua.SelectedIndex = 0;
            if (cmbGioRa_ChinhSua != null && cmbGioRa_ChinhSua.Items.Count > 0) cmbGioRa_ChinhSua.SelectedIndex = 0;
            if (cmbPhutVao_ChinhSua != null && cmbPhutVao_ChinhSua.Items.Count > 0) cmbPhutVao_ChinhSua.SelectedIndex = 0;
            if (cmbPhutRa_ChinhSua != null && cmbPhutRa_ChinhSua.Items.Count > 0) cmbPhutRa_ChinhSua.SelectedIndex = 0;
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
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string homNay = DateTime.Today.ToString("yyyy-MM-dd");

                    // 1. Số điểm danh hôm nay (Sử dụng CSDL)
                    string sql1 = "SELECT COUNT(DISTINCT MaHV) FROM DiemDanh WHERE date(NgayDD) = date(@homNay)";
                    using (SqliteCommand cmd = new SqliteCommand(sql1, conn))
                    {
                        cmd.Parameters.AddWithValue("@homNay", homNay);
                        txtSoDiemDanhHomNay.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // 2. Đang tập luyện (Sử dụng CSDL)
                    string sql2 = @"SELECT COUNT(DISTINCT MaHV) FROM DiemDanh 
                                     WHERE date(NgayDD) = date(@homNay) 
                                     AND ThoiGianVao IS NOT NULL 
                                     AND (ThoiGianRa IS NULL OR ThoiGianRa = '')";
                    using (SqliteCommand cmd = new SqliteCommand(sql2, conn))
                    {
                        cmd.Parameters.AddWithValue("@homNay", homNay);
                        txtDangTapLuyen.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // 3. Đã hoàn thành (Sử dụng CSDL)
                    // Lưu ý: Đã hoàn thành nên dựa trên DiemDanh có ThoiGianRa IS NOT NULL (hoặc LichTap.TrangThai = 'Đã hoàn thành')
                    // Giữ logic cũ dựa trên LichTap.TrangThai:
                    string sql3 = "SELECT COUNT(*) FROM LichTap WHERE TrangThai = 'Đã Hoàn Thành'";
                    using (SqliteCommand cmd = new SqliteCommand(sql3, conn))
                    {
                        txtDaHoanThanh.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // 4. Thời gian trung bình (Sử dụng CSDL)
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
                                TrangThai = reader["LichTapTrangThai"]?.ToString() ?? ""
                            };

                            // Logic xác định trạng thái hiển thị
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
                                else
                                {
                                    item.TrangThai = "Chưa hoàn thành";
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

        // Xử lý cập nhật thông tin điểm danh
        private bool CapNhatDiemDanh(string maDD, string ngayDD_DB, string tgVao, string tgRa)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string sqlUpdate = @"
                        UPDATE DiemDanh SET 
                            NgayDD = @ngayDD, 
                            ThoiGianVao = @tgVao, 
                            ThoiGianRa = @tgRa
                        WHERE MaDD = @maDD";

                    using (SqliteCommand cmd = new SqliteCommand(sqlUpdate, conn))
                    {
                        cmd.Parameters.AddWithValue("@ngayDD", ngayDD_DB);
                        cmd.Parameters.AddWithValue("@tgVao", tgVao ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@tgRa", tgRa ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@maDD", maDD);

                        if (cmd.ExecuteNonQuery() > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật Điểm danh {maDD}: {ex.Message}", "Lỗi Cập nhật", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            DateTime thoiGianDiemDanh = DateTime.Now;
            string tgDiemDanhGioPhut = thoiGianDiemDanh.ToString("HH:mm");

            DatThoiGianMacDinh();

            try
            {
                // 1. Thu thập và kiểm tra dữ liệu nhập cơ bản
                string maHV = txtMaHocVien.Text.Trim();

                string hoTen = _hoTenThemMoi;
                string MaCN = _maCNThemMoi;
                string tenBuoiTap = _tenBuoiTapThemMoi;

                if (!dpNgayDiemDanh.SelectedDate.HasValue)
                {
                    MessageBox.Show("Vui lòng chọn ngày điểm danh!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                DateTime ngayDiemDanhDate = dpNgayDiemDanh.SelectedDate.Value.Date;
                string ngayDD_DB = ngayDiemDanhDate.ToString("yyyy-MM-dd");

                string trangThaiDiemDanh = (cmbTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString();

                // KIỂM TRA LỖI NHẬP LIỆU BẮT BUỘC
                if (string.IsNullOrWhiteSpace(maHV))
                {
                    MessageBox.Show("Vui lòng nhập Mã học viên!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(hoTen) || txtHoTen.Text == "Không tìm thấy học viên")
                {
                    MessageBox.Show("Mã học viên không hợp lệ hoặc không tìm thấy Họ tên học viên. Vui lòng kiểm tra lại.", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Loại bỏ MessageBox cảnh báo MaCN NULL/rỗng 
                if (string.IsNullOrWhiteSpace(MaCN))
                {
                    if (!KiemTraHopDongHopLe(maHV, ngayDiemDanhDate))
                    {
                        MessageBox.Show("Học viên không có Hợp đồng hợp lệ và Mã chi nhánh không xác định. Không thể tiếp tục.",
                                        "Lỗi Hợp đồng/Chi nhánh", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                if (trangThaiDiemDanh == "Chọn trạng thái" || trangThaiDiemDanh == null)
                {
                    MessageBox.Show("Vui lòng chọn trạng thái!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(tenBuoiTap))
                {
                    // Kiểm tra Hợp đồng nếu không có Lịch tập
                    if (!KiemTraHopDongHopLe(maHV, ngayDiemDanhDate))
                    {
                        MessageBox.Show("Lưu không thành công! Học viên không có Lịch tập hôm nay và Hợp đồng đã hết hạn hoặc không tồn tại.",
                                         "Lỗi Hợp đồng", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    // Kiểm tra lịch tập cụ thể (theo giờ/phút)
                    DateTime thoiGianBatDauLich;
                    DateTime thoiGianKetThucLich;

                    if (!KiemTraLichTapHopLe(maHV, tenBuoiTap, MaCN, thoiGianDiemDanh, trangThaiDiemDanh, out thoiGianBatDauLich, out thoiGianKetThucLich))
                    {
                        MessageBox.Show("Lưu không thành công! Học viên không có lịch tập phù hợp tại thời điểm này (sai lệch tối đa 1 tiếng).",
                                         "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // TẠO MÃ DD MỚI
                string maDDMoi = TaoMaDiemDanhMoi();

                // Chuẩn bị thời gian vào/ra
                string thoiGianVao = (trangThaiDiemDanh == "Check-in") ? tgDiemDanhGioPhut : null;
                string thoiGianRa = (trangThaiDiemDanh == "Check-out") ? tgDiemDanhGioPhut : null;

                // 3. Lưu dữ liệu vào database
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();

                    if (trangThaiDiemDanh == "Check-in")
                    {
                        // Kiểm tra trùng lặp Check-in
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
                                MessageBox.Show($"Học viên {maHV} đã Check-in và chưa Check-out.",
                                                     "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                        }

                        // Thực hiện INSERT
                        string sqlInsert = @"
                            INSERT INTO DiemDanh 
                            (MaDD, MaHV, NgayDD, ThoiGianVao, TenBuoiTap, MaCN) 
                            VALUES (@maDD, @maHV, @ngayDD, @tgVao, @tenBuoiTap, @MaCN)";
                        using (SqliteCommand cmd = new SqliteCommand(sqlInsert, conn))
                        {
                            cmd.Parameters.AddWithValue("@maDD", maDDMoi);
                            cmd.Parameters.AddWithValue("@maHV", maHV);
                            cmd.Parameters.AddWithValue("@ngayDD", ngayDD_DB);
                            cmd.Parameters.AddWithValue("@tgVao", thoiGianVao);
                            cmd.Parameters.AddWithValue("@tenBuoiTap", tenBuoiTap ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MaCN", MaCN ?? (object)DBNull.Value); // Có thể là NULL
                            cmd.ExecuteNonQuery();
                        }

                        // Cập nhật trạng thái LichTap
                        if (!string.IsNullOrWhiteSpace(tenBuoiTap))
                            CapNhatTrangThaiLichTap(maHV, tenBuoiTap, "Đang tập");

                    }
                    else if (trangThaiDiemDanh == "Check-out")
                    {
                        // Tìm bản ghi Check-in chưa Check-out
                        string sqlCheckin = @"SELECT MaDD FROM DiemDanh 
                                             WHERE MaHV = @maHV AND date(NgayDD) = date(@ngayDD) 
                                             AND (ThoiGianRa IS NULL OR ThoiGianRa = '')
                                             ORDER BY ThoiGianVao DESC LIMIT 1";
                        string maDDCanUpdate = null;
                        using (SqliteCommand cmdCheckin = new SqliteCommand(sqlCheckin, conn))
                        {
                            cmdCheckin.Parameters.AddWithValue("@maHV", maHV);
                            cmdCheckin.Parameters.AddWithValue("@ngayDD", ngayDD_DB);
                            maDDCanUpdate = cmdCheckin.ExecuteScalar()?.ToString();
                        }

                        // Xử lý không tìm thấy
                        if (string.IsNullOrWhiteSpace(maDDCanUpdate))
                        {
                            MessageBox.Show($"Không tìm thấy lần Check-in chưa hoàn thành nào của học viên {maHV} trong ngày này.",
                                                 "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Thực hiện UPDATE
                        string sqlUpdate = @"UPDATE DiemDanh 
                                             SET ThoiGianRa = @tgRa 
                                             WHERE MaDD = @maDDCanUpdate";
                        using (SqliteCommand cmd = new SqliteCommand(sqlUpdate, conn))
                        {
                            cmd.Parameters.AddWithValue("@tgRa", thoiGianRa);
                            cmd.Parameters.AddWithValue("@maDDCanUpdate", maDDCanUpdate);
                            cmd.ExecuteNonQuery();
                        }

                        // Cập nhật trạng thái LichTap
                        if (!string.IsNullOrWhiteSpace(tenBuoiTap))
                            CapNhatTrangThaiLichTap(maHV, tenBuoiTap, "Đã hoàn thành");
                    }
                }

                MessageBox.Show($"{trangThaiDiemDanh} thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reset form và biến private
                txtMaHocVien.Clear();
                txtHoTen.Text = "";
                txtMaCN_ThemMoi.Text = "";
                txtTenBuoiTap.Clear();
                cmbTrangThai.SelectedIndex = 0;
                _hoTenThemMoi = null;
                _maCNThemMoi = null;
                _tenBuoiTapThemMoi = null;

                DatThoiGianMacDinh();
                dpNgayDiemDanh.SelectedDate = DateTime.Now;

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

        #region UI Control & Event Handlers

        // Đổ dữ liệu của hàng được chọn lên popup để chỉnh sửa.
        private void BtnChinhSua_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var diemDanhItem = button?.Tag as MoDonDuLieuDiemDanh;

            if (diemDanhItem != null)
            {
                HienThiChiTietDiemDanh(diemDanhItem);
            }
        }

        private void HienThiChiTietDiemDanh(MoDonDuLieuDiemDanh item)
        {
            _maDiemDanhDangChinhSua = item.MaDiemDanh;

            var chiTietDD = LayChiTietDiemDanh(item.MaDiemDanh);
            _maCNDangChinhSua = chiTietDD.MaCN;

            // 1. Fill thông tin cơ bản
            txtMaHV_ChinhSua.Text = item.MaHocVien;


            // 2. Fill Ngày điểm danh
            if (DateTime.TryParseExact(item.NgayDiemDanh, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ngayDD))
            {
                dpNgayDD_ChinhSua.SelectedDate = ngayDD;
            }
            else
            {
                dpNgayDD_ChinhSua.SelectedDate = null;
            }

            // 3. Set Trạng thái và điều chỉnh ComboBox Giờ/Phút
            string trangThaiText = "";

            var selectedItem = cmbTrangThai_ChinhSua.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == item.TrangThai);
            if (selectedItem != null)
            {
                cmbTrangThai_ChinhSua.SelectedItem = selectedItem;
                trangThaiText = item.TrangThai;
            }
            else
            {
                cmbTrangThai_ChinhSua.SelectedIndex = 0;
            }

            CapNhatTrangThaiThoiGianChinhSua(trangThaiText);

            // Set Giờ Vào/Ra
            if (item.ThoiGianVao != "--:--")
            {
                if (DateTime.TryParseExact(item.ThoiGianVao, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tgVao))
                {
                    cmbGioVao_ChinhSua.SelectedValue = tgVao.Hour.ToString("00");
                    cmbPhutVao_ChinhSua.SelectedValue = tgVao.Minute.ToString("00");
                }
            }
            else
            {
                cmbGioVao_ChinhSua.SelectedIndex = 0;
                cmbPhutVao_ChinhSua.SelectedIndex = 0;
            }

            if (item.ThoiGianRa != "--:--")
            {
                if (DateTime.TryParseExact(item.ThoiGianRa, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tgRa))
                {
                    cmbGioRa_ChinhSua.SelectedValue = tgRa.Hour.ToString("00");
                    cmbPhutRa_ChinhSua.SelectedValue = tgRa.Minute.ToString("00");
                }
            }
            else
            {
                cmbGioRa_ChinhSua.SelectedIndex = 0;
                cmbPhutRa_ChinhSua.SelectedIndex = 0;
            }

            popupChinhSua.Visibility = Visibility.Visible;
        }

        // Xử lý đóng Popup chỉnh sửa
        private void BtnDongPopupChinhSua_Click(object sender, RoutedEventArgs e)
        {
            popupChinhSua.Visibility = Visibility.Collapsed;
            _maDiemDanhDangChinhSua = null;
            _maCNDangChinhSua = null;
            cmbTrangThai_ChinhSua.SelectedIndex = 0;
        }

        // Đặt trạng thái Enabled cho các ComboBox Giờ/Phút trong popup chỉnh sửa
        private void CapNhatTrangThaiThoiGianChinhSua(string trangThai)
        {
            if (GridThoiGianVao_ChinhSua == null || GridThoiGianRa_ChinhSua == null) return;

            GridThoiGianVao_ChinhSua.IsEnabled = false;
            GridThoiGianRa_ChinhSua.IsEnabled = false;

            if (trangThai == "Đã hủy" || trangThai == "Chọn trạng thái")
            {
                // Disabled cả 2
            }
            else if (trangThai == "Chưa hoàn thành" || trangThai == "Đang tập" || trangThai == "Chưa bắt đầu")
            {
                GridThoiGianVao_ChinhSua.IsEnabled = true;
            }
            else if (trangThai == "Đã hoàn thành")
            {
                GridThoiGianVao_ChinhSua.IsEnabled = true;
                GridThoiGianRa_ChinhSua.IsEnabled = true;
            }
        }


        private void CmbTrangThaiChinhSua_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedContent = (cmbTrangThai_ChinhSua.SelectedItem as ComboBoxItem)?.Content.ToString();
            CapNhatTrangThaiThoiGianChinhSua(selectedContent);
        }

        // Xử lý sự kiện TextChanged của Mã học viên (Form Thêm mới)
        private void TxtMaHocVien_TextChanged(object sender, TextChangedEventArgs e)
        {
            string maHV = txtMaHocVien.Text.Trim();

            // Reset
            txtHoTen.Text = "";
            txtMaCN_ThemMoi.Text = "";
            txtTenBuoiTap.Text = "";
            _hoTenThemMoi = null;
            _maCNThemMoi = null;
            _tenBuoiTapThemMoi = null;

            if (maHV.Length >= 4)
            {
                // Truy vấn Họ tên
                string hoTen = LayHoTenHocVien(maHV);
                if (!string.IsNullOrWhiteSpace(hoTen))
                {
                    txtHoTen.Text = hoTen;
                    _hoTenThemMoi = hoTen;
                    txtHoTen.Foreground = Brushes.Black;
                }

                // Truy vấn Mã CN và Tên Buổi tập
                var info = LayMaCNVaTenBuoiTap(maHV);

                // Cập nhật TextBlock và Lưu giá trị vào biến private
                txtTenBuoiTap.Text = info.TenBuoiTap ?? "Không tìm thấy lịch tập hôm nay";
                _tenBuoiTapThemMoi = info.TenBuoiTap;

                txtMaCN_ThemMoi.Text = info.MaCN ?? "Không tìm thấy CN gần nhất";
                _maCNThemMoi = info.MaCN;

                // Cập nhật màu chữ cho TextBlock Mã CN
                if (string.IsNullOrWhiteSpace(info.MaCN))
                {
                    txtMaCN_ThemMoi.Foreground = Brushes.Red;
                }
                else
                {
                    txtMaCN_ThemMoi.Foreground = Brushes.Black;
                }
            }
        }

        // CHỨC NĂNG MỚI: Xử lý sự kiện TextChanged của Mã học viên (Form Bộ lọc)
        private void TxtLocMaHV_TextChanged(object sender, TextChangedEventArgs e)
        {
            string maHV = txtLocMaHV.Text.Trim();

            // Reset
            if (txtLocHoTen != null) txtLocHoTen.Text = "";
            if (txtLocMaCN != null) txtLocMaCN.Text = "";
            _hoTenLoc = null;
            _maCNLoc = null;

            if (maHV.Length >= 4)
            {
                string hoTen = LayHoTenHocVien(maHV);
                if (!string.IsNullOrWhiteSpace(hoTen))
                {
                    if (txtLocHoTen != null)
                    {
                        txtLocHoTen.Text = hoTen;
                        txtLocHoTen.Foreground = Brushes.Black;
                    }
                    _hoTenLoc = hoTen;
                }
                else
                {
                    if (txtLocHoTen != null)
                    {
                        txtLocHoTen.Text = "Không tìm thấy học viên";
                        txtLocHoTen.Foreground = Brushes.Red;
                    }
                    if (txtLocMaCN != null)
                    {
                        txtLocMaCN.Text = "Không tìm thấy CN";
                        txtLocMaCN.Foreground = Brushes.Red;
                    }
                    return;
                }

                var info = LayMaCNVaTenBuoiTap(maHV);

                if (txtLocMaCN != null)
                {
                    txtLocMaCN.Text = info.MaCN ?? "Không tìm thấy CN";
                    _maCNLoc = info.MaCN;

                    if (string.IsNullOrWhiteSpace(info.MaCN))
                    {
                        txtLocMaCN.Foreground = Brushes.Red;
                    }
                    else
                    {
                        txtLocMaCN.Foreground = Brushes.Black;
                    }
                }
            }
        }


        // Logic để bật/tắt các ComboBox giờ vào/ra
        private void CapNhatTrangThaiThoiGian(string trangThai)
        {
            if (GridThoiGianVao == null || GridThoiGianRa == null) return;

            DateTime now = DateTime.Now;
            string currentHour = now.Hour.ToString("00");
            string currentMinute = now.Minute.ToString("00");

            if (trangThai == "Check-in")
            {
                GridThoiGianVao.IsEnabled = true;
                GridThoiGianRa.IsEnabled = false;

                if (cmbGioVao != null && cmbGioVao.Items.Count > 0) cmbGioVao.SelectedValue = currentHour;
                if (cmbPhutVao != null && cmbPhutVao.Items.Count > 0) cmbPhutVao.SelectedValue = currentMinute;

                if (cmbGioRa != null) cmbGioRa.SelectedIndex = 0;
                if (cmbPhutRa != null) cmbPhutRa.SelectedIndex = 0;
            }
            else if (trangThai == "Check-out")
            {
                GridThoiGianVao.IsEnabled = false;
                GridThoiGianRa.IsEnabled = true;

                if (cmbGioRa != null && cmbGioRa.Items.Count > 0) cmbGioRa.SelectedValue = currentHour;
                if (cmbPhutRa != null && cmbPhutRa.Items.Count > 0) cmbPhutRa.SelectedValue = currentMinute;

                if (cmbGioVao != null) cmbGioVao.SelectedIndex = 0;
                if (cmbPhutVao != null) cmbPhutVao.SelectedIndex = 0;
            }
            else
            {
                GridThoiGianVao.IsEnabled = false;
                GridThoiGianRa.IsEnabled = false;

                if (cmbGioVao != null) cmbGioVao.SelectedIndex = 0;
                if (cmbPhutVao != null) cmbPhutVao.SelectedIndex = 0;
                if (cmbGioRa != null) cmbGioRa.SelectedIndex = 0;
                if (cmbPhutRa != null) cmbPhutRa.SelectedIndex = 0;
            }
        }

        private void CmbTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedContent = (cmbTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString();
            CapNhatTrangThaiThoiGian(selectedContent);
        }

        #endregion

        #region Search Functions
        // Hàm xử lý Focus/LostFocus/KeyDown
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
            // Reset các trường phụ thuộc (HoTen, MaCN) trong popup bộ lọc
            if (txtLocMaHV != null) TxtLocMaHV_TextChanged(txtLocMaHV, null);
            popupBoLoc.Visibility = Visibility.Visible;
        }

        private void BtnDongPopup_Click(object sender, RoutedEventArgs e)
        {
            popupBoLoc.Visibility = Visibility.Collapsed;
        }

        private void BtnXoaBoLoc_Click(object sender, RoutedEventArgs e)
        {
            // Chỉnh lại: Dùng TextBlock thay vì TextBox cho HoTen và MaCN
            if (txtLocHoTen != null) txtLocHoTen.Text = "";
            if (txtLocMaCN != null) txtLocMaCN.Text = "";
            if (txtLocMaHV != null) txtLocMaHV.Clear();

            _hoTenLoc = null;
            _maCNLoc = null;

            if (dpLocTuNgay != null) dpLocTuNgay.SelectedDate = null;
            if (dpLocDenNgay != null) dpLocDenNgay.SelectedDate = null;
            if (cmbLocTrangThai != null) cmbLocTrangThai.SelectedIndex = 0;

            if (lvLichSuDiemDanh != null) lvLichSuDiemDanh.ItemsSource = tatCaDiemDanh;

            MessageBox.Show("Đã xóa bộ lọc!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnApDungBoLoc_Click(object sender, RoutedEventArgs e)
        {
            var ketQuaLoc = tatCaDiemDanh.AsEnumerable();

            // Lọc theo Mã HV
            if (txtLocMaHV != null && !string.IsNullOrWhiteSpace(txtLocMaHV.Text))
            {
                string maHVLoc = txtLocMaHV.Text.Trim().ToLower();
                ketQuaLoc = ketQuaLoc.Where(d =>
                    d.MaHocVien.ToLower().Contains(maHVLoc)
                );
            }

            // Lọc theo Họ tên (sử dụng biến tạm _hoTenLoc đã được điền tự động)
            if (!string.IsNullOrWhiteSpace(_hoTenLoc))
            {
                string hoTenLoc = BoQuyenDau(_hoTenLoc).ToLower();
                ketQuaLoc = ketQuaLoc.Where(d =>
                    BoQuyenDau(d.HoTen).ToLower().Contains(hoTenLoc)
                );
            }

            // Hiện tại không có cột MaCN trong MoDonDuLieuDiemDanh để lọc chi tiết theo MaCN
            /*
            if (!string.IsNullOrWhiteSpace(_maCNLoc))
            {
                string maCNLoc = _maCNLoc.Trim().ToLower();
                // BỎ QUA lọc MaCN vì MoDonDuLieuDiemDanh không chứa trường này
            }
            */


            if (dpLocTuNgay != null && dpLocTuNgay.SelectedDate.HasValue)
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

            if (dpLocDenNgay != null && dpLocDenNgay.SelectedDate.HasValue)
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

            string trangThaiLoc = (cmbLocTrangThai?.SelectedItem as ComboBoxItem)?.Content.ToString();
            // Lọc theo trạng thái đã chọn
            if (trangThaiLoc != "Tất cả" && !string.IsNullOrEmpty(trangThaiLoc))
            {
                string normalizedLoc = trangThaiLoc.Replace(" ", "").ToLower();

                ketQuaLoc = ketQuaLoc.Where(d =>
                {
                    string normalizedDD = d.TrangThai.Replace(" ", "").ToLower();
                    return normalizedDD.Equals(normalizedLoc);
                });
            }

            var danhSachLoc = ketQuaLoc.ToList();
            if (lvLichSuDiemDanh != null) lvLichSuDiemDanh.ItemsSource = danhSachLoc;

            if (popupBoLoc != null) popupBoLoc.Visibility = Visibility.Collapsed;

            MessageBox.Show($"Đã lọc được {danhSachLoc.Count} kết quả!",
                "Kết quả lọc", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        private void lvLichSuDiemDanh_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void BtnLuuChinhSua_Click(object sender, RoutedEventArgs e)
        {

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