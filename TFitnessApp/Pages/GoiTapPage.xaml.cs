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
using System.ComponentModel; // Cần thiết cho INotifyPropertyChanged
using TFitnessApp.Windows;

namespace TFitnessApp
{
    public partial class GoiTapPage : Page
    {
        private GoiTapRepository _repository;

        // Biến phân trang
        private List<GoiTap> _danhSachGoc = new List<GoiTap>();
        private ObservableCollection<GoiTap> _danhSachHienThi = new ObservableCollection<GoiTap>();

        private int _trangHienTai = 1;
        private int _soBanGhiMoiTrang = 50;
        private int _tongSoTrang = 1;
        private int _tongSoBanGhi = 0;

        // Các biến lưu trạng thái lọc
        private double? _filterMinPrice = null;
        private double? _filterMaxPrice = null;
        private string _filterPT = "Tất cả";
        private int? _filterMonths = null;
        private string _filterSpecial = "Tất cả";

        public GoiTapPage()
        {
            InitializeComponent();
            _repository = new GoiTapRepository();

            // Kiểm tra null trước khi gán
            if (cboSoBanGhi != null && cboSoBanGhi.Items.Count > 2)
                cboSoBanGhi.SelectedIndex = 2; // Mặc định 50
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            try
            {
                string keyword = txtSearch.Text.Trim();

                // Gọi hàm tìm kiếm nâng cao
                _danhSachGoc = _repository.FindGoiTapAdvanced(
                    keyword,
                    _filterMinPrice,
                    _filterMaxPrice,
                    _filterPT,
                    _filterMonths,
                    _filterSpecial
                );

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

            var dataPage = _danhSachGoc
                .Skip((_trangHienTai - 1) * _soBanGhiMoiTrang)
                .Take(_soBanGhiMoiTrang)
                .ToList();

            _danhSachHienThi.Clear();
            foreach (var item in dataPage)
            {
                _danhSachHienThi.Add(item);
            }

            if (GoiTapDataGrid != null)
            {
                GoiTapDataGrid.ItemsSource = _danhSachHienThi;
            }

            UpdatePaginationUI();
        }

        private void UpdatePaginationUI()
        {
            if (txtThongTinPhanTrang == null || btnTrangTruoc == null || btnTrangSau == null) return;

            int start = (_tongSoBanGhi == 0) ? 0 : (_trangHienTai - 1) * _soBanGhiMoiTrang + 1;
            int end = Math.Min(_trangHienTai * _soBanGhiMoiTrang, _tongSoBanGhi);

            txtThongTinPhanTrang.Text = $"Hiển thị {start}-{end} của {_tongSoBanGhi} gói tập";

            btnTrangTruoc.IsEnabled = _trangHienTai > 1;
            btnTrangSau.IsEnabled = _trangHienTai < _tongSoTrang;
        }

        // --- EVENTS ---

        // Sự kiện Chọn Tất Cả
        private void SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _danhSachHienThi)
            {
                item.IsSelected = true;
            }
        }

