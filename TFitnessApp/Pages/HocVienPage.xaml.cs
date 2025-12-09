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
    /// Trang quản lý danh sách Học Viên
    /// </summary>
    public partial class HocVienPage : Page
    {
        #region Trường Dữ liệu Nội bộ
        private HocVienRepository _hocVienRepository;
        private List<HocVien> _danhSachGoc = new List<HocVien>();
        private ObservableCollection<HocVien> _danhSachHienThi = new ObservableCollection<HocVien>();

        // Biến phân trang
        private int _trangHienTai = 1;
        private int _soBanGhiMoiTrang = 50;
        private int _tongSoTrang = 1;
        private int _tongSoBanGhi = 0;

        // Biến lưu trạng thái lọc
        private HocVienFilterData _currentFilter = new HocVienFilterData();
        #endregion

        #region Khởi tạo
        public HocVienPage()
        {
            InitializeComponent();
            _hocVienRepository = new HocVienRepository();

            // Mặc định chọn 50 bản ghi/trang
            if (cboSoBanGhi != null && cboSoBanGhi.Items.Count > 2)
                cboSoBanGhi.SelectedIndex = 2;
        }

        private void SuKienTaiTrang(object sender, RoutedEventArgs e)
        {
            TaiDuLieu();
        }

        private void TaiDuLieu()
        {
            ThucHienTimKiem();
        }
        #endregion

        #region Logic Tìm kiếm
        private void ThucHienTimKiem()
        {
            try
            {
                string keyword = txtSearch.Text.Trim();

                // Gọi hàm tìm kiếm nâng cao 
                _danhSachGoc = _hocVienRepository.TimKiemHocVienNangCao(keyword, _currentFilter);

                _trangHienTai = 1;
                HienThiDuLieuPhanTrang();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tìm kiếm: {ex.Message}");
            }
        }
        #endregion

        #region Logic Phân trang
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
            foreach (var item in dataPage) { _danhSachHienThi.Add(item); }

            if (HocVienDataGrid != null)
                HocVienDataGrid.ItemsSource = _danhSachHienThi;

            CapNhatGiaoDienPhanTrang();
        }

        private void CapNhatGiaoDienPhanTrang()
        {
            if (txtThongTinPhanTrang == null || btnTrangTruoc == null) return;

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
        #endregion

        #region Sự kiện Tương tác
        private void ChonTatCa_Checked(object sender, RoutedEventArgs e) { foreach (var item in _danhSachHienThi) item.IsSelected = true; }
        private void ChonTatCa_Unchecked(object sender, RoutedEventArgs e) { foreach (var item in _danhSachHienThi) item.IsSelected = false; }

        private void cboSoBanGhi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSoBanGhi != null && cboSoBanGhi.SelectedItem != null)
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
            {
                _trangHienTai = p;
                HienThiDuLieuPhanTrang();
            }
            else
            {
                txtTrangHienTai.Text = _trangHienTai.ToString();
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) { ThucHienTimKiem(); }
        private void HocVienDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        #endregion

        #region Chức năng CRUD
        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            ThemHocVienWindow addWindow = new ThemHocVienWindow();
            addWindow.ShowDialog();
            if (addWindow.IsSuccess) TaiDuLieu();
        }

        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            var itemsToDelete = _danhSachHienThi.Where(x => x.IsSelected).ToList();
            if (itemsToDelete.Count == 0) { MessageBox.Show("Vui lòng chọn học viên để xóa!"); return; }

            if (MessageBox.Show($"Xóa {itemsToDelete.Count} học viên?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                foreach (var item in itemsToDelete) _hocVienRepository.XoaHocVien(item.MaHV);
                TaiDuLieu();
            }
        }

        private void BtnLoc_Click(object sender, RoutedEventArgs e)
        {
            LocHocVienWindow filterWindow = new LocHocVienWindow();
            filterWindow.ShowDialog();
            if (filterWindow.IsApply)
            {
                _currentFilter = filterWindow.FilterData;
                ThucHienTimKiem();
            }
        }

        private void BtnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            _currentFilter = new HocVienFilterData();
            _trangHienTai = 1;
            TaiDuLieu();
        }
        #endregion

        #region Hành động trên dòng
        private void BtnXem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HocVien hv)
            {
                XemThongTinHocVienWindow view = new XemThongTinHocVienWindow(hv);
                view.ShowDialog();
            }
        }

        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HocVien hv)
            {
                ThemHocVienWindow edit = new ThemHocVienWindow(hv);
                edit.ShowDialog();
                if (edit.IsSuccess) TaiDuLieu();
            }
        }

        private void BtnXoaRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HocVien hv && MessageBox.Show($"Xóa {hv.HoTen}?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _hocVienRepository.XoaHocVien(hv.MaHV);
                TaiDuLieu();
            }
        }
        #endregion
    }

    // --- CÁC CLASS HỖ TRỢ ---

    public class HocVienFilterData
    {
        public string GioiTinh { get; set; } = "Tất cả";
        public DateTime? NamSinhTu { get; set; }
        public DateTime? NamSinhDen { get; set; }
        public DateTime? NgayThamGiaTu { get; set; }
        public DateTime? NgayThamGiaDen { get; set; }
        public string MaGoi { get; set; } = "Tất cả";
        public string MaCN { get; set; } = "Tất cả";
        public string MaPT { get; set; } = "Tất cả";
    }
    public class ComboBoxItemData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }
    public class HocVien : INotifyPropertyChanged
    {
        public string MaHV { get; set; }
        public string HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string GioiTinh { get; set; }
        public string Email { get; set; }
        public string SDT { get; set; }
        public string DiaChi { get; set; }
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
        protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class HocVienRepository
    {
        private string _ChuoiKetNoi;
        private readonly TruyCapDB _dbAccess;

        public HocVienRepository()
        {
            _dbAccess = new TruyCapDB();
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;
        }

        public List<ComboBoxItemData> LayDanhSachComboBox(string tableName, string idCol, string nameCol)
        {
            var list = new List<ComboBoxItemData>();
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    using (var cmd = new SqliteCommand($"SELECT {idCol}, {nameCol} FROM {tableName}", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read()) list.Add(new ComboBoxItemData { ID = r[0].ToString(), Name = r[1].ToString() });
                    }
                }
            }
            catch { }
            return list;
        }

        public List<HocVien> TimKiemHocVienNangCao(string keyword, HocVienFilterData filter)
        {
            List<HocVien> list = new List<HocVien>();
            string dateConvert_NgaySinh = "(substr(hv.NgaySinh, 7, 4) || '-' || substr(hv.NgaySinh, 4, 2) || '-' || substr(hv.NgaySinh, 1, 2))";
            string dateConvert_NgayBD = "(substr(hd.NgayBatDau, 7, 4) || '-' || substr(hd.NgayBatDau, 4, 2) || '-' || substr(hd.NgayBatDau, 1, 2))";

            string sql = @"
                SELECT DISTINCT hv.MaHV, hv.HoTen, hv.NgaySinh, hv.GioiTinh, hv.Email, hv.SDT
                FROM HocVien hv
                LEFT JOIN HopDong hd ON hv.MaHV = hd.MaHV
                WHERE (hv.MaHV LIKE @k OR hv.HoTen LIKE @k OR hv.SDT LIKE @k OR hv.Email LIKE @k)";

            if (filter.GioiTinh != "Tất cả") sql += " AND hv.GioiTinh = @gt";
            if (filter.NamSinhTu.HasValue) sql += $" AND {dateConvert_NgaySinh} >= @nsTu";
            if (filter.NamSinhDen.HasValue) sql += $" AND {dateConvert_NgaySinh} <= @nsDen";
            if (filter.NgayThamGiaTu.HasValue) sql += $" AND {dateConvert_NgayBD} >= @ngThamGiaTu";
            if (filter.NgayThamGiaDen.HasValue) sql += $" AND {dateConvert_NgayBD} <= @ngThamGiaDen";
            if (filter.MaGoi != "Tất cả" && !string.IsNullOrEmpty(filter.MaGoi)) sql += " AND hd.MaGoi = @maGoi";
            if (filter.MaCN != "Tất cả" && !string.IsNullOrEmpty(filter.MaCN)) sql += " AND hd.MaCN = @maCN";
            if (filter.MaPT != "Tất cả" && !string.IsNullOrEmpty(filter.MaPT)) sql += " AND hd.MaPT = @maPT";
            sql += " ORDER BY hv.MaHV ASC";
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open(); using (var cmd = new SqliteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@k", $"%{keyword}%");
                        if (filter.GioiTinh != "Tất cả") cmd.Parameters.AddWithValue("@gt", filter.GioiTinh);
                        if (filter.NamSinhTu.HasValue) cmd.Parameters.AddWithValue("@nsTu", filter.NamSinhTu.Value.ToString("yyyy-MM-dd"));
                        if (filter.NamSinhDen.HasValue) cmd.Parameters.AddWithValue("@nsDen", filter.NamSinhDen.Value.ToString("yyyy-MM-dd"));
                        if (filter.NgayThamGiaTu.HasValue) cmd.Parameters.AddWithValue("@ngThamGiaTu", filter.NgayThamGiaTu.Value.ToString("yyyy-MM-dd"));
                        if (filter.NgayThamGiaDen.HasValue) cmd.Parameters.AddWithValue("@ngThamGiaDen", filter.NgayThamGiaDen.Value.ToString("yyyy-MM-dd"));
                        if (filter.MaGoi != "Tất cả" && !string.IsNullOrEmpty(filter.MaGoi)) cmd.Parameters.AddWithValue("@maGoi", filter.MaGoi);
                        if (filter.MaCN != "Tất cả" && !string.IsNullOrEmpty(filter.MaCN)) cmd.Parameters.AddWithValue("@maCN", filter.MaCN);
                        if (filter.MaPT != "Tất cả" && !string.IsNullOrEmpty(filter.MaPT)) cmd.Parameters.AddWithValue("@maPT", filter.MaPT);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime? ns = null;
                                string nsStr = reader["NgaySinh"]?.ToString();
                                string[] formats = { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "d-M-yyyy", "d/M/yyyy", "M/d/yyyy", "yyyy/MM/dd", "dd.MM.yyyy" };

                                if (!string.IsNullOrEmpty(nsStr))
                                {
                                    if (DateTime.TryParseExact(nsStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                                    { if (d.Year > 1900) ns = d; }
                                    else if (DateTime.TryParse(nsStr, out DateTime d2))
                                    { if (d2.Year > 1900) ns = d2; }
                                }

                                list.Add(new HocVien
                                {
                                    MaHV = reader["MaHV"].ToString(),
                                    HoTen = reader["HoTen"].ToString(),
                                    NgaySinh = ns,
                                    GioiTinh = reader["GioiTinh"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    SDT = reader["SDT"].ToString(),
                                    DiaChi = "",
                                    IsSelected = false
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi lọc: " + ex.Message); }
            return list;
        }

        public string TaoMaHVMoi()
        {
            string newMa = "HV0001";
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string sql = "SELECT MaHV FROM HocVien ORDER BY length(MaHV) DESC, MaHV DESC LIMIT 1";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            string maxMa = result.ToString();
                            if (maxMa.Length > 2 && int.TryParse(maxMa.Substring(2), out int currentNum)) { newMa = $"HV{(currentNum + 1).ToString("D4")}"; }
                        }
                    }
                }
            }
            catch { }
            return newMa;
        }
        public bool ThemHocVien(HocVien hv)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string sql = @"INSERT INTO HocVien (MaHV, HoTen, NgaySinh, GioiTinh, Email, SDT) VALUES (@MaHV, @HoTen, @NgaySinh, @GioiTinh, @Email, @SDT)";
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
            catch (Exception ex) { MessageBox.Show("Lỗi thêm học viên: " + ex.Message); return false; }
        }
        public bool CapNhatHocVien(HocVien hv)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string sql = @"UPDATE HocVien SET HoTen=@HoTen, NgaySinh=@NgaySinh, GioiTinh=@GioiTinh, Email=@Email, SDT=@SDT WHERE MaHV=@MaHV";
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
            catch (Exception ex) { MessageBox.Show("Lỗi cập nhật: " + ex.Message); return false; }
        }
        public bool XoaHocVien(string maHV)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
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
            catch (Exception ex) { MessageBox.Show("Lỗi xóa học viên: " + ex.Message); return false; }
        }
        public bool KiemTraMaHVTonTai(string maHV)
        {
            using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM HocVien WHERE MaHV = @MaHV";
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MaHV", maHV);
                    return (long)cmd.ExecuteScalar() > 0;
                }
            }
        }
    }
}