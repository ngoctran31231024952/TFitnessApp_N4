using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using System.Globalization;
using TFitnessApp.Windows; // Để gọi ThemHocVienWindow

namespace TFitnessApp
{
    /// <summary>
    /// Interaction logic for HocVienPage.xaml
    /// </summary>
    public partial class HocVienPage : Page
    {
        private HocVienRepository _hocVienRepository;

        // Các biến lưu trạng thái lọc
        private string _currentFilterGioiTinh = "Tất cả";
        private DateTime? _filterTuNgay = null;
        private DateTime? _filterDenNgay = null;
        private string _filterNgayLabel = "Tất cả ngày";

        public HocVienPage()
        {
            InitializeComponent();
            _hocVienRepository = new HocVienRepository();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadHocVienData();

            // Cập nhật tiêu đề trang chính nếu cần
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                // Giả sử MainWindow có public TextBlock PageTitle
                // mainWindow.PageTitle.Text = "Quản lý Học viên"; 
            }
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
                List<HocVien> ketQua = _hocVienRepository.FindHocVien(keyword, _currentFilterGioiTinh, _filterTuNgay, _filterDenNgay);
                HocVienDataGrid.ItemsSource = ketQua;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tìm kiếm: {ex.Message}");
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            PerformSearch();
        }

        private void HocVienDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // ==============================================================
        // 🟢 KHẮC PHỤC LỖI: ĐÂY LÀ ĐOẠN CODE BẠN ĐANG THIẾU
        // ==============================================================

        // Sự kiện click nút "Thêm"
        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            // Khởi tạo cửa sổ thêm mới
            ThemHocVienWindow addWindow = new ThemHocVienWindow();

            // Hiển thị cửa sổ dưới dạng Dialog (người dùng phải đóng nó mới quay lại được)
            addWindow.ShowDialog();

