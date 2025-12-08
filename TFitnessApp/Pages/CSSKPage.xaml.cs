using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TFitnessApp.Database;

namespace TFitnessApp
{
    public partial class CSSKPage : UserControl
    {
        private string _ChuoiKetNoi;
        private readonly TruyCapDB _dbAccess;

        private List<MoDonDuLieuChiSoSucKhoe> tatCaChiSo = new List<MoDonDuLieuChiSoSucKhoe>();
        private List<MoDonDuLieuChiSoSucKhoe> danhSachHienThi = new List<MoDonDuLieuChiSoSucKhoe>();

        // ----------------------------------------------------------------------
        // #region BIẾN TRẠNG THÁI VÀ PHÂN TRANG
        // ----------------------------------------------------------------------

        private int trangHienTai = 1;
        private const int soMucMotTrang = 10;
        private int tongSoTrang = 1;

        private const string VAN_BAN_TIM_KIEM_MAC_DINH = "Tìm kiếm theo mã phiếu, mã HV, chiều cao,...";

        private HashSet<string> cacMucDaChon = new HashSet<string>();
        private bool isAddingNew = true;
        private string maPhieuDoDangSua = null;

        // ----------------------------------------------------------------------
        // #region KHỞI TẠO
        // ----------------------------------------------------------------------

        public CSSKPage()
        {
            InitializeComponent();
            // Khởi tạo đối tượng DbAccess
            _dbAccess = new TruyCapDB();
            // Lấy chuỗi kết nối
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;

            KhoiTaoGiaoDien();
            TaiDanhSachChiSo();
            TaiDanhSachHocVien();
        }

        private void KhoiTaoGiaoDien()
        {
            if (TimKiemBox != null)
            {
                TimKiemBox.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                TimKiemBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }

            if (NgayDoDatePickerPopUp != null)
            {
                NgayDoDatePickerPopUp.SelectedDate = DateTime.Now;
            }
        }

        // ----------------------------------------------------------------------
        // #region TẢI DỮ LIỆU & LOGIC HỖ TRỢ
        // ----------------------------------------------------------------------

        private int LayTuoiHocVien(string maHV)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
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

