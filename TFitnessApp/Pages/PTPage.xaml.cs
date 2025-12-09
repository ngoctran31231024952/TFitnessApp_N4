using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.ComponentModel;
using TFitnessApp.Windows;
using TFitnessApp.Database;

namespace TFitnessApp
{
    /// <summary>
    /// Trang quản lý danh sách Huấn Luyện Viên (Personal Trainer - PT)
    /// </summary>
    public partial class PTPage : Page
    {
        #region Trường Dữ liệu Nội bộ (Internal Fields)
        // Repository để thực hiện các thao tác với cơ sở dữ liệu
        private PTRepository _ptRepository;

        // --- CÁC BIẾN DÙNG CHO PHÂN TRANG & HIỂN THỊ ---
        // _danhSachGoc: Lưu trữ toàn bộ dữ liệu PT tải từ DB
        private List<PT> _danhSachGoc = new List<PT>();
        // _danhSachHienThi: Dữ liệu được cắt theo trang hiện tại (Binding lên Grid)
        private ObservableCollection<PT> _danhSachHienThi = new ObservableCollection<PT>();

        private int _trangHienTai = 1;              // Trang hiện tại đang xem
        private int _soBanGhiMoiTrang = 50;         // Số dòng hiển thị trên mỗi trang
        private int _tongSoTrang = 1;               // Tổng số trang tính toán được
        private int _tongSoBanGhi = 0;              // Tổng số bản ghi tìm thấy

        // --- CÁC BIẾN LƯU TRẠNG THÁI LỌC ---
        private string _filterGioiTinh = "Tất cả";
        private string _filterChiNhanh = "Tất cả";
        // Biến này chỉ dùng để hiển thị tên chi nhánh đang lọc lên giao diện (nếu cần)
        private string _filterChiNhanhLabel = "Tất cả chi nhánh";
        #endregion

        #region Khởi tạo (Constructor & Initialization)
        public PTPage()
        {
            InitializeComponent();
            _ptRepository = new PTRepository();

            // Thiết lập mặc định hiển thị 50 dòng/trang cho ComboBox
            if (cboSoBanGhi != null && cboSoBanGhi.Items.Count > 2)
                cboSoBanGhi.SelectedIndex = 2;
        }

        // Sự kiện Loaded của trang
        private void SuKienTaiTrang(object sender, RoutedEventArgs e)
        {
            TaiDuLieu();
        }

        // Hàm trung gian để gọi quy trình tìm kiếm/tải dữ liệu
        private void TaiDuLieu()
        {
            ThucHienTimKiem();
        }
        #endregion

        #region Logic Tìm kiếm và Lọc (Search Logic)
        private void ThucHienTimKiem()
        {
            try
            {
                // 1. Lấy từ khóa tìm kiếm
                string keyword = txtSearch.Text.Trim();

                // 2. Gọi Repository để lấy dữ liệu theo từ khóa và các bộ lọc (Giới tính, Chi nhánh)
                // SỬA LỖI: FindPT -> TimKiemPT
                _danhSachGoc = _ptRepository.TimKiemPT(keyword, _filterGioiTinh, _filterChiNhanh);

                // 3. Reset về trang đầu tiên mỗi khi tìm kiếm mới
                _trangHienTai = 1;

                // 4. Phân trang và hiển thị
                HienThiDuLieuPhanTrang();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tìm kiếm: {ex.Message}");
            }
        }
        #endregion

        #region Logic Phân trang (Pagination Logic)
        private void HienThiDuLieuPhanTrang()
        {
            if (_danhSachGoc == null) return;
            // 1. Tính toán tổng số trang
            _tongSoBanGhi = _danhSachGoc.Count;
            _tongSoTrang = (int)Math.Ceiling((double)_tongSoBanGhi / _soBanGhiMoiTrang);
            if (_tongSoTrang == 0) _tongSoTrang = 1;
            // 2. Kiểm tra biên (Boundary Check)
            if (_trangHienTai > _tongSoTrang) _trangHienTai = _tongSoTrang;
            if (_trangHienTai < 1) _trangHienTai = 1;
            // 3. Cắt dữ liệu cho trang hiện tại (Skip & Take)
            var dataPage = _danhSachGoc
                .Skip((_trangHienTai - 1) * _soBanGhiMoiTrang)
                .Take(_soBanGhiMoiTrang)
                .ToList();
            // 4. Cập nhật ObservableCollection để Grid tự động refresh
            _danhSachHienThi.Clear();
            foreach (var item in dataPage)
            {
                _danhSachHienThi.Add(item);
            }
            // 5. Gán nguồn dữ liệu (nếu chưa gán)
            if (PTDataGrid != null)
            {
                PTDataGrid.ItemsSource = _danhSachHienThi;
            }
            // 6. Cập nhật trạng thái các nút điều hướng
            CapNhatGiaoDienPhanTrang();
        }

