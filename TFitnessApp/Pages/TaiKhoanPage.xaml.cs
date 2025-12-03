using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media;
using System.Windows.Input;
using System.Globalization;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using TFitnessApp.Windows;
using TFitnessApp.Database;

namespace TFitnessApp
{
    public partial class TaiKhoanPage : Page
    {
        #region Khai báo biến
        private string _ChuoiKetNoi;
        private readonly DbAccess _dbAccess;

        private List<MoDonDuLieuTaiKhoan> tatCaTaiKhoan = new List<MoDonDuLieuTaiKhoan>();
        private List<MoDonDuLieuTaiKhoan> danhSachGoc = new List<MoDonDuLieuTaiKhoan>();
        private ObservableCollection<MoDonDuLieuTaiKhoan> taiKhoanHienThi = new ObservableCollection<MoDonDuLieuTaiKhoan>();

        private const string VAN_BAN_TIM_KIEM_MAC_DINH = "Tìm kiếm mã TK, tên đăng nhập, họ tên...";

        private int _trangHienTai = 1;
        private int _soBanGhiMoiTrang = 50;
        private int _tongSoTrang = 1;
        private int _tongSoBanGhi = 0;
        private string _tuKhoaTimKiem = "";
        private bool _isInitialLoadComplete = false;

        #endregion

        #region Constructor và Khởi tạo
        // Khởi tạo trang Tài khoản
        public TaiKhoanPage()
        {
            InitializeComponent();
            // Khởi tạo đối tượng DbAccess
            _dbAccess = new DbAccess();
            // Lấy chuỗi kết nối
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;

            if (SearchTextBox != null)
            {
                SearchTextBox.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                SearchTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
                SearchPlaceholder.Visibility = Visibility.Visible;
            }

            KhoiTaoComboBoxSoBanGhi();
            TaiDanhSachTaiKhoan();
        }

        // Khởi tạo combobox chọn số bản ghi hiển thị mỗi trang
        private void KhoiTaoComboBoxSoBanGhi()
        {
            if (cboSoBanGhiMoiTrang != null)
            {
                cboSoBanGhiMoiTrang.ItemsSource = new List<int> { 10, 20, 50, 100 };
                cboSoBanGhiMoiTrang.SelectedItem = _soBanGhiMoiTrang;
            }
        }
        #endregion

        #region Quản lý Phân trang
        // Cập nhật các thông số phân trang (tổng số bản ghi, tổng số trang)
        private void CapNhatPhanTrang()
        {
            _tongSoBanGhi = tatCaTaiKhoan.Count;
            _tongSoTrang = (int)Math.Ceiling((double)_tongSoBanGhi / _soBanGhiMoiTrang);

            if (_trangHienTai > _tongSoTrang && _tongSoTrang > 0)
                _trangHienTai = _tongSoTrang;
            else if (_trangHienTai < 1)
                _trangHienTai = 1;

            CapNhatGiaoDienPhanTrang();
        }

        // Cập nhật giao diện các nút và thông tin phân trang
        private void CapNhatGiaoDienPhanTrang()
        {
            if (txtThongTinPhanTrang != null)
            {
                int batDau = (_trangHienTai - 1) * _soBanGhiMoiTrang + 1;
                int ketThuc = Math.Min(_trangHienTai * _soBanGhiMoiTrang, _tongSoBanGhi);

                if (_tongSoBanGhi > 0)
                {
                    txtThongTinPhanTrang.Text = $"Hiển thị {batDau}-{ketThuc} của {_tongSoBanGhi} tài khoản";
                }
                else
                {
                    txtThongTinPhanTrang.Text = $"Hiển thị 0 của 0 tài khoản";
                }
            }

            bool isFirstPage = _trangHienTai <= 1;
            bool isLastPage = _trangHienTai >= _tongSoTrang;

            if (btnTrangDau != null) btnTrangDau.IsEnabled = !isFirstPage;
            if (btnTrangTruoc != null) btnTrangTruoc.IsEnabled = !isFirstPage;
            if (btnTrangSau != null) btnTrangSau.IsEnabled = !isLastPage;
            if (btnTrangCuoi != null) btnTrangCuoi.IsEnabled = !isLastPage;

            if (txtTrangHienTai != null) txtTrangHienTai.Text = $"{_trangHienTai}/{_tongSoTrang}";
            if (txtChuyenTrang != null) txtChuyenTrang.Text = _trangHienTai.ToString();
        }

