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
using System.ComponentModel; // Cần thêm namespace này
using TFitnessApp.Windows;
using TFitnessApp.Database;

namespace TFitnessApp
{
    public partial class PTPage : Page
    {
        private PTRepository _ptRepository;

        // Biến phân trang
        private List<PT> _danhSachGoc = new List<PT>();
        private ObservableCollection<PT> _danhSachHienThi = new ObservableCollection<PT>();

        private int _trangHienTai = 1;
        private int _soBanGhiMoiTrang = 50;
        private int _tongSoTrang = 1;
        private int _tongSoBanGhi = 0;

        // Biến lọc
        private string _filterGioiTinh = "Tất cả";
        private string _filterChiNhanh = "Tất cả";
        private string _filterChiNhanhLabel = "Tất cả chi nhánh";

        public PTPage()
        {
            InitializeComponent();
            _ptRepository = new PTRepository();

            if (cboSoBanGhi != null && cboSoBanGhi.Items.Count > 2)
                cboSoBanGhi.SelectedIndex = 2;
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
                // Gọi hàm tìm kiếm có hỗ trợ lọc
                _danhSachGoc = _ptRepository.FindPT(keyword, _filterGioiTinh, _filterChiNhanh);

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

            if (PTDataGrid != null)
            {
                PTDataGrid.ItemsSource = _danhSachHienThi;
            }

            UpdatePaginationUI();
        }

        private void UpdatePaginationUI()
        {
            if (txtThongTinPhanTrang == null || txtTrangHienTai == null || txtTongSoTrang == null ||
                btnTrangDau == null || btnTrangTruoc == null || btnTrangSau == null || btnTrangCuoi == null)
                return;

            int start = (_tongSoBanGhi == 0) ? 0 : (_trangHienTai - 1) * _soBanGhiMoiTrang + 1;
            int end = Math.Min(_trangHienTai * _soBanGhiMoiTrang, _tongSoBanGhi);

            txtThongTinPhanTrang.Text = $"Hiển thị {start}-{end} của {_tongSoBanGhi} PT";
            txtTrangHienTai.Text = _trangHienTai.ToString();
            txtTongSoTrang.Text = _tongSoTrang.ToString();

            btnTrangDau.IsEnabled = _trangHienTai > 1;
            btnTrangTruoc.IsEnabled = _trangHienTai > 1;
            btnTrangSau.IsEnabled = _trangHienTai < _tongSoTrang;
            btnTrangCuoi.IsEnabled = _trangHienTai < _tongSoTrang;
        }

        // --- EVENTS ---

        // SỰ KIỆN CHỌN TẤT CẢ
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

        private void btnTrangDau_Click(object sender, RoutedEventArgs e) { _trangHienTai = 1; HienThiDuLieuPhanTrang(); }
        private void btnTrangTruoc_Click(object sender, RoutedEventArgs e) { if (_trangHienTai > 1) { _trangHienTai--; HienThiDuLieuPhanTrang(); } }
        private void btnTrangSau_Click(object sender, RoutedEventArgs e) { if (_trangHienTai < _tongSoTrang) { _trangHienTai++; HienThiDuLieuPhanTrang(); } }
        private void btnTrangCuoi_Click(object sender, RoutedEventArgs e) { _trangHienTai = _tongSoTrang; HienThiDuLieuPhanTrang(); }

