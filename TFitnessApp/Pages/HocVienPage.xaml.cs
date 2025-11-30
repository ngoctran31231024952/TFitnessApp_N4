using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using System.Globalization;
using TFitnessApp.Windows;

namespace TFitnessApp
{
    public partial class HocVienPage : Page
    {
        private HocVienRepository _hocVienRepository;
        private List<HocVien> _danhSachGoc = new List<HocVien>();
        private ObservableCollection<HocVien> _danhSachHienThi = new ObservableCollection<HocVien>();

        private int _trangHienTai = 1;
        private int _soBanGhiMoiTrang = 50;
        private int _tongSoTrang = 1;
        private int _tongSoBanGhi = 0;

        private string _currentFilterGioiTinh = "Tất cả";
        private DateTime? _filterTuNgay = null;
        private DateTime? _filterDenNgay = null;
        private string _filterNgayLabel = "Tất cả ngày";

        public HocVienPage()
        {
            InitializeComponent();
            _hocVienRepository = new HocVienRepository();
            if (cboSoBanGhi != null && cboSoBanGhi.Items.Count > 2) cboSoBanGhi.SelectedIndex = 2;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadHocVienData();
        }

        private void LoadHocVienData()
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            try
            {
                string keyword = txtSearch.Text.Trim();
                _danhSachGoc = _hocVienRepository.FindHocVien(keyword, _currentFilterGioiTinh, _filterTuNgay, _filterDenNgay);
                _trangHienTai = 1;
                HienThiDuLieuPhanTrang();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tìm kiếm: {ex.Message}");
            }
        }

        private void HienThiDuLieuPhanTrang()
        {
            if (_danhSachGoc == null) return;

            _tongSoBanGhi = _danhSachGoc.Count;
            _tongSoTrang = (int)Math.Ceiling((double)_tongSoBanGhi / _soBanGhiMoiTrang);
            if (_tongSoTrang == 0) _tongSoTrang = 1;

            if (_trangHienTai > _tongSoTrang) _trangHienTai = _tongSoTrang;
            if (_trangHienTai < 1) _trangHienTai = 1;

            var dataPage = _danhSachGoc.Skip((_trangHienTai - 1) * _soBanGhiMoiTrang).Take(_soBanGhiMoiTrang).ToList();

            _danhSachHienThi.Clear();
            foreach (var item in dataPage) _danhSachHienThi.Add(item);

            if (HocVienDataGrid != null) HocVienDataGrid.ItemsSource = _danhSachHienThi;

            UpdatePaginationUI();
        }

        private void UpdatePaginationUI()
        {
            if (txtThongTinPhanTrang == null || txtTrangHienTai == null || txtTongSoTrang == null ||
                btnTrangDau == null || btnTrangTruoc == null || btnTrangSau == null || btnTrangCuoi == null) return;

            int start = (_tongSoBanGhi == 0) ? 0 : (_trangHienTai - 1) * _soBanGhiMoiTrang + 1;
            int end = Math.Min(_trangHienTai * _soBanGhiMoiTrang, _tongSoBanGhi);

            txtThongTinPhanTrang.Text = $"Hiển thị {start}-{end} của {_tongSoBanGhi} học viên";
            txtTrangHienTai.Text = _trangHienTai.ToString();
            txtTongSoTrang.Text = _tongSoTrang.ToString();

            btnTrangDau.IsEnabled = _trangHienTai > 1;
            btnTrangTruoc.IsEnabled = _trangHienTai > 1;
            btnTrangSau.IsEnabled = _trangHienTai < _tongSoTrang;
            btnTrangCuoi.IsEnabled = _trangHienTai < _tongSoTrang;
        }

        // --- SỰ KIỆN NÚT TRONG DATAGRID (XEM - SỬA - XÓA) ---