        private void TaiDanhSachHocVien()
        {
            List<StudentModel> studentList = new List<StudentModel>();
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string sql = "SELECT MaHV, HoTen, GioiTinh, NgaySinh FROM HocVien ORDER BY HoTen ASC";
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            studentList.Add(new StudentModel
                            {
                                StudentId = reader["MaHV"].ToString(),
                                StudentName = $"{reader["MaHV"]} - {reader["HoTen"]}"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách học viên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (MaHVTextBox != null)
            {
                MaHVTextBox.Text = string.Empty;
            }
        }

        private void TaiDanhSachChiSo()
        {
            try
            {
                tatCaChiSo.Clear();

                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();

                    string sql = @"
                        SELECT 
                            p.MaPhieu,
                            p.MaHV,
                            h.HoTen,
                            p.CanNang,
                            p.ChieuCao,
                            p.Tuoi,
                            p.MucTieuSK,
                            p.TDEE,
                            COALESCE(p.NgayDo, datetime('now')) as NgayDo 
                        FROM PhieuDoChiSo p
                        INNER JOIN HocVien h ON p.MaHV = h.MaHV
                        ORDER BY p.NgayDo DESC";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new MoDonDuLieuChiSoSucKhoe
                            {
                                MaPhieuDo = reader["MaPhieu"].ToString(),
                                MaHocVien = reader["MaHV"].ToString(),
                                HoTenHocVien = reader["HoTen"].ToString(),
                                CanNang = reader["CanNang"] != DBNull.Value ?
                                    Convert.ToDouble(reader["CanNang"]).ToString("F1") : "0",
                                ChieuCao = reader["ChieuCao"] != DBNull.Value ?
                                    Convert.ToDouble(reader["ChieuCao"]).ToString("F0") : "0",
                                Tuoi = reader["Tuoi"] != DBNull.Value ?
                                    Convert.ToInt32(reader["Tuoi"]) : 0,
                                BMI = TinhBMI(
                                    reader["CanNang"] != DBNull.Value ? Convert.ToDouble(reader["CanNang"]) : 0,
                                    reader["ChieuCao"] != DBNull.Value ? Convert.ToDouble(reader["ChieuCao"]) : 0
                                ),
                                TDEE = reader["TDEE"] != DBNull.Value ?
                                    reader["TDEE"].ToString() : "0",
                                MucTieuSucKhoe = reader["MucTieuSK"]?.ToString() ?? "",
                                DaChon = cacMucDaChon.Contains(reader["MaPhieu"].ToString())
                            };

                            if (DateTime.TryParse(reader["NgayDo"].ToString(), out DateTime ngayDo))
                            {
                                item.NgayDo = ngayDo;
                            }
                            else
                            {
                                item.NgayDo = DateTime.Now;
                            }

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

        // ----------------------------------------------------------------------
        // #region PHÂN TRANG
        // ----------------------------------------------------------------------

        private void CapNhatPhanTrang()
        {
            tongSoTrang = (int)Math.Ceiling((double)danhSachHienThi.Count / soMucMotTrang);
            if (tongSoTrang == 0) tongSoTrang = 1;

            if (trangHienTai > tongSoTrang) trangHienTai = tongSoTrang;
            if (trangHienTai < 1) trangHienTai = 1;

            HienThiTrangHienTai();
            CapNhatNutPhanTrang();
        }

        private void HienThiTrangHienTai()
        {
            var mucTrangHienTai = danhSachHienThi
                .Skip((trangHienTai - 1) * soMucMotTrang)
                .Take(soMucMotTrang)
                .ToList();

            if (DanhSachChiSoControl != null)
            {
                DanhSachChiSoControl.ItemsSource = mucTrangHienTai;
            }

            if (PageInfoTextBlock != null)
            {
                PageInfoTextBlock.Text = $"Trang {trangHienTai}/{tongSoTrang}";
            }
        }

        private void CapNhatNutPhanTrang()
        {
            if (FirstButton != null) FirstButton.IsEnabled = trangHienTai > 1;
            if (PrevButton != null) PrevButton.IsEnabled = trangHienTai > 1;
            if (NextButton != null) NextButton.IsEnabled = trangHienTai < tongSoTrang;
            if (LastButton != null) LastButton.IsEnabled = trangHienTai < tongSoTrang;
        }

        private void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            trangHienTai = 1;
            CapNhatPhanTrang();
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (trangHienTai > 1)
            {
                trangHienTai--;
                CapNhatPhanTrang();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (trangHienTai < tongSoTrang)
            {
                trangHienTai++;
                CapNhatPhanTrang();
            }
        }

        private void LastPage_Click(object sender, RoutedEventArgs e)
        {
            trangHienTai = tongSoTrang;
            CapNhatPhanTrang();
        }

        // ----------------------------------------------------------------------
        // #region TÌM KIẾM & LỌC
        // ----------------------------------------------------------------------

        private void TimKiemBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TimKiemBox.Text == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                TimKiemBox.Text = "";
                TimKiemBox.Foreground = Brushes.Black;
            }
        }

        private void TimKiemBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TimKiemBox.Text))
            {
                TimKiemBox.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                TimKiemBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
        }

        private void TimKiemButton_Click(object sender, RoutedEventArgs e)
        {
            if (TimKiemBox == null) return;

            string tuKhoa = TimKiemBox.Text.Trim();

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
                    BoQuyenDau(item.MucTieuSucKhoe).ToLower().Contains(tuKhoaKhongDau)
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

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilterOverlay != null)
            {
                FilterOverlay.Visibility = Visibility.Visible;
            }
        }

        private void CloseFilter_Click(object sender, RoutedEventArgs e)
        {
            if (FilterOverlay != null)
            {
                FilterOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void CancelFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterMaPhieuTextBox.Text = string.Empty;
            FilterMaHVTextBox.Text = string.Empty;
            FilterHoTenTextBox.Text = string.Empty;
            FilterChieuCaoTextBox.Text = string.Empty;
            FilterCanNangTextBox.Text = string.Empty;
            FilterBMITextBox.Text = string.Empty;
            FilterTDEETextBox.Text = string.Empty;
            FilterTuoiTextBox.Text = string.Empty;
            FilterMucTieuTextBox.Text = string.Empty;
            FilterNgayDoDatePicker.SelectedDate = null;

            danhSachHienThi = new List<MoDonDuLieuChiSoSucKhoe>(tatCaChiSo);
            trangHienTai = 1;
            CapNhatPhanTrang();

            CloseFilter_Click(sender, e);
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            ApDungBoLoc();
            CloseFilter_Click(sender, e);
        }

        private void ApDungBoLoc()
        {
            string maPhieu = FilterMaPhieuTextBox.Text.Trim().ToLower();
            string maHV = FilterMaHVTextBox.Text.Trim().ToLower();
            string hoTen = FilterHoTenTextBox.Text.Trim().ToLower();
            string chieuCao = FilterChieuCaoTextBox.Text.Trim();
            string canNang = FilterCanNangTextBox.Text.Trim();
            string bmi = FilterBMITextBox.Text.Trim();
            string tdee = FilterTDEETextBox.Text.Trim();
            string tuoi = FilterTuoiTextBox.Text.Trim();
            string mucTieu = FilterMucTieuTextBox.Text.Trim().ToLower();
            DateTime? ngayDo = FilterNgayDoDatePicker.SelectedDate;

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
            if (!string.IsNullOrEmpty(tuoi) && int.TryParse(tuoi, out int age))
            {
                ketQuaLoc = ketQuaLoc.Where(item => item.Tuoi == age);
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
        }

        // ----------------------------------------------------------------------
        // #region CHỌN (SELECTION)
        // ----------------------------------------------------------------------

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (ChonTatCaCheckBox == null) return;
            ChonTatCaCheckBox.IsChecked = ChonTatCaCheckBox.IsChecked != true;
        }

        private void ChonTatCaCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (ChonTatCaCheckBox == null) return;

            bool isChecked = ChonTatCaCheckBox.IsChecked == true;

            foreach (var item in danhSachHienThi)
            {
                item.DaChon = isChecked;

                if (isChecked)
                    cacMucDaChon.Add(item.MaPhieuDo);
                else
                    cacMucDaChon.Remove(item.MaPhieuDo);
            }

            if (DanhSachChiSoControl != null)
            {
                DanhSachChiSoControl.Items.Refresh();
            }
        }