        private void txtTrangHienTai_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && int.TryParse(txtTrangHienTai.Text, out int p) && p >= 1 && p <= _tongSoTrang)
            { _trangHienTai = p; HienThiDuLieuPhanTrang(); }
            else { txtTrangHienTai.Text = _trangHienTai.ToString(); }
        }

        private void PTDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // --- ACTIONS ---

        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            ThemPTWindow addWindow = new ThemPTWindow();
            addWindow.ShowDialog();
            if (addWindow.IsSuccess) LoadData();
        }

        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            var itemsToDelete = _danhSachHienThi.Where(x => x.IsSelected).ToList();
            if (itemsToDelete.Count == 0) { MessageBox.Show("Vui lòng chọn PT để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (MessageBox.Show($"Xóa {itemsToDelete.Count} PT đã chọn?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var item in itemsToDelete) _ptRepository.DeletePT(item.MaPT);
                LoadData();
                MessageBox.Show("Đã xóa thành công!", "Thông báo");
            }
        }

        // --- NÚT LÀM MỚI (MỚI) ---
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            _filterGioiTinh = "Tất cả";
            _filterChiNhanh = "Tất cả";
            _trangHienTai = 1;
            LoadData();
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu filterMenu = new ContextMenu();

            MenuItem headerGioiTinh = new MenuItem { Header = "GIỚI TÍNH", IsEnabled = false, FontWeight = FontWeights.Bold };
            MenuItem itemTatCaGT = new MenuItem { Header = "Tất cả", IsChecked = (_filterGioiTinh == "Tất cả") };
            itemTatCaGT.Click += (s, args) => ApplyFilterGioiTinh("Tất cả");
            MenuItem itemNam = new MenuItem { Header = "Nam", IsChecked = (_filterGioiTinh == "Nam") };
            itemNam.Click += (s, args) => ApplyFilterGioiTinh("Nam");
            MenuItem itemNu = new MenuItem { Header = "Nữ", IsChecked = (_filterGioiTinh == "Nữ") };
            itemNu.Click += (s, args) => ApplyFilterGioiTinh("Nữ");

            filterMenu.Items.Add(headerGioiTinh);
            filterMenu.Items.Add(itemTatCaGT);
            filterMenu.Items.Add(itemNam);
            filterMenu.Items.Add(itemNu);
            filterMenu.Items.Add(new Separator());

            MenuItem headerChiNhanh = new MenuItem { Header = "CHI NHÁNH", IsEnabled = false, FontWeight = FontWeights.Bold };
            filterMenu.Items.Add(headerChiNhanh);

            MenuItem itemTatCaCN = new MenuItem { Header = "Tất cả chi nhánh", IsChecked = (_filterChiNhanh == "Tất cả") };
            itemTatCaCN.Click += (s, args) => ApplyFilterChiNhanh("Tất cả", "Tất cả chi nhánh");
            filterMenu.Items.Add(itemTatCaCN);

            var listCN = _ptRepository.GetAllChiNhanh();
            foreach (var cn in listCN)
            {
                MenuItem itemCN = new MenuItem { Header = cn.TenCN, IsChecked = (_filterChiNhanh == cn.MaCN) };
                string maCN = cn.MaCN; string tenCN = cn.TenCN;
                itemCN.Click += (s, args) => ApplyFilterChiNhanh(maCN, tenCN);
                filterMenu.Items.Add(itemCN);
            }

            if (sender is Button btn) { btn.ContextMenu = filterMenu; btn.ContextMenu.IsOpen = true; }
        }

        private void ApplyFilterGioiTinh(string val) { _filterGioiTinh = val; PerformSearch(); }
        private void ApplyFilterChiNhanh(string maCN, string tenCN) { _filterChiNhanh = maCN; _filterChiNhanhLabel = tenCN; PerformSearch(); }

        // --- Row Actions ---
        private void BtnXem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PT pt) { XemThongTinPTWindow viewWindow = new XemThongTinPTWindow(pt); viewWindow.ShowDialog(); }
        }
        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PT pt) { ThemPTWindow editWindow = new ThemPTWindow(pt); editWindow.ShowDialog(); if (editWindow.IsSuccess) LoadData(); }
        }
        private void BtnXoaRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PT pt && MessageBox.Show($"Xóa PT {pt.HoTen}?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { _ptRepository.DeletePT(pt.MaPT); LoadData(); }
        }
    }

    // ==========================================
    // 1. MODEL PT (CẬP NHẬT INotifyPropertyChanged)
    // ==========================================
    public class PT : INotifyPropertyChanged
    {
        public string MaPT { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string SDT { get; set; }
        public string GioiTinh { get; set; }
        public string MaCN { get; set; }
        public string TenCN { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ChiNhanhSimple { public string MaCN { get; set; } public string TenCN { get; set; } public override string ToString() { return TenCN; } }

    // ==========================================
    // 2. REPOSITORY PT
    // ==========================================
    public class PTRepository
    {
        private string _ChuoiKetNoi;
        private readonly TruyCapDB _dbAccess;

        public PTRepository()
        {
            // Khởi tạo đối tượng DbAccess
            _dbAccess = new TruyCapDB();
            // Lấy chuỗi kết nối
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;
        }
        public List<PT> FindPT(string keyword, string gioiTinh = "Tất cả", string maCN = "Tất cả")
        {
            List<PT> list = new List<PT>();
            string sql = @"
                SELECT p.MaPT, p.HoTen, p.Email, p.SDT, p.GioiTinh, p.MaCN, c.TenCN
                FROM PT p
                LEFT JOIN ChiNhanh c ON p.MaCN = c.MaCN
                WHERE (p.MaPT LIKE @k OR p.HoTen LIKE @k OR p.SDT LIKE @k OR p.Email LIKE @k)";

            if (gioiTinh != "Tất cả" && !string.IsNullOrEmpty(gioiTinh)) sql += " AND p.GioiTinh = @gioiTinh";
            if (maCN != "Tất cả" && !string.IsNullOrEmpty(maCN)) sql += " AND p.MaCN = @maCN";

            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@k", $"%{keyword}%");
                        if (gioiTinh != "Tất cả") cmd.Parameters.AddWithValue("@gioiTinh", gioiTinh);
                        if (maCN != "Tất cả") cmd.Parameters.AddWithValue("@maCN", maCN);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new PT
                                {
                                    MaPT = reader["MaPT"].ToString(),
                                    HoTen = reader["HoTen"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    SDT = reader["SDT"].ToString(),
                                    GioiTinh = reader["GioiTinh"] != DBNull.Value ? reader["GioiTinh"].ToString() : "",
                                    MaCN = reader["MaCN"] != DBNull.Value ? reader["MaCN"].ToString() : "",
                                    TenCN = reader["TenCN"] != DBNull.Value ? reader["TenCN"].ToString() : "Chưa phân công",
                                    IsSelected = false
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }
        public string GenerateNewMaPT() { string newMa = "PT001"; try { using (SqliteConnection conn = TruyCapDB.TaoKetNoi()) { conn.Open(); string sql = "SELECT MaPT FROM PT ORDER BY length(MaPT) DESC, MaPT DESC LIMIT 1"; using (var cmd = new SqliteCommand(sql, conn)) { var result = cmd.ExecuteScalar(); if (result != null) { string maxMa = result.ToString(); if (maxMa.Length > 2 && int.TryParse(maxMa.Substring(2), out int currentNum)) newMa = $"PT{(currentNum + 1).ToString("D3")}"; } } } } catch { } return newMa; }
        public List<ChiNhanhSimple> GetAllChiNhanh() { List<ChiNhanhSimple> list = new List<ChiNhanhSimple>(); try { using (SqliteConnection conn = TruyCapDB.TaoKetNoi()) { conn.Open(); using (var cmd = new SqliteCommand("SELECT MaCN, TenCN FROM ChiNhanh", conn)) using (var reader = cmd.ExecuteReader()) { while (reader.Read()) list.Add(new ChiNhanhSimple { MaCN = reader["MaCN"].ToString(), TenCN = reader["TenCN"].ToString() }); } } } catch { } return list; }
        public bool CheckMaPTExists(string maPT) { using (SqliteConnection conn = TruyCapDB.TaoKetNoi()) { conn.Open(); using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM PT WHERE MaPT = @id", conn)) { cmd.Parameters.AddWithValue("@id", maPT); return (long)cmd.ExecuteScalar() > 0; } } }
        public bool AddPT(PT pt) { try { using (SqliteConnection conn = TruyCapDB.TaoKetNoi()) { conn.Open(); string sql = @"INSERT INTO PT (MaPT, HoTen, Email, SDT, GioiTinh, MaCN) VALUES (@MaPT, @HoTen, @Email, @SDT, @GioiTinh, @MaCN)"; using (var cmd = new SqliteCommand(sql, conn)) { cmd.Parameters.AddWithValue("@MaPT", pt.MaPT); cmd.Parameters.AddWithValue("@HoTen", pt.HoTen); cmd.Parameters.AddWithValue("@Email", pt.Email); cmd.Parameters.AddWithValue("@SDT", pt.SDT); cmd.Parameters.AddWithValue("@GioiTinh", pt.GioiTinh); cmd.Parameters.AddWithValue("@MaCN", pt.MaCN); return cmd.ExecuteNonQuery() > 0; } } } catch { return false; } }
        public bool UpdatePT(PT pt) { try { using (SqliteConnection conn = TruyCapDB.TaoKetNoi()) { conn.Open(); string sql = @"UPDATE PT SET HoTen=@HoTen, Email=@Email, SDT=@SDT, GioiTinh=@GioiTinh, MaCN=@MaCN WHERE MaPT=@MaPT"; using (var cmd = new SqliteCommand(sql, conn)) { cmd.Parameters.AddWithValue("@MaPT", pt.MaPT); cmd.Parameters.AddWithValue("@HoTen", pt.HoTen); cmd.Parameters.AddWithValue("@Email", pt.Email); cmd.Parameters.AddWithValue("@SDT", pt.SDT); cmd.Parameters.AddWithValue("@GioiTinh", pt.GioiTinh); cmd.Parameters.AddWithValue("@MaCN", pt.MaCN); return cmd.ExecuteNonQuery() > 0; } } } catch { return false; } }
        public bool DeletePT(string maPT) { try { using (SqliteConnection conn = TruyCapDB.TaoKetNoi()) { conn.Open(); string sql = "DELETE FROM PT WHERE MaPT = @id"; using (var cmd = new SqliteCommand(sql, conn)) { cmd.Parameters.AddWithValue("@id", maPT); return cmd.ExecuteNonQuery() > 0; } } } catch { return false; } }
    }
}