        // Hiển thị dữ liệu cho trang hiện tại sau khi tính toán phân trang
        private void HienThiDuLieuPhanTrang()
        {
            if (dgTaiKhoan == null || taiKhoanHienThi == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                taiKhoanHienThi.Clear();
                CapNhatPhanTrang();

                if (_tongSoBanGhi == 0)
                {
                    dgTaiKhoan.ItemsSource = taiKhoanHienThi;
                    CapNhatThongTinSoLuong();
                    return;
                }

                var duLieuPhanTrang = tatCaTaiKhoan
                    .Skip((_trangHienTai - 1) * _soBanGhiMoiTrang)
                    .Take(_soBanGhiMoiTrang)
                    .ToList();

                foreach (var item in duLieuPhanTrang)
                {
                    taiKhoanHienThi.Add(item);
                }

                dgTaiKhoan.ItemsSource = taiKhoanHienThi;
                CapNhatTrangThaiSelectAll();
                CapNhatThongTinSoLuong();
            });
        }
        #endregion

        #region Xử lý database
        // Tải danh sách tài khoản từ database SQLite
        private void TaiDanhSachTaiKhoan()
        {
            try
            {
                Debug.WriteLine("=== BẮT ĐẦU TẢI DỮ LIỆU TÀI KHOẢN ===");
                LoadingIndicator.Visibility = Visibility.Visible;

                tatCaTaiKhoan.Clear();
                taiKhoanHienThi.Clear();
                danhSachGoc.Clear();

                using (SqliteConnection conn = DbAccess.CreateConnection())
                {
                    conn.Open();

                    string sql = @"SELECT 
                                     MaTK,
                                     HoTen,
                                     PhanQuyen,
                                     TenDangNhap,
                                     MatKhau,
                                     NgayTao,
                                     TrangThai,
                                     Email,
                                     SDT
                                     FROM TaiKhoan
                                     ORDER BY NgayTao DESC";

                    Debug.WriteLine("SQL: " + sql);

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read())
                        {
                            count++;
                            DateTime ngayTao = DateTime.MinValue;
                            string ngayTaoString = reader["NgayTao"].ToString();

                            Debug.WriteLine($"NgayTao từ DB: {ngayTaoString}");

                            bool parseSuccess = false;

                            if (!string.IsNullOrEmpty(ngayTaoString))
                            {
                                string[] formats = { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "d-M-yyyy", "d/M/yyyy", "M/d/yyyy" };

                                if (DateTime.TryParseExact(ngayTaoString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ns))
                                {
                                    if (ns.Year > 1900)
                                    {
                                        ngayTao = ns;
                                        parseSuccess = true;
                                    }
                                }
                                else if (DateTime.TryParse(ngayTaoString, out ngayTao))
                                {
                                    if (ngayTao.Year > 1900)
                                    {
                                        parseSuccess = true;
                                    }
                                }
                            }

                            if (!parseSuccess)
                            {
                                Debug.WriteLine($"KHÔNG THỂ PARSE NGÀY: {ngayTaoString}");
                                ngayTao = DateTime.MinValue;
                            }

                            var item = new MoDonDuLieuTaiKhoan
                            {
                                MaTK = reader["MaTK"]?.ToString() ?? "Không xác định",
                                HoTen = reader["HoTen"]?.ToString() ?? "Không xác định",
                                PhanQuyen = reader["PhanQuyen"]?.ToString() ?? "Không xác định",
                                TenDangNhap = reader["TenDangNhap"]?.ToString() ?? "Không xác định",
                                MatKhau = reader["MatKhau"]?.ToString() ?? "Không xác định",
                                NgayTao = ngayTao,
                                TrangThai = reader["TrangThai"]?.ToString() ?? "Không xác định",
                                Email = reader["Email"]?.ToString() ?? "",
                                SDT = reader["SDT"]?.ToString() ?? "",
                                IsSelected = false
                            };

                            Debug.WriteLine($"Tài khoản {count}: MaTK={item.MaTK}, TenDangNhap={item.TenDangNhap}, TrangThai={item.TrangThai}");

                            danhSachGoc.Add(item);
                        }

                        Debug.WriteLine($"Tổng số bản ghi đọc được: {count}");
                    }
                }