        private void DataGridRow_CheckBox_Changed(object sender, RoutedEventArgs e)
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

            if (DanhSachChiSoControl != null)
            {
                DanhSachChiSoControl.Items.Refresh();
            }
        }

        // ----------------------------------------------------------------------
        // #region THÊM/SỬA POPUP
        // ----------------------------------------------------------------------

        private void ResetPopupFields()
        {
            MaHVTextBox.Text = string.Empty;
            CanNangTextBox.Text = string.Empty;
            ChieuCaoTextBox.Text = string.Empty;
            TDEETextBoxPopUp.Text = string.Empty;
            NgayDoDatePickerPopUp.SelectedDate = DateTime.Now;
        }

        private void HienThiPopupThem()
        {
            if (PopupOverlay == null || PopupTitle == null) return;
            ResetPopupFields();
            PopupTitle.Text = "Thêm chỉ số sức khỏe";
            isAddingNew = true;
            maPhieuDoDangSua = null;
            MaHVTextBox.Text = string.Empty;
            MaHVTextBox.IsEnabled = true;

            PopupOverlay.Visibility = Visibility.Visible;
        }

        private void HienThiPopupSua(MoDonDuLieuChiSoSucKhoe chiSo)
        {
            if (PopupOverlay == null || PopupTitle == null || chiSo == null) return;

            ResetPopupFields();

            PopupTitle.Text = $"Sửa chỉ số: {chiSo.MaPhieuDo}";
            isAddingNew = false;
            maPhieuDoDangSua = chiSo.MaPhieuDo;

            MaHVTextBox.Text = chiSo.MaHocVien;
            MaHVTextBox.IsEnabled = false;

            CanNangTextBox.Text = chiSo.CanNang;
            ChieuCaoTextBox.Text = chiSo.ChieuCao;
            TDEETextBoxPopUp.Text = chiSo.TDEE;
            NgayDoDatePickerPopUp.SelectedDate = chiSo.NgayDo;
            PopupOverlay.Visibility = Visibility.Visible;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            HienThiPopupThem();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var chiSoCanSua = button.Tag as MoDonDuLieuChiSoSucKhoe;
            if (chiSoCanSua == null) return;

            HienThiPopupSua(chiSoCanSua);
        }

        private void CancelPopup_Click(object sender, RoutedEventArgs e)
        {
            if (PopupOverlay != null)
            {
                PopupOverlay.Visibility = Visibility.Collapsed;
                maPhieuDoDangSua = null;
            }
        }

        private void SavePopup_Click(object sender, RoutedEventArgs e)
        {
            if (isAddingNew)
            {
                ThemChiSoMoi();
            }
            else
            {
                if (maPhieuDoDangSua != null)
                {
                    CapNhatChiSo(maPhieuDoDangSua);
                }
                else
                {
                    MessageBox.Show("Lỗi: Không tìm thấy Mã phiếu đo để cập nhật.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ThemChiSoMoi()
        {
            if (MaHVTextBox.Text == null)
            {
                MessageBox.Show("Vui lòng chọn Học viên.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(CanNangTextBox.Text, out double canNang) || canNang <= 0)
            {
                MessageBox.Show("Cân nặng không hợp lệ.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(ChieuCaoTextBox.Text, out double chieuCao) || chieuCao <= 0)
            {
                MessageBox.Show("Chiều cao không hợp lệ.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string maHV = MaHVTextBox.Text.ToString();
            DateTime ngayDo = NgayDoDatePickerPopUp.SelectedDate ?? DateTime.Now;
            string tdee = TDEETextBoxPopUp.Text.Trim();
            string mucTieu = string.Empty;

            string maPhieuMoi = "CS" + DateTime.Now.ToString("yyyyMMddHHmmss");
            int tuoi = LayTuoiHocVien(maHV);

            if (ThucHienThemChiSo(maPhieuMoi, maHV, canNang, chieuCao, ngayDo, tdee, mucTieu, tuoi))
            {
                MessageBox.Show("Thêm chỉ số sức khỏe mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                CancelPopup_Click(null, null);
                TaiDanhSachChiSo();
            }
        }

        private void CapNhatChiSo(string maPhieuCanSua)
        {
            if (!double.TryParse(CanNangTextBox.Text, out double canNang) || canNang <= 0)
            {
                MessageBox.Show("Cân nặng không hợp lệ.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(ChieuCaoTextBox.Text, out double chieuCao) || chieuCao <= 0)
            {
                MessageBox.Show("Chiều cao không hợp lệ.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (NgayDoDatePickerPopUp.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn Ngày đo.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime ngayDo = NgayDoDatePickerPopUp.SelectedDate.Value;
            string tdee = TDEETextBoxPopUp.Text.Trim();
            string maHV = MaHVTextBox.Text.ToString();
            string mucTieu = string.Empty;

            int tuoi = LayTuoiHocVien(maHV);

            if (ThucHienCapNhatChiSo(maPhieuCanSua, canNang, chieuCao, ngayDo, tdee, mucTieu, tuoi))
            {
                MessageBox.Show("Cập nhật chỉ số sức khỏe thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                CancelPopup_Click(null, null);
                TaiDanhSachChiSo();
            }
        }

        private bool ThucHienThemChiSo(string maPhieu, string maHV, double canNang, double chieuCao,
                                      DateTime ngayDo, string tdee, string mucTieu, int tuoi)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
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
                MessageBox.Show($"Lỗi khi thêm dữ liệu không hợp lệ: {ex.Message}",
                    "Lỗi thêm dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool ThucHienCapNhatChiSo(string maPhieu, double canNang, double chieuCao, DateTime ngayDo, string tdee, string mucTieu, int tuoi)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
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
                MessageBox.Show($"Lỗi khi cập nhật dữ liệu: {ex.Message}",
                    "Lỗi Cập Nhật Dữ Liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void TinhToanBMI()
        {
            if (CanNangTextBox == null || ChieuCaoTextBox == null || BMITextBoxPopUp == null)
                return;
            if (double.TryParse(CanNangTextBox.Text, out double canNang) &&
                double.TryParse(ChieuCaoTextBox.Text, out double chieuCao) &&
                chieuCao > 0)
            {
                string bmi = TinhBMI(canNang, chieuCao);
                BMITextBoxPopUp.Text = bmi;
            }
            else
            {
                BMITextBoxPopUp.Text = "";
            }
        }

        private void CanNangTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TinhToanBMI();
        }

        private void ChieuCaoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TinhToanBMI();
        }

        // ----------------------------------------------------------------------
        // #region XÓA
        // ----------------------------------------------------------------------

        private void DeleteSingle_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            var chiSoCanXoa = button.DataContext as MoDonDuLieuChiSoSucKhoe;
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

        private void Delete_Click(object sender, RoutedEventArgs e)
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
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
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
                MessageBox.Show($"Lỗi khi thực hiện xóa: {ex.Message}",
                    "Lỗi Xóa Dữ Liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void XemChiTiet_Click(object sender, RoutedEventArgs e) { /* Giữ nguyên logic Xem chi tiết */ }

        private void TimKiemBox_TextChanged(object sender, TextChangedEventArgs e) { }
        private void MaHVTextBox_TextChanged(object sender, TextChangedEventArgs e) { }

        // ----------------------------------------------------------------------
        // #endregion
        // ----------------------------------------------------------------------
    }

    // ----------------------------------------------------------------------
    // #region MODEL CLASSES
    // ----------------------------------------------------------------------

    public class StudentModel
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
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

    public class PageInfo
    {
        public int PageNumber { get; set; }
        public bool IsCurrentPage { get; set; }
    }

    // ----------------------------------------------------------------------
    // #endregion
    // ----------------------------------------------------------------------
}