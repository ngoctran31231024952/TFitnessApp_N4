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

namespace TFitnessApp
{
    public partial class GiaoDichPage : Page
    {
        private string chuoiKetNoi;
        private List<MoDonDuLieuGiaoDich> tatCaGiaoDich = new List<MoDonDuLieuGiaoDich>();
        private List<MoDonDuLieuGiaoDich> danhSachGoc = new List<MoDonDuLieuGiaoDich>();
        private ObservableCollection<MoDonDuLieuGiaoDich> giaoDichHienThi = new ObservableCollection<MoDonDuLieuGiaoDich>();

        // Khai báo hằng số
        private const string VAN_BAN_TIM_KIEM_MAC_DINH = "Tìm kiếm học viên, mã giao dịch, gói tập...";

        // Biến phân trang
        private int _trangHienTai = 1;
        private int _soBanGhiMoiTrang = 50;
        private int _tongSoTrang = 1;
        private int _tongSoBanGhi = 0;

        // Biến lưu trạng thái hiện tại
        private string _trangThaiHienTai = "Chưa thanh toán";
        private string _tuKhoaTimKiem = "";

        // Biến cờ để kiểm soát việc load data lần đầu
        private bool _isInitialLoadComplete = false;

        public GiaoDichPage()
        {
            InitializeComponent();

            // Khởi tạo chuỗi kết nối
            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            chuoiKetNoi = $"Data Source={duongDanDB};";

            // Kiểm tra file database
            if (!File.Exists(duongDanDB))
            {
                MessageBox.Show($"Không tìm thấy cơ sở dữ liệu tại:\n{duongDanDB}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Đặt mặc định text tìm kiếm
            if (SearchTextBox != null)
            {
                SearchTextBox.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                SearchTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
                // Đảm bảo placeholder hiển thị ban đầu
                SearchPlaceholder.Visibility = Visibility.Visible;
            }

            // Khởi tạo combobox số bản ghi
            KhoiTaoComboBoxSoBanGhi();

            // Tải dữ liệu khi khởi tạo
            TaiDanhSachGiaoDich();
        }

        #region Khởi tạo phân trang

        private void KhoiTaoComboBoxSoBanGhi()
        {
            if (cboSoBanGhiMoiTrang != null)
            {
                cboSoBanGhiMoiTrang.ItemsSource = new List<int> { 10, 20, 50, 100 };
                cboSoBanGhiMoiTrang.SelectedItem = _soBanGhiMoiTrang;
            }
        }

        private void CapNhatPhanTrang()
        {
            _tongSoBanGhi = tatCaGiaoDich.Count;
            _tongSoTrang = (int)Math.Ceiling((double)_tongSoBanGhi / _soBanGhiMoiTrang);

            if (_trangHienTai > _tongSoTrang && _tongSoTrang > 0)
                _trangHienTai = _tongSoTrang;
            else if (_trangHienTai < 1)
                _trangHienTai = 1;

            // Cập nhật giao diện phân trang
            CapNhatGiaoDienPhanTrang();
        }

        private void CapNhatGiaoDienPhanTrang()
        {
            // Cập nhật text thông tin phân trang
            if (txtThongTinPhanTrang != null)
            {
                int batDau = (_trangHienTai - 1) * _soBanGhiMoiTrang + 1;
                int ketThuc = Math.Min(_trangHienTai * _soBanGhiMoiTrang, _tongSoBanGhi);

                if (_tongSoBanGhi > 0)
                {
                    txtThongTinPhanTrang.Text = $"Hiển thị {batDau}-{ketThuc} của {_tongSoBanGhi} giao dịch";
                }
                else
                {
                    txtThongTinPhanTrang.Text = $"Hiển thị 0 của 0 giao dịch";
                }
            }

            // Cập nhật nút điều hướng
            bool isFirstPage = _trangHienTai <= 1;
            bool isLastPage = _trangHienTai >= _tongSoTrang;

            if (btnTrangDau != null)
                btnTrangDau.IsEnabled = !isFirstPage;

            if (btnTrangTruoc != null)
                btnTrangTruoc.IsEnabled = !isFirstPage;

            if (btnTrangSau != null)
                btnTrangSau.IsEnabled = !isLastPage;

            if (btnTrangCuoi != null)
                btnTrangCuoi.IsEnabled = !isLastPage;

            // Cập nhật text trang hiện tại
            if (txtTrangHienTai != null)
                txtTrangHienTai.Text = $"{_trangHienTai}/{_tongSoTrang}";

            // Cập nhật textbox chuyển trang
            if (txtChuyenTrang != null)
                txtChuyenTrang.Text = _trangHienTai.ToString();
        }

        private void HienThiDuLieuPhanTrang()
        {
            // Kiểm tra xem các control đã được khởi tạo chưa
            if (dgGiaoDich == null || giaoDichHienThi == null) return;

            // Đảm bảo chạy trên UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                giaoDichHienThi.Clear();

                CapNhatPhanTrang();

                if (_tongSoBanGhi == 0)
                {
                    dgGiaoDich.ItemsSource = giaoDichHienThi;
                    CapNhatThongTinSoLuong();
                    return;
                }

                var duLieuPhanTrang = tatCaGiaoDich
                    .Skip((_trangHienTai - 1) * _soBanGhiMoiTrang)
                    .Take(_soBanGhiMoiTrang)
                    .ToList();

                foreach (var item in duLieuPhanTrang)
                {
                    giaoDichHienThi.Add(item);
                }

                // Cập nhật DataGrid
                dgGiaoDich.ItemsSource = giaoDichHienThi;

                CapNhatThongTinSoLuong();
            });
        }

