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
using TFitnessApp.Database;

namespace TFitnessApp
{
    /// <summary>
    /// Trang quản lý danh sách Gói Tập
    /// </summary>
    public partial class GoiTapPage : Page
    {
        #region Trường Dữ liệu Nội bộ
        // Khai báo Repository để gọi xuống Database
        private GoiTapRepository _repository;

        // --- CÁC BIẾN DÙNG CHO PHÂN TRANG ---
        private List<GoiTap> _danhSachGoc = new List<GoiTap>();// Chứa toàn bộ dữ liệu tìm được
        private ObservableCollection<GoiTap> _danhSachHienThi = new ObservableCollection<GoiTap>();// Chỉ chứa dữ liệu của trang hiện tại (để binding lên Grid)

        private int _trangHienTai = 1;              // Trang đang xem
        private int _soBanGhiMoiTrang = 50;         // Số dòng trên 1 trang (mặc định 50)
        private int _tongSoTrang = 1;               // Tổng số trang tính toán được
        private int _tongSoBanGhi = 0;              // Tổng số bản ghi tìm thấy

        // --- CÁC BIẾN LƯU TRẠNG THÁI BỘ LỌC (để giữ lại giá trị khi chuyển trang) ---
        private double? _filterMinPrice = null;     // Giá thấp nhất
        private double? _filterMaxPrice = null;     // Giá cao nhất
        private string _filterPT = "Tất cả";        // Lọc theo PT
        private int? _filterMonths = null;          // Lọc theo thời hạn
        private string _filterSpecial = "Tất cả";   // Lọc dịch vụ đặc biệt
        #endregion

        #region Khởi tạo
        public GoiTapPage()
        {
            InitializeComponent();
            _repository = new GoiTapRepository();
            // Đặt mặc định combo box số bản ghi là 50 (index = 2)
            if (cboSoBanGhi != null && cboSoBanGhi.Items.Count > 2)
                cboSoBanGhi.SelectedIndex = 2;
        }

        // Sự kiện: Khi trang được tải xong (Loaded)
        private void SuKienTaiTrang(object sender, RoutedEventArgs e)
        {
            TaiDuLieu();                            // Gọi hàm tải dữ liệu
        }

        // Hàm trung gian để gọi tìm kiếm
        private void TaiDuLieu()
        {
            ThucHienTimKiem();
        }
        #endregion

        #region Logic tìm kiếm
        // --- HÀM XỬ LÝ LOGIC TÌM KIẾM CHÍNH ---
        private void ThucHienTimKiem()
        {
            try
            {
                // 1. Lấy từ khóa từ ô tìm kiếm
                string keyword = txtSearch.Text.Trim();

                // 2. Gọi xuống DB để lấy danh sách thỏa mãn TẤT CẢ điều kiện lọc
                _danhSachGoc = _repository.TimKiemGoiTapNangCao(
                    keyword,
                    _filterMinPrice,
                    _filterMaxPrice,
                    _filterPT,
                    _filterMonths,
                    _filterSpecial
                );
                // 3. Reset về trang 1 sau khi tìm kiếm mới
                _trangHienTai = 1;
                // 4. Cắt dữ liệu để hiển thị
                HienThiDuLieuPhanTrang();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tìm kiếm: {ex.Message}");
            }
        }
        #endregion

        #region Logic phân trang
        // --- HÀM TÍNH TOÁN PHÂN TRANG (PAGINATION) ---
        private void HienThiDuLieuPhanTrang()
        {
            if (_danhSachGoc == null) return;
            // 1. Tính toán tổng số trang
            _tongSoBanGhi = _danhSachGoc.Count;
            _tongSoTrang = (int)Math.Ceiling((double)_tongSoBanGhi / _soBanGhiMoiTrang);
            if (_tongSoTrang == 0) _tongSoTrang = 1;
            // 2. Kiểm tra logic trang hiện tại không vượt quá giới hạn
            if (_trangHienTai > _tongSoTrang) _trangHienTai = _tongSoTrang;
            if (_trangHienTai < 1) _trangHienTai = 1;
            // 3. Cắt dữ liệu (Skip & Take). Ví dụ: Trang 2, mỗi trang 50 => Bỏ qua 50 dòng đầu, Lấy 50 dòng tiếp theo
            var dataPage = _danhSachGoc
                .Skip((_trangHienTai - 1) * _soBanGhiMoiTrang)
                .Take(_soBanGhiMoiTrang)
                .ToList();
            // 4. Đưa dữ liệu vào ObservableCollection để giao diện tự cập nhật
            _danhSachHienThi.Clear();
            foreach (var item in dataPage)
            {
                _danhSachHienThi.Add(item);
            }
            // 5. Gán nguồn dữ liệu cho DataGrid
            if (GoiTapDataGrid != null)
            {
                GoiTapDataGrid.ItemsSource = _danhSachHienThi;
            }
            // 6. Cập nhật dòng chữ hiển thị số trang (Footer)
            CapNhatGiaoDienPhanTrang();
        }