            // Sau khi cửa sổ đóng, kiểm tra xem có thêm thành công không
            if (addWindow.IsSuccess)
            {
                // Nếu thành công, tải lại danh sách để cập nhật dữ liệu mới
                LoadHocVienData();
            }
        }

        // Sự kiện click nút "Xóa"
        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            // Lấy danh sách hiện tại
            var items = HocVienDataGrid.ItemsSource as List<HocVien>;
            if (items == null) return;

            // Lọc ra những người được tích chọn (IsSelected = true)
            var itemsToDelete = items.Where(x => x.IsSelected).ToList();

            if (itemsToDelete.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất một học viên để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa {itemsToDelete.Count} học viên đã chọn?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                int deleteCount = 0;
                foreach (var item in itemsToDelete)
                {
                    if (_hocVienRepository.DeleteHocVien(item.MaHV))
                    {
                        deleteCount++;
                    }
                }

                if (deleteCount > 0)
                {
                    MessageBox.Show($"Đã xóa thành công {deleteCount} học viên.", "Thông báo");
                    LoadHocVienData(); // Tải lại bảng sau khi xóa
                }
                else
                {
                    MessageBox.Show("Xóa thất bại, vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        // ==============================================================


        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu filterMenu = new ContextMenu();

            MenuItem headerGioiTinh = new MenuItem { Header = "GIỚI TÍNH", IsEnabled = false, FontWeight = FontWeights.Bold };
            MenuItem itemTatCaGT = new MenuItem { Header = "Tất cả", IsChecked = (_currentFilterGioiTinh == "Tất cả") };
            itemTatCaGT.Click += (s, args) => ApplyFilterGioiTinh("Tất cả");
            MenuItem itemNam = new MenuItem { Header = "Nam", IsChecked = (_currentFilterGioiTinh == "Nam") };
            itemNam.Click += (s, args) => ApplyFilterGioiTinh("Nam");
            MenuItem itemNu = new MenuItem { Header = "Nữ", IsChecked = (_currentFilterGioiTinh == "Nữ") };
            itemNu.Click += (s, args) => ApplyFilterGioiTinh("Nữ");

            filterMenu.Items.Add(headerGioiTinh);
            filterMenu.Items.Add(itemTatCaGT);
            filterMenu.Items.Add(itemNam);
            filterMenu.Items.Add(itemNu);
            filterMenu.Items.Add(new Separator());

            MenuItem headerNgaySinh = new MenuItem { Header = "NGÀY SINH", IsEnabled = false, FontWeight = FontWeights.Bold };
            MenuItem itemTatCaNgay = new MenuItem { Header = "Tất cả ngày", IsChecked = (_filterNgayLabel == "Tất cả ngày") };
            itemTatCaNgay.Click += (s, args) => ApplyFilterNgay("Tất cả ngày", null, null);

            MenuItem itemChonNam = new MenuItem { Header = "Chọn năm sinh..." };
            int currentYear = DateTime.Now.Year;
            for (int year = 1980; year <= currentYear - 5; year++)
            {
                MenuItem yearItem = new MenuItem { Header = $"Năm {year}", IsChecked = (_filterNgayLabel == $"Năm {year}") };
                int selectedYear = year;
                yearItem.Click += (s, args) => ApplyFilterNgay($"Năm {selectedYear}", new DateTime(selectedYear, 1, 1), new DateTime(selectedYear, 12, 31));
                itemChonNam.Items.Add(yearItem);
            }

            filterMenu.Items.Add(headerNgaySinh);
            filterMenu.Items.Add(itemTatCaNgay);
            filterMenu.Items.Add(itemChonNam);

            if (sender is Button btn)
            {
                btn.ContextMenu = filterMenu;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void ApplyFilterGioiTinh(string gioiTinh)
        {
            _currentFilterGioiTinh = gioiTinh;
            PerformSearch();
        }

        private void ApplyFilterNgay(string label, DateTime? tu, DateTime? den)
        {
            _filterNgayLabel = label;
            _filterTuNgay = tu;
            _filterDenNgay = den;
            PerformSearch();
        }
    }

    // ==========================================
    // 1. MODEL HOC VIEN
    // ==========================================
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

    // ==========================================
    // 2. REPOSITORY HOC VIEN
    // ==========================================
    public class HocVienRepository
    {
        private readonly string _connectionString;

        public HocVienRepository()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            if (!File.Exists(dbPath))
            {
                dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TFitness.db");
            }
            _connectionString = $"Data Source={dbPath};";
        }

        public List<HocVien> FindHocVien(string keyword, string gioiTinh = "Tất cả", DateTime? tuNgay = null, DateTime? denNgay = null)
        {
            List<HocVien> danhSachHocVien = new List<HocVien>();
            string dbPathCheck = _connectionString.Replace("Data Source=", "").Replace(";", "");
            if (!File.Exists(dbPathCheck)) return danhSachHocVien;

            string sql = @"SELECT MaHV, HoTen, NgaySinh, GioiTinh, Email, SDT 
                           FROM HocVien 
                           WHERE (MaHV LIKE @keyword 
                              OR HoTen LIKE @keyword 
                              OR SDT LIKE @keyword 
                              OR Email LIKE @keyword)";

            if (gioiTinh != "Tất cả" && !string.IsNullOrEmpty(gioiTinh))
            {
                sql += " AND GioiTinh = @gioiTinh";
            }

            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                        if (gioiTinh != "Tất cả" && !string.IsNullOrEmpty(gioiTinh))
                        {
                            command.Parameters.AddWithValue("@gioiTinh", gioiTinh);
                        }

                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime? ngaySinh = null;
                                string ngaySinhStr = reader["NgaySinh"]?.ToString();

                                if (!string.IsNullOrWhiteSpace(ngaySinhStr))
                                {
                                    string[] formats = { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "d-M-yyyy", "d/M/yyyy", "M/d/yyyy" };
                                    if (DateTime.TryParseExact(ngaySinhStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ns))
                                    {
                                        if (ns.Year > 1900) ngaySinh = ns;
                                    }
                                }

                                var hv = new HocVien
                                {
                                    MaHV = reader["MaHV"].ToString(),
                                    HoTen = reader["HoTen"].ToString(),
                                    NgaySinh = ngaySinh,
                                    GioiTinh = reader["GioiTinh"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    SDT = reader["SDT"].ToString(),
                                    DiaChi = "",
                                    IsSelected = false
                                };

                                bool passDateFilter = true;
                                if (tuNgay.HasValue && (!hv.NgaySinh.HasValue || hv.NgaySinh.Value.Date < tuNgay.Value.Date)) passDateFilter = false;
                                if (denNgay.HasValue && (!hv.NgaySinh.HasValue || hv.NgaySinh.Value.Date > denNgay.Value.Date)) passDateFilter = false;

                                if (passDateFilter) danhSachHocVien.Add(hv);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi truy vấn: {ex.Message}");
            }

            return danhSachHocVien;
        }

        public List<HocVien> GetAllHocVien() => FindHocVien("");

        public bool AddHocVien(HocVien hv)
        {
            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO HocVien (MaHV, HoTen, NgaySinh, GioiTinh, Email, SDT) 
                                   VALUES (@MaHV, @HoTen, @NgaySinh, @GioiTinh, @Email, @SDT)";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaHV", hv.MaHV);
                        cmd.Parameters.AddWithValue("@HoTen", hv.HoTen);
                        cmd.Parameters.AddWithValue("@NgaySinh", hv.NgaySinh.HasValue ? hv.NgaySinh.Value.ToString("dd-MM-yyyy") : "");
                        cmd.Parameters.AddWithValue("@GioiTinh", hv.GioiTinh);
                        cmd.Parameters.AddWithValue("@Email", hv.Email);
                        cmd.Parameters.AddWithValue("@SDT", hv.SDT);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thêm học viên: " + ex.Message);
                return false;
            }
        }

        public bool CheckMaHVExists(string maHV)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM HocVien WHERE MaHV = @MaHV";
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MaHV", maHV);
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public bool DeleteHocVien(string maHV)
        {
            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM HocVien WHERE MaHV = @MaHV";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaHV", maHV);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xóa học viên: " + ex.Message);
                return false;
            }
        }
    }
}