        // 1. Xem chi tiết
        private void BtnXem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HocVien hv)
            {
                // Mở cửa sổ Xem thông tin (sẽ tạo ở bước sau)
                XemThongTinHocVienWindow viewWindow = new XemThongTinHocVienWindow(hv);
                viewWindow.ShowDialog();
            }
        }

        // 2. Sửa thông tin
        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HocVien hv)
            {
                // Mở cửa sổ Thêm nhưng truyền data vào để thành chế độ Sửa
                ThemHocVienWindow editWindow = new ThemHocVienWindow(hv);
                editWindow.ShowDialog();

                if (editWindow.IsSuccess)
                {
                    LoadHocVienData(); // Load lại nếu sửa thành công
                }
            }
        }

        // 3. Xóa 1 dòng
        private void BtnXoaRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HocVien hv)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa học viên {hv.HoTen} ({hv.MaHV})?",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_hocVienRepository.DeleteHocVien(hv.MaHV))
                    {
                        // Xóa ảnh nếu có
                        try
                        {
                            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HocVienImages", $"{hv.MaHV}.jpg");
                            if (File.Exists(imagePath)) File.Delete(imagePath);
                            imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HocVienImages", $"{hv.MaHV}.png");
                            if (File.Exists(imagePath)) File.Delete(imagePath);
                        }
                        catch { }

                        MessageBox.Show("Đã xóa thành công.", "Thông báo");
                        LoadHocVienData();
                    }
                    else
                    {
                        MessageBox.Show("Xóa thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // --- CÁC SỰ KIỆN KHÁC (GIỮ NGUYÊN) ---
        private void cboSoBanGhi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSoBanGhi == null) return;
            if (cboSoBanGhi.SelectedItem != null)
            {
                _soBanGhiMoiTrang = (int)cboSoBanGhi.SelectedItem;
                _trangHienTai = 1;
                HienThiDuLieuPhanTrang();
            }
        }
        private void btnTrangDau_Click(object sender, RoutedEventArgs e) { _trangHienTai = 1; HienThiDuLieuPhanTrang(); }
        private void btnTrangTruoc_Click(object sender, RoutedEventArgs e) { if (_trangHienTai > 1) { _trangHienTai--; HienThiDuLieuPhanTrang(); } }
        private void btnTrangSau_Click(object sender, RoutedEventArgs e) { if (_trangHienTai < _tongSoTrang) { _trangHienTai++; HienThiDuLieuPhanTrang(); } }
        private void btnTrangCuoi_Click(object sender, RoutedEventArgs e) { _trangHienTai = _tongSoTrang; HienThiDuLieuPhanTrang(); }

        private void txtTrangHienTai_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(txtTrangHienTai.Text, out int newPage) && newPage >= 1 && newPage <= _tongSoTrang)
                {
                    _trangHienTai = newPage; HienThiDuLieuPhanTrang();
                }
                else
                {
                    txtTrangHienTai.Text = _trangHienTai.ToString();
                }
            }
        }
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) { PerformSearch(); }
        private void HocVienDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            ThemHocVienWindow addWindow = new ThemHocVienWindow(); // Mặc định là thêm mới
            addWindow.ShowDialog();
            if (addWindow.IsSuccess) LoadHocVienData();
        }
        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            var items = _danhSachHienThi;
            var itemsToDelete = items.Where(x => x.IsSelected).ToList();
            if (itemsToDelete.Count == 0) { MessageBox.Show("Vui lòng tích chọn ít nhất một học viên để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa {itemsToDelete.Count} học viên đã chọn?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                int deleteCount = 0;
                foreach (var item in itemsToDelete) { if (_hocVienRepository.DeleteHocVien(item.MaHV)) deleteCount++; }
                if (deleteCount > 0) { MessageBox.Show($"Đã xóa thành công {deleteCount} học viên.", "Thông báo"); LoadHocVienData(); }
            }
        }
        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        { /* Code lọc giữ nguyên */
            ContextMenu filterMenu = new ContextMenu();
            MenuItem headerGioiTinh = new MenuItem { Header = "GIỚI TÍNH", IsEnabled = false, FontWeight = FontWeights.Bold };
            MenuItem itemTatCaGT = new MenuItem { Header = "Tất cả", IsChecked = (_currentFilterGioiTinh == "Tất cả") }; itemTatCaGT.Click += (s, args) => ApplyFilterGioiTinh("Tất cả");
            MenuItem itemNam = new MenuItem { Header = "Nam", IsChecked = (_currentFilterGioiTinh == "Nam") }; itemNam.Click += (s, args) => ApplyFilterGioiTinh("Nam");
            MenuItem itemNu = new MenuItem { Header = "Nữ", IsChecked = (_currentFilterGioiTinh == "Nữ") }; itemNu.Click += (s, args) => ApplyFilterGioiTinh("Nữ");
            filterMenu.Items.Add(headerGioiTinh); filterMenu.Items.Add(itemTatCaGT); filterMenu.Items.Add(itemNam); filterMenu.Items.Add(itemNu); filterMenu.Items.Add(new Separator());
            MenuItem headerNgaySinh = new MenuItem { Header = "NGÀY SINH", IsEnabled = false, FontWeight = FontWeights.Bold };
            MenuItem itemTatCaNgay = new MenuItem { Header = "Tất cả ngày", IsChecked = (_filterNgayLabel == "Tất cả ngày") }; itemTatCaNgay.Click += (s, args) => ApplyFilterNgay("Tất cả ngày", null, null);
            MenuItem itemChonNam = new MenuItem { Header = "Chọn năm sinh..." };
            int currentYear = DateTime.Now.Year;
            for (int year = 1980; year <= currentYear - 5; year++)
            {
                MenuItem yearItem = new MenuItem { Header = $"Năm {year}", IsChecked = (_filterNgayLabel == $"Năm {year}") }; int selectedYear = year;
                yearItem.Click += (s, args) => ApplyFilterNgay($"Năm {selectedYear}", new DateTime(selectedYear, 1, 1), new DateTime(selectedYear, 12, 31));
                itemChonNam.Items.Add(yearItem);
            }
            filterMenu.Items.Add(headerNgaySinh); filterMenu.Items.Add(itemTatCaNgay); filterMenu.Items.Add(itemChonNam);
            if (sender is Button btn) { btn.ContextMenu = filterMenu; btn.ContextMenu.IsOpen = true; }
        }
        private void ApplyFilterGioiTinh(string gioiTinh) { _currentFilterGioiTinh = gioiTinh; PerformSearch(); }
        private void ApplyFilterNgay(string label, DateTime? tu, DateTime? den) { _filterNgayLabel = label; _filterTuNgay = tu; _filterDenNgay = den; PerformSearch(); }
    }

    public class HocVien
    {
        public string MaHV { get; set; }
        public string HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string GioiTinh { get; set; }
        public string Email { get; set; }
        public string SDT { get; set; }
        public string DiaChi { get; set; }
        public bool IsSelected { get; set; }
    }

    public class HocVienRepository
    {
        private readonly string _connectionString;
        public HocVienRepository()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            if (!File.Exists(dbPath)) dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TFitness.db");
            _connectionString = $"Data Source={dbPath};";
        }
        public string GenerateNewMaHV()
        {
            string newMaHV = "HV0001";
            try { using (var conn = new SqliteConnection(_connectionString)) { conn.Open(); string sql = "SELECT MaHV FROM HocVien ORDER BY length(MaHV) DESC, MaHV DESC LIMIT 1"; using (var cmd = new SqliteCommand(sql, conn)) { var result = cmd.ExecuteScalar(); if (result != null && result != DBNull.Value) { string maxMa = result.ToString(); if (maxMa.Length > 2 && int.TryParse(maxMa.Substring(2), out int currentNum)) { newMaHV = $"HV{(currentNum + 1).ToString("D4")}"; } } } } } catch { }
            return newMaHV;
        }
        public List<HocVien> FindHocVien(string keyword, string gioiTinh = "Tất cả", DateTime? tuNgay = null, DateTime? denNgay = null)
        {
            List<HocVien> danhSachHocVien = new List<HocVien>();
            string dbPathCheck = _connectionString.Replace("Data Source=", "").Replace(";", "");
            if (!File.Exists(dbPathCheck)) return danhSachHocVien;
            string sql = @"SELECT MaHV, HoTen, NgaySinh, GioiTinh, Email, SDT FROM HocVien WHERE (MaHV LIKE @keyword OR HoTen LIKE @keyword OR SDT LIKE @keyword OR Email LIKE @keyword)";
            if (gioiTinh != "Tất cả" && !string.IsNullOrEmpty(gioiTinh)) sql += " AND GioiTinh = @gioiTinh";
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open(); using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                        if (gioiTinh != "Tất cả" && !string.IsNullOrEmpty(gioiTinh)) command.Parameters.AddWithValue("@gioiTinh", gioiTinh);
                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime? ngaySinh = null; string ngaySinhStr = reader["NgaySinh"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(ngaySinhStr)) { string[] formats = { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "d-M-yyyy", "d/M/yyyy", "M/d/yyyy" }; if (DateTime.TryParseExact(ngaySinhStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ns)) if (ns.Year > 1900) ngaySinh = ns; }
                                var hv = new HocVien { MaHV = reader["MaHV"].ToString(), HoTen = reader["HoTen"].ToString(), NgaySinh = ngaySinh, GioiTinh = reader["GioiTinh"].ToString(), Email = reader["Email"].ToString(), SDT = reader["SDT"].ToString(), DiaChi = "", IsSelected = false };
                                bool passDateFilter = true;
                                if (tuNgay.HasValue && (!hv.NgaySinh.HasValue || hv.NgaySinh.Value.Date < tuNgay.Value.Date)) passDateFilter = false;
                                if (denNgay.HasValue && (!hv.NgaySinh.HasValue || hv.NgaySinh.Value.Date > denNgay.Value.Date)) passDateFilter = false;
                                if (passDateFilter) danhSachHocVien.Add(hv);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi truy vấn: {ex.Message}"); }
            return danhSachHocVien;
        }
        public bool AddHocVien(HocVien hv)
        {
            try { using (var conn = new SqliteConnection(_connectionString)) { conn.Open(); string sql = @"INSERT INTO HocVien (MaHV, HoTen, NgaySinh, GioiTinh, Email, SDT) VALUES (@MaHV, @HoTen, @NgaySinh, @GioiTinh, @Email, @SDT)"; using (var cmd = new SqliteCommand(sql, conn)) { cmd.Parameters.AddWithValue("@MaHV", hv.MaHV); cmd.Parameters.AddWithValue("@HoTen", hv.HoTen); cmd.Parameters.AddWithValue("@NgaySinh", hv.NgaySinh.HasValue ? hv.NgaySinh.Value.ToString("dd-MM-yyyy") : ""); cmd.Parameters.AddWithValue("@GioiTinh", hv.GioiTinh); cmd.Parameters.AddWithValue("@Email", hv.Email); cmd.Parameters.AddWithValue("@SDT", hv.SDT); return cmd.ExecuteNonQuery() > 0; } } } catch (Exception ex) { MessageBox.Show("Lỗi thêm học viên: " + ex.Message); return false; }
        }
        // --- HÀM CẬP NHẬT MỚI ---
        public bool UpdateHocVien(HocVien hv)
        {
            try { using (var conn = new SqliteConnection(_connectionString)) { conn.Open(); string sql = @"UPDATE HocVien SET HoTen = @HoTen, NgaySinh = @NgaySinh, GioiTinh = @GioiTinh, Email = @Email, SDT = @SDT WHERE MaHV = @MaHV"; using (var cmd = new SqliteCommand(sql, conn)) { cmd.Parameters.AddWithValue("@MaHV", hv.MaHV); cmd.Parameters.AddWithValue("@HoTen", hv.HoTen); cmd.Parameters.AddWithValue("@NgaySinh", hv.NgaySinh.HasValue ? hv.NgaySinh.Value.ToString("dd-MM-yyyy") : ""); cmd.Parameters.AddWithValue("@GioiTinh", hv.GioiTinh); cmd.Parameters.AddWithValue("@Email", hv.Email); cmd.Parameters.AddWithValue("@SDT", hv.SDT); return cmd.ExecuteNonQuery() > 0; } } } catch (Exception ex) { MessageBox.Show("Lỗi cập nhật: " + ex.Message); return false; }
        }
        public bool CheckMaHVExists(string maHV) { using (var conn = new SqliteConnection(_connectionString)) { conn.Open(); string sql = "SELECT COUNT(*) FROM HocVien WHERE MaHV = @MaHV"; using (var cmd = new SqliteCommand(sql, conn)) { cmd.Parameters.AddWithValue("@MaHV", maHV); long count = (long)cmd.ExecuteScalar(); return count > 0; } } }
        public bool DeleteHocVien(string maHV) { try { using (var conn = new SqliteConnection(_connectionString)) { conn.Open(); string sql = "DELETE FROM HocVien WHERE MaHV = @MaHV"; using (var cmd = new SqliteCommand(sql, conn)) { cmd.Parameters.AddWithValue("@MaHV", maHV); return cmd.ExecuteNonQuery() > 0; } } } catch (Exception ex) { MessageBox.Show("Lỗi xóa học viên: " + ex.Message); return false; } }
    }
}