        #endregion

        #region Database Functions

        private void TaiDanhSachGiaoDich()
        {
            try
            {
                Debug.WriteLine("=== BẮT ĐẦU TẢI DỮ LIỆU ===");

                // Hiển thị loading
                LoadingIndicator.Visibility = Visibility.Visible;

                // Reset danh sách
                tatCaGiaoDich.Clear();
                giaoDichHienThi.Clear();
                danhSachGoc.Clear();

                using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                {
                    conn.Open();

                    string sql = @"SELECT 
                                gd.MaGD,
                                gd.MaHV,
                                hv.HoTen,
                                gd.MaGoi,
                                gt.TenGoi,
                                gd.MaTK,
                                gd.TongTien,
                                gd.DaThanhToan,
                                gd.SoTienNo,
                                gd.NgayGD,
                                gd.TrangThai
                                FROM GiaoDich gd
                                INNER JOIN HocVien hv ON gd.MaHV = hv.MaHV
                                LEFT JOIN GoiTap gt ON gd.MaGoi = gt.MaGoi
                                ORDER BY gd.NgayGD DESC";

                    Debug.WriteLine("SQL: " + sql);

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read())
                        {
                            count++;
                            // Xử lý ngày tháng
                            DateTime ngayGD;
                            string ngayGDString = reader["NgayGD"].ToString();

                            if (string.IsNullOrEmpty(ngayGDString) || !DateTime.TryParse(ngayGDString, out ngayGD))
                            {
                                ngayGD = DateTime.Now;
                            }

                            var item = new MoDonDuLieuGiaoDich
                            {
                                MaGD = reader["MaGD"]?.ToString() ?? "Không xác định",
                                MaHV = reader["MaHV"]?.ToString() ?? "Không xác định",
                                HoTen = reader["HoTen"]?.ToString() ?? "Không xác định",
                                MaGoi = reader["MaGoi"]?.ToString() ?? "Không xác định",
                                TenGoi = reader["TenGoi"]?.ToString() ?? "Không xác định",
                                MaTK = reader["MaTK"]?.ToString() ?? "Không xác định",
                                TongTien = reader["TongTien"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TongTien"]),
                                DaThanhToan = reader["DaThanhToan"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DaThanhToan"]),
                                SoTienNo = reader["SoTienNo"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SoTienNo"]),
                                NgayGD = ngayGD,
                                TrangThai = reader["TrangThai"]?.ToString() ?? "Không xác định",
                                IsSelected = false
                            };

                            Debug.WriteLine($"Giao dịch {count}: MaGD={item.MaGD}, TrangThai={item.TrangThai}");

                            danhSachGoc.Add(item);
                        }

                        Debug.WriteLine($"Tổng số bản ghi đọc được: {count}");
                    }
                }

                Debug.WriteLine($"Tổng số giao dịch trong danhSachGoc: {danhSachGoc.Count}");

                // Gán danh sách gốc cho danh sách tổng hiện tại
                tatCaGiaoDich = new List<MoDonDuLieuGiaoDich>(danhSachGoc);

                // Đánh dấu đã load xong
                _isInitialLoadComplete = true;

                // ÁP DỤNG BỘ LỌC MẶC ĐỊNH NGAY SAU KHI LOAD XONG
                ApDungBoLocVaTimKiem();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LỖI: {ex.Message}");
                MessageBox.Show($"Lỗi khi tải danh sách giao dịch:\n{ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Ẩn loading
                LoadingIndicator.Visibility = Visibility.Collapsed;
                Debug.WriteLine("=== KẾT THÚC TẢI DỮ LIỆU ===");
            }
        }

        #endregion

        #region Search và Filter Functions

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.Black;
            }
            // Ẩn placeholder khi focus
            SearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                SearchTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
                // Hiện placeholder khi mất focus và không có nội dung
                SearchPlaceholder.Visibility = Visibility.Visible;
            }
        }