                Debug.WriteLine($"Tổng số tài khoản trong danhSachGoc: {danhSachGoc.Count}");
                tatCaTaiKhoan = new List<MoDonDuLieuTaiKhoan>(danhSachGoc);
                _isInitialLoadComplete = true;
                ApDungTimKiem();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LỖI: {ex.Message}");
                MessageBox.Show($"Lỗi khi tải danh sách tài khoản:\n{ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                Debug.WriteLine("=== KẾT THÚC TẢI DỮ LIỆU TÀI KHOẢN ===");
            }
        }
        #endregion

        #region Xử lý Tìm kiếm (Search)
        // Xử lý khi ô tìm kiếm nhận focus (xóa placeholder)
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.Black;
            }
            SearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        // Xử lý khi ô tìm kiếm mất focus (hiển thị placeholder nếu rỗng)
        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                SearchTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
                SearchPlaceholder.Visibility = Visibility.Visible;
            }
        }

        // Xử lý sự kiện TextChanged để ẩn/hiện placeholder
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text) || SearchTextBox.Text == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                SearchPlaceholder.Visibility = Visibility.Visible;
            }
            else
            {
                SearchPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        // Kích hoạt tìm kiếm khi nhấn Enter
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ThucHienTimKiem();
            }
        }

        // Kích hoạt tìm kiếm khi nhấn nút Search
        private void ThucHienTimKiem_Click(object sender, RoutedEventArgs e)
        {
            ThucHienTimKiem();
        }

        // Lấy từ khóa và gọi hàm áp dụng tìm kiếm
        private void ThucHienTimKiem()
        {
            if (SearchTextBox == null || dgTaiKhoan == null) return;

            string tuKhoa = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(tuKhoa) || tuKhoa == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                _tuKhoaTimKiem = "";
                ApDungTimKiem();
                return;
            }

            _tuKhoaTimKiem = tuKhoa;
            ApDungTimKiem();
        }

        // Lọc danh sách dữ liệu gốc theo từ khóa tìm kiếm
        private void ApDungTimKiem()
        {
            Debug.WriteLine("=== ÁP DỤNG TÌM KIẾM ===");
            Debug.WriteLine($"Từ khóa tìm kiếm: {_tuKhoaTimKiem}");
            Debug.WriteLine($"Số bản ghi trong danhSachGoc: {danhSachGoc.Count}");

            var ketQua = danhSachGoc.AsEnumerable();

            if (!string.IsNullOrEmpty(_tuKhoaTimKiem))
            {
                string tuKhoaKhongDau = BoQuyenDau(_tuKhoaTimKiem).ToLower();
                ketQua = ketQua.Where(tk =>
                    BoQuyenDau(tk.MaTK).ToLower().Contains(tuKhoaKhongDau) ||
                    BoQuyenDau(tk.TenDangNhap).ToLower().Contains(tuKhoaKhongDau) ||
                    BoQuyenDau(tk.HoTen).ToLower().Contains(tuKhoaKhongDau) ||
                    BoQuyenDau(tk.PhanQuyen).ToLower().Contains(tuKhoaKhongDau) ||
                    BoQuyenDau(tk.TrangThai).ToLower().Contains(tuKhoaKhongDau) ||
                    tk.NgayTaoFormatted.ToLower().Contains(_tuKhoaTimKiem.ToLower())
                );
            }

            tatCaTaiKhoan = ketQua.ToList();
            Debug.WriteLine($"Số bản ghi sau khi tìm kiếm: {tatCaTaiKhoan.Count}");

            _trangHienTai = 1;
            HienThiDuLieuPhanTrang();
        }

        // Hàm loại bỏ dấu tiếng Việt (để hỗ trợ tìm kiếm không dấu)
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

        #region Xử lý Sự kiện Phân trang
        // Thay đổi số bản ghi tối đa trên một trang
        private void cboSoBanGhiMoiTrang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSoBanGhiMoiTrang.SelectedItem != null)
            {
                _soBanGhiMoiTrang = (int)cboSoBanGhiMoiTrang.SelectedItem;
                _trangHienTai = 1;
                HienThiDuLieuPhanTrang();
            }
        }

        // Chuyển về trang đầu tiên
        private void btnTrangDau_Click(object sender, RoutedEventArgs e)
        {
            _trangHienTai = 1;
            HienThiDuLieuPhanTrang();
        }

        // Chuyển về trang trước
        private void btnTrangTruoc_Click(object sender, RoutedEventArgs e)
        {
            if (_trangHienTai > 1)
            {
                _trangHienTai--;
                HienThiDuLieuPhanTrang();
            }
        }

        // Chuyển sang trang sau
        private void btnTrangSau_Click(object sender, RoutedEventArgs e)
        {
            if (_trangHienTai < _tongSoTrang)
            {
                _trangHienTai++;
                HienThiDuLieuPhanTrang();
            }
        }

        // Chuyển đến trang cuối cùng
        private void btnTrangCuoi_Click(object sender, RoutedEventArgs e)
        {
            _trangHienTai = _tongSoTrang;
            HienThiDuLieuPhanTrang();
        }

        // Xử lý sự kiện khi nhấn Enter trong ô nhập số trang
        private void txtChuyenTrang_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && txtChuyenTrang != null)
            {
                if (int.TryParse(txtChuyenTrang.Text, out int trangMoi))
                {
                    if (trangMoi >= 1 && trangMoi <= _tongSoTrang)
                    {
                        _trangHienTai = trangMoi;
                        HienThiDuLieuPhanTrang();
                    }
                    else
                    {
                        MessageBox.Show($"Vui lòng nhập trang từ 1 đến {_tongSoTrang}", "Thông báo",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập số trang hợp lệ", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                if (txtChuyenTrang.Text != _trangHienTai.ToString())
                {
                    txtChuyenTrang.Text = _trangHienTai.ToString();
                }
                Keyboard.ClearFocus();
            }
        }
        #endregion

        #region Xử lý Nút lệnh và Menu Context
        // Thực hiện làm mới dữ liệu
        private void RefreshCommand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SearchTextBox != null)
                {
                    SearchTextBox.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                    SearchTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
                    _tuKhoaTimKiem = "";
                    SearchPlaceholder.Visibility = Visibility.Visible;
                }

                _isInitialLoadComplete = false;
                TaiDanhSachTaiKhoan();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi làm mới dữ liệu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Mở cửa sổ tạo tài khoản mới
        private void AddAccountCommand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window3 taoTaiKhoanWindow = new Window3();
                taoTaiKhoanWindow.Owner = Window.GetWindow(this);
                taoTaiKhoanWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                taoTaiKhoanWindow.ShowDialog();

                _isInitialLoadComplete = false;
                TaiDanhSachTaiKhoan();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ tạo tài khoản: {ex.Message}", "Lỗi",
                                 MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Xóa các tài khoản đã chọn
        private void DeleteAccountCommand_Click(object sender, RoutedEventArgs e)
        {
            var taiKhoanDaChon = taiKhoanHienThi.Where(tk => tk.IsSelected).ToList();

            if (taiKhoanDaChon.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một tài khoản để xóa!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Bạn có chắc muốn xóa {taiKhoanDaChon.Count} tài khoản đã chọn?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqliteConnection conn = DbAccess.CreateConnection())
                    {
                        conn.Open();

                        foreach (var tk in taiKhoanDaChon)
                        {
                            string sql = "DELETE FROM TaiKhoan WHERE MaTK = @MaTK";
                            using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@MaTK", tk.MaTK);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    MessageBox.Show($"Đã xóa {taiKhoanDaChon.Count} tài khoản thành công!", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    _isInitialLoadComplete = false;
                    TaiDanhSachTaiKhoan();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa tài khoản:\n{ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Mở cửa sổ xem chi tiết tài khoản
        private void ViewAccountCommand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null && button.DataContext is MoDonDuLieuTaiKhoan account)
                {
                    Window4 xemTaiKhoanWindow = new Window4(account);
                    xemTaiKhoanWindow.Owner = Window.GetWindow(this);
                    xemTaiKhoanWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    xemTaiKhoanWindow.ShowDialog();

                    _isInitialLoadComplete = false;
                    TaiDanhSachTaiKhoan();
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn một tài khoản để xem chi tiết.", "Thông báo",
                                     MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ xem tài khoản: {ex.Message}", "Lỗi",
                                 MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Xử lý khi chọn Refresh từ context menu
        private void MenuItem_Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshCommand_Click(sender, e);
        }

        // Xử lý khi chọn Add Account từ context menu
        private void MenuItem_AddAccount_Click(object sender, RoutedEventArgs e)
        {
            AddAccountCommand_Click(sender, e);
        }
        #endregion

        #region Quản lý Lựa chọn (Selection)
        // Cập nhật trạng thái của checkbox "Chọn tất cả" trong header DataGrid
        private void CapNhatTrangThaiSelectAll()
        {
            if (dgTaiKhoan == null) return;

            var selectAllCheckBox = FindVisualChild<CheckBox>(dgTaiKhoan, "chkSelectAll");

            if (selectAllCheckBox != null)
            {
                int totalItems = taiKhoanHienThi.Count;
                int selectedItems = taiKhoanHienThi.Count(tk => tk.IsSelected);

                if (totalItems > 0 && selectedItems == totalItems)
                {
                    selectAllCheckBox.IsChecked = true;
                }
                else if (selectedItems > 0)
                {
                    selectAllCheckBox.IsChecked = null;
                }
                else
                {
                    selectAllCheckBox.IsChecked = false;
                }
            }
        }

        // Hàm hỗ trợ tìm kiếm phần tử con trong cây VisualTree (dùng để tìm Checkbox header)
        private T FindVisualChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                {
                    if (string.IsNullOrEmpty(childName))
                        return result;

                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                        return result;
                }

                var childResult = FindVisualChild<T>(child, childName);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        // Xử lý khi checkbox của một hàng được chọn
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.DataContext is MoDonDuLieuTaiKhoan taiKhoan)
            {
                taiKhoan.IsSelected = true;
                if (dgTaiKhoan.SelectedItem == null || dgTaiKhoan.SelectedItem != taiKhoan)
                {
                    dgTaiKhoan.SelectedItem = taiKhoan;
                }
            }
            CapNhatTrangThaiSelectAll();
            CapNhatThongTinSoLuong();
        }

        // Xử lý khi checkbox của một hàng bị bỏ chọn
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.DataContext is MoDonDuLieuTaiKhoan taiKhoan)
            {
                taiKhoan.IsSelected = false;
                if (dgTaiKhoan.SelectedItem == taiKhoan)
                {
                    dgTaiKhoan.SelectedItem = null;
                }
            }
            CapNhatTrangThaiSelectAll();
            CapNhatThongTinSoLuong();
        }

        // Xử lý sự kiện khi checkbox "Chọn tất cả" thay đổi trạng thái
        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            bool isChecked = checkBox?.IsChecked ?? false;

            foreach (var taiKhoan in taiKhoanHienThi)
            {
                taiKhoan.IsSelected = isChecked;
            }

            dgTaiKhoan.Items.Refresh();
            CapNhatThongTinSoLuong();
        }

        // Cập nhật thông tin số lượng tài khoản đang hiển thị và đã chọn
        private void CapNhatThongTinSoLuong()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (txtSoTaiKhoanHienThi != null)
                    txtSoTaiKhoanHienThi.Text = $"Đang hiển thị: {taiKhoanHienThi.Count} tài khoản";

                if (txtSoTaiKhoanDaChon != null)
                {
                    int soLuongDaChon = taiKhoanHienThi.Count(tk => tk.IsSelected);
                    txtSoTaiKhoanDaChon.Text = $"Đã chọn: {soLuongDaChon}";
                    txtSoTaiKhoanDaChon.Visibility = soLuongDaChon > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            });
        }
        #endregion
    }

    #region Model class
    // Model đại diện cho một tài khoản
    public class MoDonDuLieuTaiKhoan
    {
        public string MaTK { get; set; }
        public string HoTen { get; set; }
        public string PhanQuyen { get; set; }
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public DateTime NgayTao { get; set; }
        public string TrangThai { get; set; }
        public string Email { get; set; }
        public string SDT { get; set; }
        public bool IsSelected { get; set; }

        // Thuộc tính định dạng ngày tạo cho hiển thị trên DataGrid
        public string NgayTaoFormatted
        {
            get
            {
                if (NgayTao == DateTime.MinValue)
                    return "Không xác định";
                return NgayTao.ToString("dd/MM/yyyy");
            }
        }
    }
    #endregion
}