        // Cập nhật giao diện Footer (Nút Next/Prev, Text hiển thị)
        private void CapNhatGiaoDienPhanTrang()
        {
            if (txtThongTinPhanTrang == null || btnTrangTruoc == null || btnTrangSau == null) return;

            int start = (_tongSoBanGhi == 0) ? 0 : (_trangHienTai - 1) * _soBanGhiMoiTrang + 1;
            int end = Math.Min(_trangHienTai * _soBanGhiMoiTrang, _tongSoBanGhi);
            txtThongTinPhanTrang.Text = $"Hiển thị {start}-{end} của {_tongSoBanGhi} gói tập";

            // Ẩn/Hiện nút trang trước/sau tùy theo trang hiện tại
            btnTrangTruoc.IsEnabled = _trangHienTai > 1;
            btnTrangSau.IsEnabled = _trangHienTai < _tongSoTrang;
        }
        #endregion

        #region Sự kiện tương tác người dùng
        // --- CÁC SỰ KIỆN TƯƠNG TÁC NGƯỜI DÙNG ---

        // Checkbox chọn tất cả ở Header DataGrid
        private void ChonTatCa_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _danhSachHienThi)
            {
                item.IsSelected = true;
            }
        }

        private void ChonTatCa_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _danhSachHienThi)
            {
                item.IsSelected = false;
            }
        }
        // Khi gõ chữ vào ô tìm kiếm -> Tìm ngay lập tức
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ThucHienTimKiem();
        }
        // Khi thay đổi số bản ghi mỗi trang (10, 20, 50...)
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
        // Nút chuyển trang
        private void btnTrangTruoc_Click(object sender, RoutedEventArgs e)
        {
            if (_trangHienTai > 1)
            {
                _trangHienTai--;
                HienThiDuLieuPhanTrang();
            }
        }
        private void btnTrangSau_Click(object sender, RoutedEventArgs e)
        {
            if (_trangHienTai < _tongSoTrang)
            {
                _trangHienTai++;
                HienThiDuLieuPhanTrang();
            }
        }

        private void GoiTapDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) {}
        #endregion

        #region Chức năng CRUD
        // --- CÁC CHỨC NĂNG CRUD (THÊM, XÓA, SỬA) ---
        // 1. Nút THÊM MỚI
        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            // Mở cửa sổ thêm mới (Constructor không truyền tham số)
            ThemGoiTapWindow addWindow = new ThemGoiTapWindow();
            addWindow.ShowDialog();
            // Nếu thêm thành công -> Tải lại dữ liệu
            if (addWindow.IsSuccess) TaiDuLieu();
        }
        // 2. Nút XÓA (Xóa nhiều dòng đã chọn)
        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            // Lọc ra các item đang được tích chọn
            var itemsToDelete = _danhSachHienThi.Where(x => x.IsSelected).ToList();
            if (itemsToDelete.Count == 0) { MessageBox.Show("Vui lòng chọn gói tập để xóa!", "Thông báo"); return; }

            if (MessageBox.Show($"Xóa {itemsToDelete.Count} gói tập đã chọn?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // Duyệt vòng lặp để xóa từng cái
                foreach (var item in itemsToDelete) _repository.XoaGoiTap(item.MaGoi);
                TaiDuLieu();                    // Refresh lại lưới
                MessageBox.Show("Đã xóa thành công!", "Thông báo");
            }
        }

        // 3. Nút LỌC NÂNG CAO
        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            LocGoiTapWindow filterWindow = new LocGoiTapWindow();
            filterWindow.ShowDialog();
            // Nếu người dùng ấn "Áp dụng" ở cửa sổ lọc
            if (filterWindow.IsApply && filterWindow.FilterData != null)
            {
                var data = filterWindow.FilterData;
                // Lưu lại các điều kiện lọc vào biến toàn cục
                _filterMinPrice = data.MinPrice;
                _filterMaxPrice = data.MaxPrice;
                _filterPT = data.PTOption;
                _filterMonths = data.Months;
                _filterSpecial = data.SpecialService;
                // Thực hiện tìm kiếm lại với bộ lọc mới
                ThucHienTimKiem();
            }
        }

        // 4. Nút LÀM MỚI (Reset mọi thứ)
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            _filterMinPrice = null;
            _filterMaxPrice = null;
            _filterPT = "Tất cả";
            _filterMonths = null;
            _filterSpecial = "Tất cả";

            _trangHienTai = 1;
            TaiDuLieu();
        }
        #endregion

        #region Các hành động trên từng dòng
        // --- CÁC HÀNH ĐỘNG TRÊN TỪNG DÒNG (ROW ACTIONS) ---
        // Hành động xem chi tiết
        private void BtnXem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GoiTap gt)
            {
                XemThongTinGoiTapWindow viewWindow = new XemThongTinGoiTapWindow(gt);
                viewWindow.ShowDialog();
            }
        }
        // Hành động sửa
        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GoiTap gt)
            {
                ThemGoiTapWindow editWindow = new ThemGoiTapWindow(gt);
                editWindow.ShowDialog();
                if (editWindow.IsSuccess) TaiDuLieu();
            }
        }
        //Hành động xóa 1 dòng
        private void BtnXoaRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GoiTap gt)
            {
                if (MessageBox.Show($"Xóa gói tập: {gt.TenGoi}?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _repository.XoaGoiTap(gt.MaGoi);
                    TaiDuLieu();
                }
            }
        }
        #endregion
    }
    #region Lớp đối tượng
    // 1. MODEL: LỚP ĐỐI TƯỢNG GÓI TẬP
    // implement INotifyPropertyChanged để giao diện (WPF) tự động cập nhật khi dữ liệu thay đổi
    public class GoiTap : INotifyPropertyChanged
    {
        // --- CÁC THUỘC TÍNH ÁNH XẠ VỚI CỘT TRONG DATABASE ---
        public string MaGoi { get; set; }
        public string TenGoi { get; set; }
        public int ThoiHan { get; set; }
        public double GiaNiemYet { get; set; }
        public int SoBuoiPT { get; set; }
        public string DichVuDacBiet { get; set; }
        public string TrangThai { get; set; }

        // --- CÁC THUỘC TÍNH PHỤ TRỢ (KHÔNG LƯU TRONG DB) ---

        // Biến lưu trạng thái checkbox trên lưới dữ liệu (DataGrid)
        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    // Khi giá trị thay đổi, thông báo cho giao diện biết để cập nhật lại hình ảnh checkbox
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }
        // Thuộc tính chỉ đọc để hiển thị giá tiền đẹp hơn (VD: 1000000 -> 1,000,000)
        public string GiaNiemYetFormatted => GiaNiemYet.ToString("N0", CultureInfo.InvariantCulture);

        // --- CƠ CHẾ CẬP NHẬT GIAO DIỆN ---
        public event PropertyChangedEventHandler PropertyChanged;
        // Hàm kích hoạt sự kiện thông báo thay đổi
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion
   
    #region Xử lý truy vấn Database
    // 2. REPOSITORY: LỚP XỬ LÝ TRUY VẤN DATABASE (CRUD)
    // Chứa toàn bộ các câu lệnh SQL: Select, Insert, Update, Delete
    public class GoiTapRepository
    {
        private string _ChuoiKetNoi;
        private readonly TruyCapDB _dbAccess;

        public GoiTapRepository()
        {
            // Khởi tạo đối tượng DbAccess
            _dbAccess = new TruyCapDB();
            // Lấy chuỗi kết nối
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;
        }
        #endregion
        #region Tìm kiếm nâng cao
        // Kết hợp nhiều điều kiện lọc: Từ khóa, Giá, PT, Thời hạn, Dịch vụ...
        public List<GoiTap> TimKiemGoiTapNangCao(string keyword, double? minPrice, double? maxPrice, string ptOption, int? months, string specialService)
        {
            List<GoiTap> list = new List<GoiTap>();
            // Câu lệnh SQL khởi tạo (Tìm theo từ khóa trước)
            string sql = "SELECT * FROM GoiTap WHERE (MaGoi LIKE @k OR TenGoi LIKE @k)";

            // --- CỘNG CHUỖI SQL DỰA VÀO ĐIỀU KIỆN LỌC ---

            // 1. Lọc theo khoảng giá
            if (minPrice.HasValue) sql += " AND GiaNiemYet >= @minP";
            if (maxPrice.HasValue) sql += " AND GiaNiemYet <= @maxP";

            // 2. Lọc theo tùy chọn PT (Có hoặc Không)
            if (!string.IsNullOrEmpty(ptOption))
            {
                if (ptOption == "Có PT") sql += " AND SoBuoiPT > 0";
                else if (ptOption == "Không PT") sql += " AND SoBuoiPT = 0";
            }

            // 3. Lọc theo thời hạn (1 tháng, 3 tháng,...)
            if (months.HasValue && months.Value > 0)
            {
                sql += " AND ThoiHan = @months";
            }

            // 3. Lọc theo thời hạn (1 tháng, 3 tháng,...)
            if (!string.IsNullOrEmpty(specialService) && specialService != "Tất cả")
            {
                sql += " AND DichVuDacBiet LIKE @special";
            }

            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
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

                        // Thực thi và đọc dữ liệu trả về
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Ánh xạ từng dòng dữ liệu từ DB sang đối tượng C#
                                list.Add(new GoiTap
                                {
                                    MaGoi = reader["MaGoi"].ToString(),
                                    TenGoi = reader["TenGoi"].ToString(),
                                    ThoiHan = Convert.ToInt32(reader["ThoiHan"]),
                                    GiaNiemYet = Convert.ToDouble(reader["GiaNiemYet"]),
                                    SoBuoiPT = Convert.ToInt32(reader["SoBuoiPT"]),
                                    DichVuDacBiet = reader["DichVuDacBiet"].ToString(),
                                    TrangThai = reader["TrangThai"].ToString(),
                                    IsSelected = false       // Mặc định chưa chọn khi mới load   
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
        public List<GoiTap> TimKiemGoiTap(string keyword)
        {
            return TimKiemGoiTapNangCao(keyword, null, null, null, null, null);
        }

        // Hàm giữ chỗ để tạo mã tự động (chưa implement)
        public string TaoMaGoiMoi()
        {
            // Logic cũ của bạn đang để trống, có thể giữ nguyên hoặc implement sau
            return "";
        }

        // Kiểm tra xem Mã gói đã tồn tại chưa (Dùng khi thêm mới để tránh trùng khóa chính)
        public bool KiemTraMaGoiTonTai(string maGoi)
        {
            using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM GoiTap WHERE MaGoi = @id";
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", maGoi);
                    // ExecuteScalar trả về giá trị đầu tiên (ở đây là số lượng bản ghi tìm thấy)
                    return (long)cmd.ExecuteScalar() > 0;
                }
            }
        }
     #endregion

        #region Thêm, Sửa, Xóa gói tập
        // --- THÊM MỚI GÓI TẬP (INSERT) ---
        public bool ThemGoiTap(GoiTap gt)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    // Câu lệnh INSERT
                    string sql = @"INSERT INTO GoiTap (MaGoi, TenGoi, ThoiHan, GiaNiemYet, SoBuoiPT, DichVuDacBiet, TrangThai) 
                                   VALUES (@MaGoi, @TenGoi, @ThoiHan, @Gia, @SoBuoi, @DV, @TrangThai)";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        // Gán giá trị cho tham số
                        cmd.Parameters.AddWithValue("@MaGoi", gt.MaGoi);
                        cmd.Parameters.AddWithValue("@TenGoi", gt.TenGoi);
                        cmd.Parameters.AddWithValue("@ThoiHan", gt.ThoiHan);
                        cmd.Parameters.AddWithValue("@Gia", gt.GiaNiemYet);
                        cmd.Parameters.AddWithValue("@SoBuoi", gt.SoBuoiPT);
                        cmd.Parameters.AddWithValue("@DV", gt.DichVuDacBiet);
                        cmd.Parameters.AddWithValue("@TrangThai", gt.TrangThai);
                        // ExecuteNonQuery: Thực thi lệnh không trả về dữ liệu (Insert/Update/Delete)
                        // Trả về số dòng bị ảnh hưởng (> 0 là thành công)
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi thêm: " + ex.Message); return false; }
        }
        // --- SỬA GÓI TẬP (UPDATE) ---
        public bool CapNhatGoiTap(GoiTap gt)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
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
        // --- XÓA GÓI TẬP (DELETE) ---
        public bool XoaGoiTap(string maGoi)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
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
        #endregion
    }
}