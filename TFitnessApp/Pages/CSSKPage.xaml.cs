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
        private string chuoiKetNoi;
        private List<MoDonDuLieuChiSoSucKhoe> tatCaChiSo = new List<MoDonDuLieuChiSoSucKhoe>();
        private List<MoDonDuLieuChiSoSucKhoe> danhSachHienThi = new List<MoDonDuLieuChiSoSucKhoe>();

        // Pagination
        private int trangHienTai = 1;
        private const int soMucMotTrang = 10;
        private int tongSoTrang = 1;

        // Placeholder constants
        private const string VAN_BAN_TIM_KIEM_MAC_DINH = "Tìm kiếm học viên";

        // Tracking selected items
        private HashSet<string> cacMucDaChon = new HashSet<string>();

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

        #region Khởi tạo & Tải dữ liệu

        private void KhoiTaoGiaoDien()
        {
            if (SearchBox != null)
            {
                SearchBox.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                SearchBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }

            if (MeasurementDatePicker != null)
            {
                MeasurementDatePicker.SelectedDate = DateTime.Now;
            }

            TaiDanhSachHocVien();
        }

        private void TaiDanhSachHocVien()
        {
            try
            {
                var danhSachHocVien = new List<dynamic>();

                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string sql = "SELECT MaHV, HoTen FROM HocVien ORDER BY HoTen ASC";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            danhSachHocVien.Add(new
                            {
                                StudentId = reader["MaHV"].ToString(),
                                StudentName = $"{reader["MaHV"]} - {reader["HoTen"]}"
                            });
                        }
                    }
                }

                if (StudentComboBox != null)
                {
                    StudentComboBox.ItemsSource = danhSachHocVien;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách học viên: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            MaPhieu,
                            MaHV,
                            HoTen,
                            CanNang,
                            ChieuCao,
                            Tuoi,
                            MucTieuSK,
                            TDEE,
                            MaTK,
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
                                MaPhieu = reader["MaPhieu"].ToString(),
                                MaHocVien = reader["MaHV"].ToString(),
                                HoTenHocVien = reader["HoTen"].ToString(),
                                CanNang = reader["CanNang"] != DBNull.Value ?
                                    Convert.ToDouble(reader["CanNang"]).ToString("F1") : "0",
                                ChieuCao = reader["ChieuCao"] != DBNull.Value ?
                                    Convert.ToDouble(reader["ChieuCao"]).ToString("F0") : "0",
                                BMI = TinhBMI(
                                    reader["CanNang"] != DBNull.Value ? Convert.ToDouble(reader["CanNang"]) : 0,
                                    reader["ChieuCao"] != DBNull.Value ? Convert.ToDouble(reader["ChieuCao"]) : 0
                                ),
                                TDEE = reader["TDEE"] != DBNull.Value ?
                                    reader["TDEE"].ToString() : "0",
                                MucTieuSucKhoe = reader["MucTieuSK"]?.ToString() ?? "",
                                DaChon = false
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

        #endregion

        #region Phân trang

        private void CapNhatPhanTrang()
        {
            tongSoTrang = (int)Math.Ceiling((double)danhSachHienThi.Count / soMucMotTrang);
            if (tongSoTrang == 0) tongSoTrang = 1;

            if (trangHienTai > tongSoTrang) trangHienTai = tongSoTrang;

            HienThiTrangHienTai();
            CapNhatNutPhanTrang();
        }

        private void HienThiTrangHienTai()
        {
            var mucTrangHienTai = danhSachHienThi
                .Skip((trangHienTai - 1) * soMucMotTrang)
                .Take(soMucMotTrang)
                .ToList();

            if (StudentListControl != null)
            {
                StudentListControl.ItemsSource = mucTrangHienTai;
            }
        }

        private void CapNhatNutPhanTrang()
        {
            var danhSachTrang = new List<PageInfo>();

            for (int i = 1; i <= tongSoTrang; i++)
            {
                danhSachTrang.Add(new PageInfo
                {
                    PageNumber = i,
                    IsCurrentPage = i == trangHienTai
                });
            }

            if (PageNumbersControl != null)
            {
                PageNumbersControl.ItemsSource = danhSachTrang;
            }

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

        private void PageNumber_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                trangHienTai = Convert.ToInt32(button.Tag);
                CapNhatPhanTrang();
            }
        }

        #endregion

        #region Tìm kiếm

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                SearchBox.Text = "";
                SearchBox.Foreground = Brushes.Black;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                SearchBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBox == null) return;

            string tuKhoa = SearchBox.Text.Trim();

            if (string.IsNullOrEmpty(tuKhoa) || tuKhoa == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                danhSachHienThi = new List<MoDonDuLieuChiSoSucKhoe>(tatCaChiSo);
            }
            else
            {
                string tuKhoaKhongDau = BoQuyenDau(tuKhoa).ToLower();
                danhSachHienThi = tatCaChiSo.Where(item =>
                    BoQuyenDau(item.HoTenHocVien).ToLower().Contains(tuKhoaKhongDau) ||
                    item.MaHocVien.ToLower().Contains(tuKhoa.ToLower()) ||
                    item.MaPhieu.ToLower().Contains(tuKhoa.ToLower())
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

        #endregion

        #region Chọn tất cả

        private void SelectAllCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (SelectAllCheckBox == null) return;

            bool isChecked = SelectAllCheckBox.IsChecked == true;

            foreach (var item in danhSachHienThi)
            {
                item.DaChon = isChecked;

                if (isChecked)
                    cacMucDaChon.Add(item.MaPhieu);
                else
                    cacMucDaChon.Remove(item.MaPhieu);
            }

            if (StudentListControl != null)
            {
                StudentListControl.ItemsSource = null;
                StudentListControl.ItemsSource = danhSachHienThi
                    .Skip((trangHienTai - 1) * soMucMotTrang)
                    .Take(soMucMotTrang);
            }
        }

        #endregion

        #region Thêm/Sửa/Xóa

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Reset form
            if (StudentComboBox != null) StudentComboBox.SelectedIndex = -1;
            if (WeightTextBox != null) WeightTextBox.Clear();
            if (HeightTextBox != null) HeightTextBox.Clear();
            if (BMITextBox != null) BMITextBox.Clear();
            if (BodyFatTextBox != null) BodyFatTextBox.Clear();
            if (MuscleMassTextBox != null) MuscleMassTextBox.Clear();
            if (TDEETextBox != null) TDEETextBox.Clear();
            if (MeasurementDatePicker != null) MeasurementDatePicker.SelectedDate = DateTime.Now;

            if (PopupTitle != null) PopupTitle.Text = "Thêm chỉ số sức khỏe";
            if (PopupOverlay != null) PopupOverlay.Visibility = Visibility.Visible;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.Tag as MoDonDuLieuChiSoSucKhoe;

            if (item == null) return;

            try
            {
                // Load chi tiết từ database
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();
                    string sql = @"
                        SELECT p.*, h.HoTen 
                        FROM PhieuDoChiSo p
                        INNER JOIN HocVien h ON p.MaHV = h.MaHV
                        WHERE p.MaPhieu = @MaPhieu";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaPhieu", item.MaPhieu);

                        using (SqliteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Fill form
                                if (StudentComboBox != null)
                                {
                                    StudentComboBox.SelectedValue = reader["MaHV"].ToString();
                                }

                                if (WeightTextBox != null)
                                    WeightTextBox.Text = reader["CanNang"]?.ToString() ?? "";

                                if (HeightTextBox != null)
                                    HeightTextBox.Text = reader["ChieuCao"]?.ToString() ?? "";

                                if (BodyFatTextBox != null)
                                    BodyFatTextBox.Text = reader["TiLeMo"]?.ToString() ?? "";

                                if (MuscleMassTextBox != null)
                                    MuscleMassTextBox.Text = reader["KhoiLuongCo"]?.ToString() ?? "";

                                if (TDEETextBox != null)
                                    TDEETextBox.Text = reader["TDEE"]?.ToString() ?? "";

                                // Tự động tính BMI
                                TinhToanBMI();

                                //if (MeasurementDatePicker != null && reader["NgayDo"] != DBNull.Value)
                                //{
                                //    if (DateTime.TryParse(reader["NgayDo"].ToString(), out DateTime ngayDo))
                                //    {
                                //        MeasurementDatePicker.SelectedDate = ngayDo;
                                //    }
                                //}
                            }
                        }
                    }
                }

                if (PopupTitle != null) PopupTitle.Text = "Chỉnh sửa chỉ số sức khỏe";
                if (PopupOverlay != null) PopupOverlay.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thông tin: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePopup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (StudentComboBox == null || StudentComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Vui lòng chọn học viên!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(WeightTextBox?.Text) ||
                    string.IsNullOrWhiteSpace(HeightTextBox?.Text))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ cân nặng và chiều cao!",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string maHV = StudentComboBox.SelectedValue.ToString();
                double canNang = Convert.ToDouble(WeightTextBox.Text);
                double chieuCao = Convert.ToDouble(HeightTextBox.Text);
                double? tiLeMo = string.IsNullOrWhiteSpace(BodyFatTextBox?.Text) ?
                    (double?)null : Convert.ToDouble(BodyFatTextBox.Text);
                double? khoiLuongCo = string.IsNullOrWhiteSpace(MuscleMassTextBox?.Text) ?
                    (double?)null : Convert.ToDouble(MuscleMassTextBox.Text);
                int? tdee = string.IsNullOrWhiteSpace(TDEETextBox?.Text) ?
                    (int?)null : Convert.ToInt32(TDEETextBox.Text);
                DateTime ngayDo = MeasurementDatePicker?.SelectedDate ?? DateTime.Now;

                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();

                    // Tạo mã phiếu đo mới
                    string MaPhieu = TaoMaPhieuMoi(conn);

                    string sql = @"
                        INSERT INTO PhieuDoChiSo 
                        (MaPhieu, MaHV, CanNang, ChieuCao, TiLeMo, KhoiLuongCo, TDEE, NgayDo)
                        VALUES 
                        (@MaPhieu, @maHV, @canNang, @chieuCao, @tiLeMo, @khoiLuongCo, @tdee, @ngayDo)";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaPhieu", MaPhieu);
                        cmd.Parameters.AddWithValue("@maHV", maHV);
                        cmd.Parameters.AddWithValue("@canNang", canNang);
                        cmd.Parameters.AddWithValue("@chieuCao", chieuCao);
                        cmd.Parameters.AddWithValue("@tiLeMo", tiLeMo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@khoiLuongCo", khoiLuongCo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@tdee", tdee ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ngayDo", ngayDo.ToString("yyyy-MM-dd HH:mm:ss"));

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Lưu thành công!", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                if (PopupOverlay != null) PopupOverlay.Visibility = Visibility.Collapsed;
                TaiDanhSachChiSo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string TaoMaPhieuMoi(SqliteConnection conn)
        {
            string sql = "SELECT MAX(MaPhieu) FROM PhieuDoChiSo WHERE MaPhieu LIKE 'PD%'";

            using (SqliteCommand cmd = new SqliteCommand(sql, conn))
            {
                var maxMa = cmd.ExecuteScalar()?.ToString();
                int nextNumber = 1;

                if (!string.IsNullOrEmpty(maxMa) && maxMa.Length > 2 && maxMa.StartsWith("PD"))
                {
                    string numPart = maxMa.Substring(2);
                    if (int.TryParse(numPart, out int currentMax))
                    {
                        nextNumber = currentMax + 1;
                    }
                }

                return "PD" + nextNumber.ToString("D4");
            }
        }

        private void CancelPopup_Click(object sender, RoutedEventArgs e)
        {
            if (PopupOverlay != null) PopupOverlay.Visibility = Visibility.Collapsed;
        }

        private void DeleteSingle_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.Tag as MoDonDuLieuChiSoSucKhoe;

            if (item == null) return;

            var result = MessageBox.Show(
                $"Bạn có chắc muốn xóa phiếu đo {item.MaPhieu}?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                XoaChiSo(new List<string> { item.MaPhieu });
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var cacMucCanXoa = danhSachHienThi
                .Where(item => item.DaChon)
                .Select(item => item.MaPhieu)
                .ToList();

            if (cacMucCanXoa.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một mục để xóa!",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc muốn xóa {cacMucCanXoa.Count} phiếu đo đã chọn?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                XoaChiSo(cacMucCanXoa);
            }
        }

        private void XoaChiSo(List<string> danhSachMaPhieu)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();

                    foreach (string MaPhieu in danhSachMaPhieu)
                    {
                        string sql = "DELETE FROM PhieuDoChiSo WHERE MaPhieu = @MaPhieu";

                        using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@MaPhieu", MaPhieu);
                            cmd.ExecuteNonQuery();
                        }

                        cacMucDaChon.Remove(MaPhieu);
                    }
                }

                MessageBox.Show($"Đã xóa {danhSachMaPhieu.Count} phiếu đo thành công!",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                TaiDanhSachChiSo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewDetail_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.Tag as MoDonDuLieuChiSoSucKhoe;

            if (item == null) return;

            MessageBox.Show(
                $"Mã phiếu đo: {item.MaPhieu}\n" +
                $"Học viên: {item.HoTenHocVien} ({item.MaHocVien})\n" +
                $"Cân nặng: {item.CanNang} kg\n" +
                $"Chiều cao: {item.ChieuCao} cm\n" +
                $"BMI: {item.BMI}\n" +
                $"TDEE: {item.TDEE} calo/ngày\n" +
                $"Mục tiêu: {item.MucTieuSucKhoe}\n" +
                $"Ngày đo: {item.NgayDo:dd/MM/yyyy}",
                "Chi tiết phiếu đo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion

        #region Tính toán BMI

        private void TinhToanBMI()
        {
            if (WeightTextBox == null || HeightTextBox == null || BMITextBox == null)
                return;

            if (double.TryParse(WeightTextBox.Text, out double canNang) &&
                double.TryParse(HeightTextBox.Text, out double chieuCao) &&
                chieuCao > 0)
            {
                string bmi = TinhBMI(canNang, chieuCao);
                BMITextBox.Text = bmi;
            }
            else
            {
                BMITextBox.Text = "";
            }
        }

        #endregion

        #region Không sử dụng

        private void BackButton_Click(object sender, RoutedEventArgs e) { }
        private void BackButton_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) { }
        private void SelectAll_Click(object sender, RoutedEventArgs e) { }
        private void SearchButton_Click(object sender, RoutedEventArgs e) { }
        private void FilterButton_Click(object sender, RoutedEventArgs e) { }

        #endregion
    }

    #region Model Classes

    public class MoDonDuLieuChiSoSucKhoe
    {
        public string MaPhieu { get; set; }
        public string MaHocVien { get; set; }
        public string HoTenHocVien { get; set; }
        public DateTime NgayDo { get; set; }
        public string ChieuCao { get; set; }
        public string CanNang { get; set; }
        public string BMI { get; set; }
        public string TDEE { get; set; }
        public string MucTieuSucKhoe { get; set; }
        public bool DaChon { get; set; }
    }

    public class PageInfo
    {
        public int PageNumber { get; set; }
        public bool IsCurrentPage { get; set; }
    }

    #endregion
}