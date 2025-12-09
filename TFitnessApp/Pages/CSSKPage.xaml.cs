using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using System.Text;

namespace TFitnessApp
{
    public partial class CSSKPage : UserControl
    {
        #region Thuộc tính
        private string chuoiKetNoi;
        private List<MoDonDuLieuChiSoSucKhoe> tatCaChiSo = new List<MoDonDuLieuChiSoSucKhoe>();
        private List<MoDonDuLieuChiSoSucKhoe> danhSachHienThi = new List<MoDonDuLieuChiSoSucKhoe>();

        // Phân trang
        private int trangHienTai = 1;
        private const int SO_MUC_MOT_TRANG = 10;
        private int tongSoTrang = 1;

        // Hằng số
        private const string VAN_BAN_TIM_KIEM_MAC_DINH = "Tìm kiếm theo mã phiếu, mã HV, chiều cao,...";

        // Trạng thái
        private HashSet<string> cacMucDaChon = new HashSet<string>();
        private bool isThemChiSoMoi = true;
        private string MaPhieuDoDangSua = null;
        #endregion

        #region Khởi tạo
        public CSSKPage()
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

            KhoiTaoGiaoDien();
            TaiDanhSachChiSo();
        }

        private void KhoiTaoGiaoDien()
        {
            if (txbTimKiem != null)
            {
                txbTimKiem.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                txbTimKiem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }

            if (dpNgayDo != null)
            {
                dpNgayDo.SelectedDate = DateTime.Now;
            }
        }
        #endregion

        #region Xử lý Dữ liệu
        private int LayTuoiHocVien(string maHV)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string sql = "SELECT NgaySinh FROM HocVien WHERE MaHV = @MaHV";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaHV", maHV);
                        object result = cmd.ExecuteScalar();