        private void CapNhatGiaoDienPhanTrang()
        {
            if (txtThongTinPhanTrang == null || txtTrangHienTai == null || txtTongSoTrang == null ||
                btnTrangDau == null || btnTrangTruoc == null || btnTrangSau == null || btnTrangCuoi == null)
                return;

            int start = (_tongSoBanGhi == 0) ? 0 : (_trangHienTai - 1) * _soBanGhiMoiTrang + 1;
            int end = Math.Min(_trangHienTai * _soBanGhiMoiTrang, _tongSoBanGhi);

            txtThongTinPhanTrang.Text = $"Hiển thị {start}-{end} của {_tongSoBanGhi} PT";
            txtTrangHienTai.Text = _trangHienTai.ToString();
            txtTongSoTrang.Text = _tongSoTrang.ToString();

            // Kiểm soát trạng thái Enable/Disable của nút điều hướng
            btnTrangDau.IsEnabled = _trangHienTai > 1;
            btnTrangTruoc.IsEnabled = _trangHienTai > 1;
            btnTrangSau.IsEnabled = _trangHienTai < _tongSoTrang;
            btnTrangCuoi.IsEnabled = _trangHienTai < _tongSoTrang;
        }
        #endregion

        #region Sự kiện Tương tác (Events)
        // Xử lý chọn tất cả checkbox
        private void ChonTatCa_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _danhSachHienThi) item.IsSelected = true;
        }

        private void ChonTatCa_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _danhSachHienThi) item.IsSelected = false;
        }

        // Tìm kiếm tức thì khi gõ phím
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) { ThucHienTimKiem(); }

        // Thay đổi số bản ghi hiển thị trên mỗi trang
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

        // Các nút điều hướng trang
        private void btnTrangDau_Click(object sender, RoutedEventArgs e) { _trangHienTai = 1; HienThiDuLieuPhanTrang(); }
        private void btnTrangTruoc_Click(object sender, RoutedEventArgs e) { if (_trangHienTai > 1) { _trangHienTai--; HienThiDuLieuPhanTrang(); } }
        private void btnTrangSau_Click(object sender, RoutedEventArgs e) { if (_trangHienTai < _tongSoTrang) { _trangHienTai++; HienThiDuLieuPhanTrang(); } }
        private void btnTrangCuoi_Click(object sender, RoutedEventArgs e) { _trangHienTai = _tongSoTrang; HienThiDuLieuPhanTrang(); }

        // Nhập số trang trực tiếp và nhấn Enter
        private void txtTrangHienTai_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && int.TryParse(txtTrangHienTai.Text, out int p) && p >= 1 && p <= _tongSoTrang)
            {
                _trangHienTai = p;
                HienThiDuLieuPhanTrang();
            }
            else
            {
                txtTrangHienTai.Text = _trangHienTai.ToString();
            }
        }

        private void PTDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        #endregion

        #region Chức năng CRUD (Thêm, Xóa, Sửa, Lọc)
        // Nút THÊM
        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            ThemPTWindow addWindow = new ThemPTWindow();
            addWindow.ShowDialog();
            if (addWindow.IsSuccess) TaiDuLieu();
        }

        // Nút XÓA NHIỀU
        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            var itemsToDelete = _danhSachHienThi.Where(x => x.IsSelected).ToList();
            if (itemsToDelete.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn PT để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Xóa {itemsToDelete.Count} PT đã chọn?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // SỬA LỖI: DeletePT -> XoaPT
                foreach (var item in itemsToDelete) _ptRepository.XoaPT(item.MaPT);
                TaiDuLieu();
                MessageBox.Show("Đã xóa thành công!", "Thông báo");
            }
        }

        // Nút LÀM MỚI
        private void BtnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            _filterGioiTinh = "Tất cả";
            _filterChiNhanh = "Tất cả";
            _trangHienTai = 1;
            TaiDuLieu();
        }

        // Nút LỌC (Sử dụng ContextMenu làm bộ lọc nhanh)
        private void BtnLoc_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu filterMenu = new ContextMenu();

            // Phần lọc Giới tính
            MenuItem headerGioiTinh = new MenuItem { Header = "GIỚI TÍNH", IsEnabled = false, FontWeight = FontWeights.Bold };
            MenuItem itemTatCaGT = new MenuItem { Header = "Tất cả", IsChecked = (_filterGioiTinh == "Tất cả") };
            itemTatCaGT.Click += (s, args) => ApDungLocGioiTinh("Tất cả");
            MenuItem itemNam = new MenuItem { Header = "Nam", IsChecked = (_filterGioiTinh == "Nam") };
            itemNam.Click += (s, args) => ApDungLocGioiTinh("Nam");
            MenuItem itemNu = new MenuItem { Header = "Nữ", IsChecked = (_filterGioiTinh == "Nữ") };
            itemNu.Click += (s, args) => ApDungLocGioiTinh("Nữ");

            filterMenu.Items.Add(headerGioiTinh);
            filterMenu.Items.Add(itemTatCaGT);
            filterMenu.Items.Add(itemNam);
            filterMenu.Items.Add(itemNu);
            filterMenu.Items.Add(new Separator());

            // Phần lọc Chi nhánh (Load động từ DB)
            MenuItem headerChiNhanh = new MenuItem { Header = "CHI NHÁNH", IsEnabled = false, FontWeight = FontWeights.Bold };
            filterMenu.Items.Add(headerChiNhanh);

            MenuItem itemTatCaCN = new MenuItem { Header = "Tất cả chi nhánh", IsChecked = (_filterChiNhanh == "Tất cả") };
            itemTatCaCN.Click += (s, args) => ApDungLocChiNhanh("Tất cả", "Tất cả chi nhánh");
            filterMenu.Items.Add(itemTatCaCN);
            var listCN = _ptRepository.LayTatCaChiNhanh();
            foreach (var cn in listCN)
            {
                MenuItem itemCN = new MenuItem { Header = cn.TenCN, IsChecked = (_filterChiNhanh == cn.MaCN) };
                string maCN = cn.MaCN; string tenCN = cn.TenCN;
                itemCN.Click += (s, args) => ApDungLocChiNhanh(maCN, tenCN);
                filterMenu.Items.Add(itemCN);
            }

            // Hiển thị menu tại vị trí nút bấm
            if (sender is Button btn)
            {
                btn.ContextMenu = filterMenu;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void ApDungLocGioiTinh(string val) { _filterGioiTinh = val; ThucHienTimKiem(); }
        private void ApDungLocChiNhanh(string maCN, string tenCN) { _filterChiNhanh = maCN; _filterChiNhanhLabel = tenCN; ThucHienTimKiem(); }
        #endregion

        #region Hành động trên từng dòng (Row Actions)
        private void BtnXem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PT pt)
            {
                XemThongTinPTWindow viewWindow = new XemThongTinPTWindow(pt);
                viewWindow.ShowDialog();
            }
        }

        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PT pt)
            {
                ThemPTWindow editWindow = new ThemPTWindow(pt);
                editWindow.ShowDialog();
                if (editWindow.IsSuccess) TaiDuLieu();
            }
        }

        private void BtnXoaRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PT pt && MessageBox.Show($"Xóa PT {pt.HoTen}?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // SỬA LỖI: DeletePT -> XoaPT
                _ptRepository.XoaPT(pt.MaPT);
                TaiDuLieu();
            }
        }
        #endregion
    }

    // ==========================================
    // CÁC LỚP HỖ TRỢ (MODEL & DTO)
    // ==========================================

    // Model PT: Ánh xạ dữ liệu Huấn luyện viên
    public class PT : INotifyPropertyChanged
    {
        public string MaPT { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string SDT { get; set; }
        public string GioiTinh { get; set; }
        public string MaCN { get; set; }
        public string TenCN { get; set; } // Tên chi nhánh (Lấy từ bảng ChiNhanh)

        // Thuộc tính hỗ trợ Binding Checkbox
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

    // DTO đơn giản cho danh sách Chi nhánh (dùng trong ComboBox/Menu lọc)
    public class ChiNhanhSimple
    {
        public string MaCN { get; set; }
        public string TenCN { get; set; }
        public override string ToString() { return TenCN; }
    }

    // ==========================================
    // REPOSITORY: XỬ LÝ TRUY VẤN DỮ LIỆU
    // ==========================================
    public class PTRepository
    {
        private string _ChuoiKetNoi;
        private readonly TruyCapDB _dbAccess;

        public PTRepository()
        {
            _dbAccess = new TruyCapDB();
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;
        }
        public List<PT> TimKiemPT(string keyword, string gioiTinh = "Tất cả", string maCN = "Tất cả")
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

        // GenerateNewMaPT -> TaoMaPTMoi: Sinh mã tự động tăng (VD: PT001, PT002)
        public string TaoMaPTMoi()
        {
            string newMa = "PT001";
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string sql = "SELECT MaPT FROM PT ORDER BY length(MaPT) DESC, MaPT DESC LIMIT 1";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            string maxMa = result.ToString();
                            if (maxMa.Length > 2 && int.TryParse(maxMa.Substring(2), out int currentNum))
                                newMa = $"PT{(currentNum + 1).ToString("D3")}";
                        }
                    }
                }
            }
            catch { }
            return newMa;
        }

        // GetAllChiNhanh -> LayTatCaChiNhanh: Lấy danh sách chi nhánh để hiển thị lọc
        public List<ChiNhanhSimple> LayTatCaChiNhanh()
        {
            List<ChiNhanhSimple> list = new List<ChiNhanhSimple>();
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    using (var cmd = new SqliteCommand("SELECT MaCN, TenCN FROM ChiNhanh", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(new ChiNhanhSimple { MaCN = reader["MaCN"].ToString(), TenCN = reader["TenCN"].ToString() });
                    }
                }
            }
            catch { }
            return list;
        }

        // CheckMaPTExists -> KiemTraMaPTTonTai
        public bool KiemTraMaPTTonTai(string maPT)
        {
            using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
            {
                conn.Open();
                using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM PT WHERE MaPT = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", maPT);
                    return (long)cmd.ExecuteScalar() > 0;
                }
            }
        }
        public bool ThemPT(PT pt)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string sql = @"INSERT INTO PT (MaPT, HoTen, Email, SDT, GioiTinh, MaCN) VALUES (@MaPT, @HoTen, @Email, @SDT, @GioiTinh, @MaCN)";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaPT", pt.MaPT);
                        cmd.Parameters.AddWithValue("@HoTen", pt.HoTen);
                        cmd.Parameters.AddWithValue("@Email", pt.Email);
                        cmd.Parameters.AddWithValue("@SDT", pt.SDT);
                        cmd.Parameters.AddWithValue("@GioiTinh", pt.GioiTinh);
                        cmd.Parameters.AddWithValue("@MaCN", pt.MaCN);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch { return false; }
        }
        public bool CapNhatPT(PT pt)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string sql = @"UPDATE PT SET HoTen=@HoTen, Email=@Email, SDT=@SDT, GioiTinh=@GioiTinh, MaCN=@MaCN WHERE MaPT=@MaPT";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaPT", pt.MaPT);
                        cmd.Parameters.AddWithValue("@HoTen", pt.HoTen);
                        cmd.Parameters.AddWithValue("@Email", pt.Email);
                        cmd.Parameters.AddWithValue("@SDT", pt.SDT);
                        cmd.Parameters.AddWithValue("@GioiTinh", pt.GioiTinh);
                        cmd.Parameters.AddWithValue("@MaCN", pt.MaCN);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch { return false; }
        }
        public bool XoaPT(string maPT)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string sql = "DELETE FROM PT WHERE MaPT = @id";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", maPT);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch { return false; }
        }
    }
}