        // THÊM: Sự kiện TextChanged để xử lý ẩn/hiện placeholder
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Hiện/ẩn placeholder dựa trên nội dung TextBox
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text) || SearchTextBox.Text == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                SearchPlaceholder.Visibility = Visibility.Visible;
            }
            else
            {
                SearchPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ThucHienTimKiem();
            }
        }

        private void ThucHienTimKiem_Click(object sender, RoutedEventArgs e)
        {
            ThucHienTimKiem();
        }

        private void ThucHienTimKiem()
        {
            if (SearchTextBox == null || dgGiaoDich == null) return;

            string tuKhoa = SearchTextBox.Text.Trim();

            // Bỏ qua nếu đang hiển thị placeholder hoặc rỗng
            if (string.IsNullOrEmpty(tuKhoa) || tuKhoa == VAN_BAN_TIM_KIEM_MAC_DINH)
            {
                _tuKhoaTimKiem = "";
                ApDungBoLocVaTimKiem();
                return;
            }

            _tuKhoaTimKiem = tuKhoa;
            ApDungBoLocVaTimKiem();
        }

        private void ApDungBoLocVaTimKiem()
        {
            Debug.WriteLine("=== ÁP DỤNG BỘ LỌC VÀ TÌM KIẾM ===");
            Debug.WriteLine($"Trạng thái hiện tại: {_trangThaiHienTai}");
            Debug.WriteLine($"Từ khóa tìm kiếm: {_tuKhoaTimKiem}");
            Debug.WriteLine($"Số bản ghi trong danhSachGoc: {danhSachGoc.Count}");

            // Bắt đầu từ danh sách gốc
            var ketQua = danhSachGoc.AsEnumerable();

            // Áp dụng bộ lọc trạng thái
            if (!string.IsNullOrEmpty(_trangThaiHienTai))
            {
                string trangThaiCanLoc = _trangThaiHienTai;

                // SỬA QUAN TRỌNG: Ánh xạ chính xác giá trị từ RadioButton sang giá trị trong database
                switch (_trangThaiHienTai)
                {
                    case "Chưa thanh toán":
                        trangThaiCanLoc = "Chưa Thanh Toán";
                        break;
                    case "Thanh toán một phần":
                        trangThaiCanLoc = "Trả Một Phần";
                        break;
                    case "Đã thanh toán":
                        trangThaiCanLoc = "Đã Thanh Toán";
                        break;
                    default:
                        trangThaiCanLoc = null;
                        break;
                }

                Debug.WriteLine($"Trạng thái cần lọc: {trangThaiCanLoc}");

                if (!string.IsNullOrEmpty(trangThaiCanLoc))
                {
                    var truocKhiLoc = ketQua.Count();
                    // SỬA: So sánh không phân biệt hoa thường
                    ketQua = ketQua.Where(gd =>
                        gd.TrangThai.Equals(trangThaiCanLoc, StringComparison.OrdinalIgnoreCase));
                    var sauKhiLoc = ketQua.Count();

                    Debug.WriteLine($"Số bản ghi trước/sau khi lọc: {truocKhiLoc} -> {sauKhiLoc}");
                }
            }

            // Áp dụng tìm kiếm
            if (!string.IsNullOrEmpty(_tuKhoaTimKiem))
            {
                string tuKhoaKhongDau = BoQuyenDau(_tuKhoaTimKiem).ToLower();
                ketQua = ketQua.Where(gd =>
                    BoQuyenDau(gd.HoTen).ToLower().Contains(tuKhoaKhongDau) ||
                    gd.MaHV.ToLower().Contains(_tuKhoaTimKiem.ToLower()) ||
                    gd.MaGD.ToLower().Contains(_tuKhoaTimKiem.ToLower()) ||
                    BoQuyenDau(gd.TenGoi).ToLower().Contains(tuKhoaKhongDau) ||
                    gd.MaTK.ToLower().Contains(_tuKhoaTimKiem.ToLower())
                );
            }

            // Cập nhật danh sách hiển thị
            tatCaGiaoDich = ketQua.ToList();
            Debug.WriteLine($"Số bản ghi sau khi lọc: {tatCaGiaoDich.Count}");

            _trangHienTai = 1;
            HienThiDuLieuPhanTrang();
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

        #region Filter Functions - Radio Buttons

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Chỉ xử lý khi đã load xong dữ liệu
            if (!_isInitialLoadComplete) return;

            var radioButton = sender as RadioButton;
            if (radioButton == null) return;

            string trangThaiLoc = radioButton.Content.ToString();
            _trangThaiHienTai = trangThaiLoc;

            Debug.WriteLine($"Radio button được chọn: {trangThaiLoc}");
            ApDungBoLocVaTimKiem();
        }

        #endregion

        #region Phân trang Events

        private void cboSoBanGhiMoiTrang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSoBanGhiMoiTrang.SelectedItem != null)
            {
                _soBanGhiMoiTrang = (int)cboSoBanGhiMoiTrang.SelectedItem;
                _trangHienTai = 1;
                HienThiDuLieuPhanTrang();
            }
        }

        private void btnTrangDau_Click(object sender, RoutedEventArgs e)
        {
            _trangHienTai = 1;
            HienThiDuLieuPhanTrang();
        }

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

        private void btnTrangCuoi_Click(object sender, RoutedEventArgs e)
        {
            _trangHienTai = _tongSoTrang;
            HienThiDuLieuPhanTrang();
        }

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
                // Giữ lại số trang hiện tại trong textbox sau khi chuyển trang
                if (txtChuyenTrang.Text != _trangHienTai.ToString())
                {
                    txtChuyenTrang.Text = _trangHienTai.ToString();
                }
                // Di chuyển focus để loại bỏ con trỏ nhấp nháy
                Keyboard.ClearFocus();
            }
        }

        #endregion

        #region Button Commands

        private void RefreshCommand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Reset ô tìm kiếm
                if (SearchTextBox != null)
                {
                    SearchTextBox.Text = VAN_BAN_TIM_KIEM_MAC_DINH;
                    SearchTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
                    _tuKhoaTimKiem = "";
                    // Hiện placeholder khi reset
                    SearchPlaceholder.Visibility = Visibility.Visible;
                }

                // Tải lại dữ liệu (sẽ tự động kích hoạt bộ lọc mặc định)
                _isInitialLoadComplete = false;
                TaiDanhSachGiaoDich();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi làm mới dữ liệu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTransactionCommand_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Mở form thêm giao dịch mới", "Thêm giao dịch",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteTransactionCommand_Click(object sender, RoutedEventArgs e)
        {
            var giaoDichDaChon = giaoDichHienThi.Where(gd => gd.IsSelected).ToList();

            if (giaoDichDaChon.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một giao dịch để xóa!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Bạn có chắc muốn xóa {giaoDichDaChon.Count} giao dịch đã chọn?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqliteConnection conn = new SqliteConnection(chuoiKetNoi))
                    {
                        conn.Open();

                        foreach (var gd in giaoDichDaChon)
                        {
                            string sql = "DELETE FROM GiaoDich WHERE MaGD = @MaGD";
                            using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@MaGD", gd.MaGD);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    MessageBox.Show($"Đã xóa {giaoDichDaChon.Count} giao dịch thành công!", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Tải lại dữ liệu sau khi xóa
                    _isInitialLoadComplete = false;
                    TaiDanhSachGiaoDich();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa giao dịch:\n{ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportTransactionCommand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fileName = $"DanhSachGiaoDich_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                MessageBox.Show($"Đã xuất danh sách giao dịch ra file: {fileName}", "Xuất dữ liệu",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất dữ liệu:\n{ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewTransactionCommand_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var giaoDich = button?.DataContext as MoDonDuLieuGiaoDich;

            if (giaoDich != null)
            {
                MessageBox.Show($"Xem chi tiết giao dịch: {giaoDich.MaGD}\nHọc viên: {giaoDich.HoTen}\nTổng tiền: {giaoDich.TongTien:N0} VNĐ",
                    "Chi tiết giao dịch", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Selection Management

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.DataContext is MoDonDuLieuGiaoDich giaoDich)
            {
                giaoDich.IsSelected = true;
            }
            CapNhatThongTinSoLuong();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.DataContext is MoDonDuLieuGiaoDich giaoDich)
            {
                giaoDich.IsSelected = false;
            }
            CapNhatThongTinSoLuong();
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            bool isChecked = checkBox?.IsChecked ?? false;

            foreach (var giaoDich in giaoDichHienThi)
            {
                giaoDich.IsSelected = isChecked;
            }

            dgGiaoDich.Items.Refresh();
            CapNhatThongTinSoLuong();
        }

        private void CapNhatThongTinSoLuong()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (txtSoGiaoDichHienThi != null)
                    txtSoGiaoDichHienThi.Text = $"Đang hiển thị: {giaoDichHienThi.Count} giao dịch";

                if (txtSoGiaoDichDaChon != null)
                {
                    int soLuongDaChon = giaoDichHienThi.Count(gd => gd.IsSelected);
                    txtSoGiaoDichDaChon.Text = $"Đã chọn: {soLuongDaChon}";
                    txtSoGiaoDichDaChon.Visibility = soLuongDaChon > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            });
        }

        #endregion

        #region Context Menu

        private void MenuItem_Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshCommand_Click(sender, e);
        }

        private void MenuItem_AddTransaction_Click(object sender, RoutedEventArgs e)
        {
            AddTransactionCommand_Click(sender, e);
        }

        #endregion
    }

    public class MoDonDuLieuGiaoDich
    {
        public string MaGD { get; set; }
        public string MaHV { get; set; }
        public string HoTen { get; set; }
        public string MaGoi { get; set; }
        public string TenGoi { get; set; }
        public string MaTK { get; set; }
        public decimal TongTien { get; set; }
        public decimal DaThanhToan { get; set; }
        public decimal SoTienNo { get; set; }
        public DateTime NgayGD { get; set; }
        public string TrangThai { get; set; }
        public bool IsSelected { get; set; }

        // Format properties for display
        public string TongTienFormatted => TongTien == 0 ? "" : $"{TongTien:N0} VNĐ";
        public string DaThanhToanFormatted => DaThanhToan == 0 ? "" : $"{DaThanhToan:N0} VNĐ";
        public string SoTienNoFormatted => SoTienNo == 0 ? "" : $"{SoTienNo:N0} VNĐ";
        public string NgayGDFormatted => NgayGD.ToString("dd/MM/yyyy HH:mm");
    }

    #region Converter Classes

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString();
            switch (status)
            {
                case "Trả Một Phần":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                case "Chưa Thanh Toán":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                case "Đã Thanh Toán":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                default:
                    return new SolidColorBrush(Colors.Gray);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}