        private void SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _danhSachHienThi)
            {
                item.IsSelected = false;
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) { PerformSearch(); }

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

        private void btnTrangTruoc_Click(object sender, RoutedEventArgs e) { if (_trangHienTai > 1) { _trangHienTai--; HienThiDuLieuPhanTrang(); } }
        private void btnTrangSau_Click(object sender, RoutedEventArgs e) { if (_trangHienTai < _tongSoTrang) { _trangHienTai++; HienThiDuLieuPhanTrang(); } }

        private void GoiTapDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // --- ACTIONS ---

        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            ThemGoiTapWindow addWindow = new ThemGoiTapWindow();
            addWindow.ShowDialog();
            if (addWindow.IsSuccess) LoadData();
        }

        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            var itemsToDelete = _danhSachHienThi.Where(x => x.IsSelected).ToList();
            if (itemsToDelete.Count == 0) { MessageBox.Show("Vui lòng chọn gói tập để xóa!", "Thông báo"); return; }

            if (MessageBox.Show($"Xóa {itemsToDelete.Count} gói tập đã chọn?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                foreach (var item in itemsToDelete) _repository.DeleteGoiTap(item.MaGoi);
                LoadData();
                MessageBox.Show("Đã xóa thành công!", "Thông báo");
            }
        }

        // --- SỰ KIỆN LỌC ---
        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            LocGoiTapWindow filterWindow = new LocGoiTapWindow();
            filterWindow.ShowDialog();

            if (filterWindow.IsApply && filterWindow.FilterData != null)
            {
                var data = filterWindow.FilterData;
                _filterMinPrice = data.MinPrice;
                _filterMaxPrice = data.MaxPrice;
                _filterPT = data.PTOption;
                _filterMonths = data.Months;
                _filterSpecial = data.SpecialService;

                PerformSearch();
            }
        }

        // --- NÚT LÀM MỚI (MỚI THÊM) ---
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            _filterMinPrice = null;
            _filterMaxPrice = null;
            _filterPT = "Tất cả";
            _filterMonths = null;
            _filterSpecial = "Tất cả";

            _trangHienTai = 1;
            LoadData();
        }

        // --- Row Actions ---

        private void BtnXem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GoiTap gt)
            {
                XemThongTinGoiTapWindow viewWindow = new XemThongTinGoiTapWindow(gt);
                viewWindow.ShowDialog();
            }
        }

        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GoiTap gt)
            {
                ThemGoiTapWindow editWindow = new ThemGoiTapWindow(gt);
                editWindow.ShowDialog();
                if (editWindow.IsSuccess) LoadData();
            }
        }

        private void BtnXoaRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GoiTap gt)
            {
                if (MessageBox.Show($"Xóa gói tập: {gt.TenGoi}?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _repository.DeleteGoiTap(gt.MaGoi);
                    LoadData();
                }
            }
        }
    }

    // ==========================================
    // 1. MODEL GOI TAP (CẬP NHẬT INotifyPropertyChanged)
    // ==========================================
    public class GoiTap : INotifyPropertyChanged
    {
        public string MaGoi { get; set; }
        public string TenGoi { get; set; }
        public int ThoiHan { get; set; }
        public double GiaNiemYet { get; set; }
        public int SoBuoiPT { get; set; }
        public string DichVuDacBiet { get; set; }
        public string TrangThai { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public string GiaNiemYetFormatted => GiaNiemYet.ToString("N0", CultureInfo.InvariantCulture);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ==========================================
    // 2. REPOSITORY GOI TAP
    // ==========================================
    public class GoiTapRepository
    {
        private readonly string _connectionString;

        public GoiTapRepository()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            if (!File.Exists(dbPath))
            {
                dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TFitness.db");
            }
            _connectionString = $"Data Source={dbPath};";
        }

        // --- HÀM LỌC NÂNG CAO ---
        public List<GoiTap> FindGoiTapAdvanced(string keyword, double? minPrice, double? maxPrice, string ptOption, int? months, string specialService)
        {
            List<GoiTap> list = new List<GoiTap>();
            string sql = "SELECT * FROM GoiTap WHERE (MaGoi LIKE @k OR TenGoi LIKE @k)";

            // 1. Lọc theo Giá
            if (minPrice.HasValue) sql += " AND GiaNiemYet >= @minP";
            if (maxPrice.HasValue) sql += " AND GiaNiemYet <= @maxP";

            // 2. Lọc theo PT (Có/Không)
            if (!string.IsNullOrEmpty(ptOption))
            {
                if (ptOption == "Có PT") sql += " AND SoBuoiPT > 0";
                else if (ptOption == "Không PT") sql += " AND SoBuoiPT = 0";
            }

            // 3. Lọc theo Thời hạn (tháng)
            if (months.HasValue && months.Value > 0)
            {
                sql += " AND ThoiHan = @months";
            }

            // 4. Lọc Dịch vụ đặc biệt (Có/Không)
            if (!string.IsNullOrEmpty(specialService) && specialService != "Tất cả")
            {
                // Trong DB lưu là "Có"/"Không" hoặc "có"/"không"
                // Sử dụng LIKE để tìm không phân biệt hoa thường tương đối
                sql += " AND DichVuDacBiet LIKE @special";
            }

            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@k", $"%{keyword}%");

                        if (minPrice.HasValue) cmd.Parameters.AddWithValue("@minP", minPrice.Value);
                        if (maxPrice.HasValue) cmd.Parameters.AddWithValue("@maxP", maxPrice.Value);
                        if (months.HasValue) cmd.Parameters.AddWithValue("@months", months.Value);
                        if (!string.IsNullOrEmpty(specialService) && specialService != "Tất cả")
                            cmd.Parameters.AddWithValue("@special", specialService);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new GoiTap
                                {
                                    MaGoi = reader["MaGoi"].ToString(),
                                    TenGoi = reader["TenGoi"].ToString(),
                                    ThoiHan = Convert.ToInt32(reader["ThoiHan"]),
                                    GiaNiemYet = Convert.ToDouble(reader["GiaNiemYet"]),
                                    SoBuoiPT = Convert.ToInt32(reader["SoBuoiPT"]),
                                    DichVuDacBiet = reader["DichVuDacBiet"].ToString(),
                                    TrangThai = reader["TrangThai"].ToString(),
                                    IsSelected = false
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi lọc Gói tập: " + ex.Message); }
            return list;
        }

        // Hàm tìm kiếm đơn giản (giữ lại để tương thích nếu cần)
        public List<GoiTap> FindGoiTap(string keyword)
        {
            return FindGoiTapAdvanced(keyword, null, null, null, null, null);
        }

        public string GenerateNewMaGoi()
        {
            string newMa = "GT001";
            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT MaGoi FROM GoiTap ORDER BY length(MaGoi) DESC, MaGoi DESC LIMIT 1";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        var res = cmd.ExecuteScalar();
                    }
                }
            }
            catch { }
            return ""; // Để trống
        }

        public bool CheckMaGoiExists(string maGoi)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM GoiTap WHERE MaGoi = @id";
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", maGoi);
                    return (long)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public bool AddGoiTap(GoiTap gt)
        {
            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO GoiTap (MaGoi, TenGoi, ThoiHan, GiaNiemYet, SoBuoiPT, DichVuDacBiet, TrangThai) 
                                   VALUES (@MaGoi, @TenGoi, @ThoiHan, @Gia, @SoBuoi, @DV, @TrangThai)";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaGoi", gt.MaGoi);
                        cmd.Parameters.AddWithValue("@TenGoi", gt.TenGoi);
                        cmd.Parameters.AddWithValue("@ThoiHan", gt.ThoiHan);
                        cmd.Parameters.AddWithValue("@Gia", gt.GiaNiemYet);
                        cmd.Parameters.AddWithValue("@SoBuoi", gt.SoBuoiPT);
                        cmd.Parameters.AddWithValue("@DV", gt.DichVuDacBiet);
                        cmd.Parameters.AddWithValue("@TrangThai", gt.TrangThai);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi thêm: " + ex.Message); return false; }
        }

        public bool UpdateGoiTap(GoiTap gt)
        {
            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"UPDATE GoiTap SET TenGoi=@TenGoi, ThoiHan=@ThoiHan, GiaNiemYet=@Gia, SoBuoiPT=@SoBuoi, DichVuDacBiet=@DV, TrangThai=@TrangThai 
                                   WHERE MaGoi=@MaGoi";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaGoi", gt.MaGoi);
                        cmd.Parameters.AddWithValue("@TenGoi", gt.TenGoi);
                        cmd.Parameters.AddWithValue("@ThoiHan", gt.ThoiHan);
                        cmd.Parameters.AddWithValue("@Gia", gt.GiaNiemYet);
                        cmd.Parameters.AddWithValue("@SoBuoi", gt.SoBuoiPT);
                        cmd.Parameters.AddWithValue("@DV", gt.DichVuDacBiet);
                        cmd.Parameters.AddWithValue("@TrangThai", gt.TrangThai);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi cập nhật: " + ex.Message); return false; }
        }

        public bool DeleteGoiTap(string maGoi)
        {
            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM GoiTap WHERE MaGoi = @id";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", maGoi);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi xóa: " + ex.Message); return false; }
        }
    }
}