                        if (result != null && DateTime.TryParse(result.ToString(), out DateTime ngaySinh))
                        {
                            DateTime today = DateTime.Today;
                            int age = today.Year - ngaySinh.Year;
                            if (ngaySinh.Date > today.AddYears(-age)) age--;
                            return age;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lấy tuổi học viên: {ex.Message}");
            }
            return 0;
        }

        private bool KiemTraTonTaiHocVien(string maHV)
        {
            if (string.IsNullOrEmpty(maHV)) return false;

            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(MaHV) FROM HocVien WHERE MaHV = @MaHV";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaHV", maHV);
                        long count = (long)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kiểm tra Mã Học viên trong DB: {ex.Message}", "Lỗi DB", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void TaiDanhSachChiSo()
        {
            try
            {
                tatCaChiSo.Clear();

                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            p.MaPhieu, p.MaHV, h.HoTen, p.CanNang, p.ChieuCao, p.Tuoi,
                            p.MucTieuSK, p.TDEE, COALESCE(p.NgayDo, datetime('now')) as NgayDo 
                        FROM PhieuDoChiSo p
                        INNER JOIN HocVien h ON p.MaHV = h.MaHV
                        ORDER BY p.NgayDo DESC";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            double canNang = reader["CanNang"] != DBNull.Value ? Convert.ToDouble(reader["CanNang"]) : 0;
                            double chieuCao = reader["ChieuCao"] != DBNull.Value ? Convert.ToDouble(reader["ChieuCao"]) : 0;
                            DateTime ngayDo = DateTime.TryParse(reader["NgayDo"].ToString(), out DateTime d) ? d : DateTime.Now;

                            var item = new MoDonDuLieuChiSoSucKhoe
                            {
                                MaPhieuDo = reader["MaPhieu"].ToString(),
                                MaHocVien = reader["MaHV"].ToString(),
                                HoTenHocVien = reader["HoTen"].ToString(),
                                CanNang = canNang.ToString("F1"),
                                ChieuCao = chieuCao.ToString("F0"),
                                Tuoi = reader["Tuoi"] != DBNull.Value ? Convert.ToInt32(reader["Tuoi"]) : 0,
                                BMI = TinhBMI(canNang, chieuCao),
                                TDEE = reader["TDEE"] != DBNull.Value ? reader["TDEE"].ToString() : "0",
                                MucTieuSucKhoe = reader["MucTieuSK"]?.ToString() ?? "",
                                NgayDo = ngayDo,
                                DaChon = cacMucDaChon.Contains(reader["MaPhieu"].ToString())
                            };
                            tatCaChiSo.Add(item);
                        }
                    }
                }
                danhSachHienThi = new List<MoDonDuLieuChiSoSucKhoe>(tatCaChiSo);
                CapNhatPhanTrang();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách chỉ số: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string TinhBMI(double canNang, double chieuCao)
        {
            if (chieuCao <= 0) return "0";
            double chieuCaoMet = chieuCao / 100;
            double bmi = canNang / (chieuCaoMet * chieuCaoMet);
            return bmi.ToString("F1");
        }
        #endregion

        #region Phân trang
        private void CapNhatPhanTrang()
        {
            tongSoTrang = (int)Math.Ceiling((double)danhSachHienThi.Count / SO_MUC_MOT_TRANG);
            if (tongSoTrang == 0) tongSoTrang = 1;

            if (trangHienTai > tongSoTrang) trangHienTai = tongSoTrang;
            if (trangHienTai < 1) trangHienTai = 1;

            HienThiTrangHienTai();
            CapNhatbtnPhanTrang();
        }

        private void HienThiTrangHienTai()
        {
            var mucTrangHienTai = danhSachHienThi
                .Skip((trangHienTai - 1) * SO_MUC_MOT_TRANG)
                .Take(SO_MUC_MOT_TRANG)
                .ToList();

            if (lstChiSo != null)
            {
                lstChiSo.ItemsSource = mucTrangHienTai;
            }

            if (txbThongTinTrang != null)
            {
                txbThongTinTrang.Text = $"Trang {trangHienTai}/{tongSoTrang}";
            }
        }

        private void CapNhatbtnPhanTrang()
        {
            if (btnTrangDau != null) btnTrangDau.IsEnabled = trangHienTai > 1;
            if (btnTrangTruoc != null) btnTrangTruoc.IsEnabled = trangHienTai > 1;
            if (btnTrangSau != null) btnTrangSau.IsEnabled = trangHienTai < tongSoTrang;
            if (btnTrangCuoi != null) btnTrangCuoi.IsEnabled = trangHienTai < tongSoTrang;
        }

        private void btnTrangDau_Click(object sender, RoutedEventArgs e)
        {
            trangHienTai = 1;
            CapNhatPhanTrang();
        }

        private void btnTrangTruoc_Click(object sender, RoutedEventArgs e)
        {
            if (trangHienTai > 1)
            {
                trangHienTai--;
                CapNhatPhanTrang();
            }
        }

        private void btnTrangSau_Click(object sender, RoutedEventArgs e)
        {
            if (trangHienTai < tongSoTrang)
            {
                trangHienTai++;
                CapNhatPhanTrang();
            }
        }

        private void btnTrangCuoi_Click(object sender, RoutedEventArgs e)
        {
            trangHienTai = tongSoTrang;
            CapNhatPhanTrang();
        }
        #endregion

        #region Xử lý Tìm kiếm & Lọc
        private void txbTimKiem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txbTimKiem.Text == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                txbTimKiem.Text = "";
                txbTimKiem.Foreground = Brushes.Black;
            }
        }

        private void txbTimKiem_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txbTimKiem.Text))
            {
                txbTimKiem.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                txbTimKiem.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
        }

        private void btnTimKiem_Click(object sender, RoutedEventArgs e)
        {
            if (txbTimKiem == null) return;

            string tuKhoa = txbTimKiem.Text.Trim();

            if (string.IsNullOrEmpty(tuKhoa) || tuKhoa == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                danhSachHienThi = new List<MoDonDuLieuChiSoSucKhoe>(tatCaChiSo);
            }
            else
            {
                string tuKhoaKhongDau = BoQuyenDau(tuKhoa).ToLower();
                string tuKhoaThuong = tuKhoa.ToLower();

                danhSachHienThi = tatCaChiSo.Where(item =>
                    item.MaPhieuDo.ToLower().Contains(tuKhoaThuong) ||
                    item.MaHocVien.ToLower().Contains(tuKhoaThuong) ||
                    BoQuyenDau(item.HoTenHocVien).ToLower().Contains(tuKhoaKhongDau) ||
                    item.ChieuCao.Contains(tuKhoa) ||
                    item.CanNang.Contains(tuKhoa) ||
                    item.BMI.Contains(tuKhoa) ||
                    item.TDEE.Contains(tuKhoa) ||
                    item.Tuoi.ToString().Contains(tuKhoa) ||
                    item.NgayDo.ToString("dd/MM/yyyy").Contains(tuKhoa) ||
                    item.MucTieuSucKhoe.Contains(tuKhoa)
                ).ToList();
            }

            trangHienTai = 1;
            CapNhatPhanTrang();
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

        private void btnLoc_Click(object sender, RoutedEventArgs e)
        {
            if (GrdOverlayLoc != null)
            {
                GrdOverlayLoc.Visibility = Visibility.Visible;
            }
        }

        private void DongLoc_Click(object sender, RoutedEventArgs e)
        {
            if (GrdOverlayLoc != null)
            {
                GrdOverlayLoc.Visibility = Visibility.Collapsed;
            }
        }

        private void HuyLoc_Click(object sender, RoutedEventArgs e)
        {
            txbLocMaPhieu.Text = string.Empty;
            txbLocMaHV.Text = string.Empty;
            txbLocHoTen.Text = string.Empty;
            txbLocChieuCao.Text = string.Empty;
            txbLocCanNang.Text = string.Empty;
            txbLocBMI.Text = string.Empty;
            txbLocTDEE.Text = string.Empty;
            txbLocMucTieu.Text = string.Empty;
            dpLocNgayDo.SelectedDate = null;

            danhSachHienThi = new List<MoDonDuLieuChiSoSucKhoe>(tatCaChiSo);
            trangHienTai = 1;
            CapNhatPhanTrang();

            DongLoc_Click(sender, e);
        }

        private void ApDungLoc_Click(object sender, RoutedEventArgs e)
        {
            string maPhieu = txbLocMaPhieu.Text.Trim().ToLower();
            string maHV = txbLocMaHV.Text.Trim().ToLower();
            string hoTen = txbLocHoTen.Text.Trim().ToLower();
            string chieuCao = txbLocChieuCao.Text.Trim();
            string canNang = txbLocCanNang.Text.Trim();
            string bmi = txbLocBMI.Text.Trim();
            string tdee = txbLocTDEE.Text.Trim();
            string mucTieu = txbLocMucTieu.Text.Trim().ToLower();
            DateTime? ngayDo = dpLocNgayDo.SelectedDate;

            IEnumerable<MoDonDuLieuChiSoSucKhoe> ketQuaLoc = tatCaChiSo;

            if (!string.IsNullOrEmpty(maPhieu))
            {
                ketQuaLoc = ketQuaLoc.Where(item => item.MaPhieuDo.ToLower().Contains(maPhieu));
            }
            if (!string.IsNullOrEmpty(maHV))
            {
                ketQuaLoc = ketQuaLoc.Where(item => item.MaHocVien.ToLower().Contains(maHV));
            }
            if (!string.IsNullOrEmpty(hoTen))
            {
                string hoTenKhongDau = BoQuyenDau(hoTen);
                ketQuaLoc = ketQuaLoc.Where(item => BoQuyenDau(item.HoTenHocVien).ToLower().Contains(hoTenKhongDau));
            }
            if (!string.IsNullOrEmpty(chieuCao))
            {
                ketQuaLoc = ketQuaLoc.Where(item => item.ChieuCao.Contains(chieuCao));
            }
            if (!string.IsNullOrEmpty(canNang))
            {
                ketQuaLoc = ketQuaLoc.Where(item => item.CanNang.Contains(canNang));
            }
            if (!string.IsNullOrEmpty(bmi))
            {
                ketQuaLoc = ketQuaLoc.Where(item => item.BMI.Contains(bmi));
            }
            if (!string.IsNullOrEmpty(tdee))
            {
                ketQuaLoc = ketQuaLoc.Where(item => item.TDEE.Contains(tdee));
            }
            if (!string.IsNullOrEmpty(mucTieu))
            {
                string mucTieuKhongDau = BoQuyenDau(mucTieu);
                ketQuaLoc = ketQuaLoc.Where(item => BoQuyenDau(item.MucTieuSucKhoe).ToLower().Contains(mucTieuKhongDau));
            }
            if (ngayDo.HasValue)
            {
                ketQuaLoc = ketQuaLoc.Where(item => item.NgayDo.Date == ngayDo.Value.Date);
            }

            danhSachHienThi = ketQuaLoc.ToList();
            trangHienTai = 1;
            CapNhatPhanTrang();
            DongLoc_Click(sender, e);
        }
        #endregion

        #region Xử lý Chọn (Selection)
        private void btnChonTatCa_Click(object sender, RoutedEventArgs e)
        {
            if (chkChonTatCa == null) return;
            chkChonTatCa.IsChecked = chkChonTatCa.IsChecked != true;
        }

        private void chkChonTatCa_Changed(object sender, RoutedEventArgs e)
        {
            if (chkChonTatCa == null) return;

            bool isChecked = chkChonTatCa.IsChecked == true;

            foreach (var item in tatCaChiSo)
            {
                item.DaChon = isChecked;

                if (isChecked)
                    cacMucDaChon.Add(item.MaPhieuDo);
                else
                    cacMucDaChon.Remove(item.MaPhieuDo);
            }

            foreach (var item in danhSachHienThi)
            {
                item.DaChon = isChecked;
            }

            if (lstChiSo != null)
            {
                lstChiSo.Items.Refresh();
            }
        }

        private void chkDuLieu_Changed(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox == null) return;

            var item = checkBox.DataContext as MoDonDuLieuChiSoSucKhoe;
            if (item == null) return;

            CapNhatTrangThaiChon(item);
        }

        private void CapNhatTrangThaiChon(MoDonDuLieuChiSoSucKhoe item)
        {
            if (item.DaChon)
            {
                cacMucDaChon.Add(item.MaPhieuDo);
            }
            else
            {
                cacMucDaChon.Remove(item.MaPhieuDo);
            }

            var fullItem = tatCaChiSo.FirstOrDefault(i => i.MaPhieuDo == item.MaPhieuDo);
            if (fullItem != null)
            {
                fullItem.DaChon = item.DaChon;
            }

            if (lstChiSo != null)
            {
                lstChiSo.Items.Refresh();
            }
        }
        #endregion

        #region Popup Thêm/Sửa (CRUD)

        private void ResetCacTruongPopup()
        {
            if (txbMaHV != null) txbMaHV.Text = string.Empty;
            if (txbCanNang != null) txbCanNang.Text = string.Empty;
            if (txbChieuCao != null) txbChieuCao.Text = string.Empty;
            if (txbBMI != null) txbBMI.Text = string.Empty;
            if (txbTDEE != null) txbTDEE.Text = string.Empty;
            if (cboMucTieuSK != null) cboMucTieuSK.SelectedIndex = -1;
            if (dpNgayDo != null) dpNgayDo.SelectedDate = DateTime.Now;
        }

        private void HienThiPopupThem()
        {
            if (GrdOverlayPopup == null || txbTieuDePopup == null) return;

            ResetCacTruongPopup();

            txbTieuDePopup.Text = "Thêm chỉ số sức khỏe";
            isThemChiSoMoi = true;
            MaPhieuDoDangSua = null;

            if (txbMaHV != null)
            {
                txbMaHV.IsReadOnly = false;
                txbMaHV.Background = Brushes.White;
            }
            GrdOverlayPopup.Visibility = Visibility.Visible;
        }

        private void HienThiPopupSua(MoDonDuLieuChiSoSucKhoe chiSo)
        {
            if (GrdOverlayPopup == null || txbTieuDePopup == null || chiSo == null) return;
            ResetCacTruongPopup();

            txbTieuDePopup.Text = $"Sửa chỉ số: {chiSo.MaPhieuDo}";
            isThemChiSoMoi = false;
            MaPhieuDoDangSua = chiSo.MaPhieuDo;

            if (txbMaHV != null)
            {
                txbMaHV.Text = chiSo.MaHocVien;
                txbMaHV.IsReadOnly = true;
                txbMaHV.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            }

            if (txbCanNang != null) txbCanNang.Text = chiSo.CanNang;
            if (txbChieuCao != null) txbChieuCao.Text = chiSo.ChieuCao;
            if (txbBMI != null) txbBMI.Text = chiSo.BMI;
            if (txbTDEE != null) txbTDEE.Text = chiSo.TDEE;
            if (dpNgayDo != null) dpNgayDo.SelectedDate = chiSo.NgayDo;
            if (cboMucTieuSK != null && !string.IsNullOrEmpty(chiSo.MucTieuSucKhoe))
            {
                foreach (ComboBoxItem item in cboMucTieuSK.Items)
                {
                    if (item.Content.ToString() == chiSo.MucTieuSucKhoe)
                    {
                        cboMucTieuSK.SelectedItem = item;
                        break;
                    }
                }
            }

            GrdOverlayPopup.Visibility = Visibility.Visible;
        }

        private void btnThem_Click(object sender, RoutedEventArgs e)
        {
            HienThiPopupThem();
        }

        private void btnSua_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var chiSoCanSua = button.Tag as MoDonDuLieuChiSoSucKhoe;
            if (chiSoCanSua == null) return;

            HienThiPopupSua(chiSoCanSua);
        }

        private void HuyPopup_Click(object sender, RoutedEventArgs e)
        {
            if (GrdOverlayPopup != null)
            {
                GrdOverlayPopup.Visibility = Visibility.Collapsed;
                MaPhieuDoDangSua = null;
            }
        }

        private void LuuPopup_Click(object sender, RoutedEventArgs e)
        {
            if (isThemChiSoMoi)
            {
                ThemChiSoMoi();
            }
            else
            {
                if (MaPhieuDoDangSua != null)
                {
                    CapNhatChiSo(MaPhieuDoDangSua);
                }
                else
                {
                    MessageBox.Show("Lỗi: Không tìm thấy Mã phiếu đo để cập nhật.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool KiemTraVaLayDuLieuHopLe(out double canNang, out double chieuCao, out DateTime ngayDo, out string tdee, out string mucTieu, out string maHV)
        {
            canNang = 0; chieuCao = 0; ngayDo = DateTime.Now; tdee = ""; mucTieu = ""; maHV = "";

            maHV = txbMaHV.Text.Trim();
            tdee = txbTDEE.Text.Trim();
            mucTieu = (cboMucTieuSK.SelectedItem as ComboBoxItem)?.Content.ToString()?.Trim() ?? "";

            if (string.IsNullOrEmpty(maHV))
            {
                MessageBox.Show("Vui lòng nhập Mã học viên (MaHV).", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!KiemTraTonTaiHocVien(maHV))
            {
                MessageBox.Show($"Mã học viên '{maHV}' không tồn tại trong hệ thống. Vui lòng kiểm tra lại Mã Học viên.", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!double.TryParse(txbCanNang.Text, out canNang) || canNang <= 0)
            {
                MessageBox.Show("Cân nặng không hợp lệ. Vui lòng nhập số dương cho Cân nặng (kg).", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }


            if (!double.TryParse(txbChieuCao.Text, out chieuCao) || chieuCao <= 0)
            {
                MessageBox.Show("Chiều cao không hợp lệ. Vui lòng nhập số dương cho Chiều cao (cm).", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (dpNgayDo.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn Ngày đo.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            ngayDo = dpNgayDo.SelectedDate.Value;


            if (!string.IsNullOrEmpty(tdee) && !double.TryParse(tdee, out _))
            {
                MessageBox.Show("TDEE không hợp lệ. Vui lòng nhập số (calo/ngày) hoặc để trống.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(mucTieu))
            {
                MessageBox.Show("Vui lòng chọn Mục tiêu sức khỏe.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ThemChiSoMoi()
        {
            if (!KiemTraVaLayDuLieuHopLe(out double canNang, out double chieuCao, out DateTime ngayDo, out string tdee, out string mucTieu, out string maHV))
            {
                return;
            }
            string maPhieuMoi = "CS" + DateTime.Now.ToString("yyyyMMddHHmmss");
            int tuoi = LayTuoiHocVien(maHV);

            if (ThucHienThemChiSo(maPhieuMoi, maHV, canNang, chieuCao, ngayDo, tdee, mucTieu, tuoi))
            {
                MessageBox.Show("Thêm chỉ số sức khỏe mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                HuyPopup_Click(null, null);
                TaiDanhSachChiSo();
            }
        }

        private void CapNhatChiSo(string maPhieuCanSua)
        {
            if (!KiemTraVaLayDuLieuHopLe(out double canNang, out double chieuCao, out DateTime ngayDo, out string tdee, out string mucTieu, out string maHV))
            {
                return;
            }
            int tuoi = LayTuoiHocVien(maHV);

            if (ThucHienCapNhatChiSo(maPhieuCanSua, canNang, chieuCao, ngayDo, tdee, mucTieu, tuoi))
            {
                MessageBox.Show("Cập nhật chỉ số sức khỏe thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                HuyPopup_Click(null, null);
                TaiDanhSachChiSo();
            }
        }

        private bool ThucHienThemChiSo(string maPhieu, string maHV, double canNang, double chieuCao,
                                      DateTime ngayDo, string tdee, string mucTieu, int tuoi)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string bmi = TinhBMI(canNang, chieuCao);

                    // Yêu cầu 5: Không lưu BMI
                    string sql = @"
                        INSERT INTO PhieuDoChiSo 
                        (MaPhieu, MaHV, CanNang, ChieuCao, Tuoi, TDEE, NgayDo, MucTieuSK)
                        VALUES (@MaPhieu, @MaHV, @CanNang, @ChieuCao, @Tuoi, @TDEE, @NgayDo, @MucTieuSK)";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaPhieu", maPhieu);
                        cmd.Parameters.AddWithValue("@MaHV", maHV);
                        cmd.Parameters.AddWithValue("@CanNang", canNang);
                        cmd.Parameters.AddWithValue("@ChieuCao", chieuCao);
                        cmd.Parameters.AddWithValue("@Tuoi", tuoi);
                        cmd.Parameters.AddWithValue("@TDEE", tdee);
                        cmd.Parameters.AddWithValue("@NgayDo", ngayDo.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@MucTieuSK", mucTieu);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thêm dữ liệu vào cơ sở dữ liệu: {ex.Message}",
                    "Lỗi Thêm Dữ Liệu DB", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool ThucHienCapNhatChiSo(string maPhieu, double canNang, double chieuCao,
                                             DateTime ngayDo, string tdee, string mucTieu, int tuoi)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string bmi = TinhBMI(canNang, chieuCao);
                    string sql = @"
                        UPDATE PhieuDoChiSo SET
                            CanNang = @CanNang,
                            ChieuCao = @ChieuCao,
                            Tuoi = @Tuoi,
                            TDEE = @TDEE,
                            NgayDo = @NgayDo,
                            MucTieuSK = @MucTieuSK
                        WHERE MaPhieu = @MaPhieu";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CanNang", canNang);
                        cmd.Parameters.AddWithValue("@ChieuCao", chieuCao);
                        cmd.Parameters.AddWithValue("@Tuoi", tuoi);
                        cmd.Parameters.AddWithValue("@TDEE", tdee);
                        cmd.Parameters.AddWithValue("@NgayDo", ngayDo.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@MucTieuSK", mucTieu);
                        cmd.Parameters.AddWithValue("@MaPhieu", maPhieu);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật dữ liệu vào cơ sở dữ liệu: {ex.Message}",
                    "Lỗi Cập Nhật Dữ Liệu DB", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void TinhToanBMI()
        {
            if (txbCanNang == null || txbChieuCao == null || txbBMI == null)
                return;

            if (double.TryParse(txbCanNang.Text, out double canNang) &&
                double.TryParse(txbChieuCao.Text, out double chieuCao) &&
                chieuCao > 0)
            {
                string bmi = TinhBMI(canNang, chieuCao);
                txbBMI.Text = bmi;
            }
            else
            {
                if (!string.IsNullOrEmpty(txbCanNang.Text) || !string.IsNullOrEmpty(txbChieuCao.Text))
                {
                    if (double.TryParse(txbCanNang.Text, out _) && double.TryParse(txbChieuCao.Text, out _) && double.Parse(txbChieuCao.Text) <= 0)
                    {
                        txbBMI.Text = "Lỗi (CC <= 0)";
                    }
                    else if (!double.TryParse(txbCanNang.Text, out _) || !double.TryParse(txbChieuCao.Text, out _))
                    {
                        txbBMI.Text = "Lỗi (Nhập số)";
                    }
                }
                else
                {
                    txbBMI.Text = "";
                }
            }
        }

        private void txbCanNang_TextChanged(object sender, TextChangedEventArgs e)
        {
            TinhToanBMI();
        }

        private void txbChieuCao_TextChanged(object sender, TextChangedEventArgs e)
        {
            TinhToanBMI();
        }

        private void btnXoaDon_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var chiSoCanXoa = button.Tag as MoDonDuLieuChiSoSucKhoe;
            if (chiSoCanXoa == null) return;

            string maPhieuDo = chiSoCanXoa.MaPhieuDo;

            MessageBoxResult result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa chỉ số sức khỏe có Mã phiếu đo: {maPhieuDo}?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (XoaChiSoSucKhoe(new List<string> { maPhieuDo }))
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    cacMucDaChon.Remove(maPhieuDo);
                    TaiDanhSachChiSo();
                }
            }
        }

        private void btnXoaChon_Click(object sender, RoutedEventArgs e)
        {
            if (cacMucDaChon.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một chỉ số sức khỏe để xóa.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa {cacMucDaChon.Count} mục đã chọn? Thao tác này không thể hoàn tác.",
                "Xác nhận xóa nhiều mục", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                List<string> maPhieuList = cacMucDaChon.ToList();

                if (XoaChiSoSucKhoe(maPhieuList))
                {
                    MessageBox.Show($"Đã xóa thành công **{maPhieuList.Count}** mục.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    cacMucDaChon.Clear();
                    TaiDanhSachChiSo();
                }
            }
        }

        private bool XoaChiSoSucKhoe(List<string> maPhieuList)
        {
            if (maPhieuList == null || maPhieuList.Count == 0) return true;

            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    var placeholders = string.Join(",", Enumerable.Range(0, maPhieuList.Count).Select(i => $"@p{i}"));
                    string sql = $"DELETE FROM PhieuDoChiSo WHERE MaPhieu IN ({placeholders})";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        for (int i = 0; i < maPhieuList.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@p{i}", maPhieuList[i]);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thực hiện xóa khỏi cơ sở dữ liệu: {ex.Message}",
                    "Lỗi Xóa Dữ Liệu DB", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        #endregion
    }

    public class MoDonDuLieuChiSoSucKhoe
    {
        public string MaPhieuDo { get; set; }
        public string MaHocVien { get; set; }
        public string HoTenHocVien { get; set; }
        public DateTime NgayDo { get; set; }
        public string ChieuCao { get; set; }
        public string CanNang { get; set; }
        public string BMI { get; set; }
        public string TDEE { get; set; }
        public int Tuoi { get; set; }
        public string MucTieuSucKhoe { get; set; }
        public bool DaChon { get; set; }
    }
}