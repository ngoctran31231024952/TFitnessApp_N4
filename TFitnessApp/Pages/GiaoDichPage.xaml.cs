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
using System.Data;
using ClosedXML.Excel;
using TFitnessApp.Windows;
using System.Windows.Controls.Primitives;
using TFitnessApp.Database;
using static TFitnessApp.GiaoDichPage;

namespace TFitnessApp
{
    public partial class GiaoDichPage : Page
    {
        #region Trường Dữ liệu Nội bộ
        private string _ChuoiKetNoi;
        private readonly TruyCapDB _dbAccess;
        private List<MoDonDuLieuGiaoDich> tatCaGiaoDich = new List<MoDonDuLieuGiaoDich>();
        private List<MoDonDuLieuGiaoDich> danhSachGoc = new List<MoDonDuLieuGiaoDich>();
        private ObservableCollection<MoDonDuLieuGiaoDich> giaoDichHienThi = new ObservableCollection<MoDonDuLieuGiaoDich>();

        // Khai báo hằng số
        private const string VAN_BAN_TIM_KIEM_MAC_DINH = "Tìm kiếm học viên, mã giao dịch, gói tập...";

        // Định dạng ngày giờ để Parse
        private static readonly string[] RobustDateFormats = new string[]
        {
            "dd/MM/yyyy HH:mm",  
        };

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

        // Biến lưu trạng thái Bộ lọc tùy chỉnh
        private MoDonBoLoc _boLocTuyChinh = new MoDonBoLoc();

        public enum KhoangTongTien
        {
            KhongChon,
            Duoi3Trieu,
            Tu3Den5Trieu,
            Tren5Trieu
        }
        #endregion

        #region Khởi tạo
        public GiaoDichPage()
        {
            InitializeComponent();

            // Khởi tạo đối tượng DbAccess
            _dbAccess = new TruyCapDB();
            // Lấy chuỗi kết nối
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;

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
        #endregion

        #region Khởi tạo và Hiển thị Dữ liệu
        // Khởi tạo ComboBox chọn số lượng bản ghi mỗi trang
        private void KhoiTaoComboBoxSoBanGhi()
        {
            if (cboSoBanGhiMoiTrang != null)
            {
                cboSoBanGhiMoiTrang.ItemsSource = new List<int> { 10, 20, 50, 100 };
                cboSoBanGhiMoiTrang.SelectedItem = _soBanGhiMoiTrang;
            }
        }

        // Cập nhật các biến phân trang (tổng số bản ghi, tổng số trang)
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

        // Cập nhật trạng thái các nút phân trang và thông tin hiển thị
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

        // Hiển thị dữ liệu lên DataGrid theo trang hiện tại
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

                // Sau khi hiển thị dữ liệu mới, cần kiểm tra và cập nhật lại trạng thái Select All
                CapNhatTrangThaiSelectAll();

                CapNhatThongTinSoLuong();
            });
        }
        #endregion

        #region Database Functions
        // Tải toàn bộ danh sách giao dịch từ database
        private void TaiDanhSachGiaoDich()
        {
            try
            {
                // Reset danh sách
                tatCaGiaoDich.Clear();
                giaoDichHienThi.Clear();
                danhSachGoc.Clear();

                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
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

                            DateTime ngayGD = DateTime.MinValue;
                            string ngayGDString = reader["NgayGD"].ToString();

                            // Dùng TryParseExact thay cho TryParse
                            if (!string.IsNullOrEmpty(ngayGDString) &&
                            DateTime.TryParseExact(ngayGDString, RobustDateFormats,
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out ngayGD))
                            {
                                // Parse thành công
                            }
                            else
                            {
                                // Nếu thất bại, gán DateTime.MinValue và ghi log
                                ngayGD = DateTime.MinValue;
                                Debug.WriteLine($"KHÔNG THỂ PARSE NGÀY GIỜ: {ngayGDString}");
                            }

                            // Tạo đối tượng dữ liệu
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
                            danhSachGoc.Add(item);
                        }
                    }
                }

                // Gán danh sách gốc cho danh sách tổng hiện tại
                tatCaGiaoDich = new List<MoDonDuLieuGiaoDich>(danhSachGoc);

                // Đánh dấu đã load xong
                _isInitialLoadComplete = true;

                // ÁP DỤNG BỘ LỌC VÀ TÌM KIẾM MẶC ĐỊNH NGAY SAU KHI LOAD XONG
                ApDungBoLocVaTimKiem();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LỖI: {ex.Message}");
                MessageBox.Show($"Lỗi khi tải danh sách giao dịch:\n{ex.Message}",
                 "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Xử lý sự kiện tìm kiếm trên thanh tìm kiếm 
        // Xử lý khi TextBox tìm kiếm nhận focus
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

        // Xử lý khi TextBox tìm kiếm mất focus
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

        // Sự kiện TextChanged để xử lý ẩn/hiện placeholder
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

        // Xử lý sự kiện Enter trên TextBox tìm kiếm
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ThucHienTimKiem();
            }
        }

        // Xử lý sự kiện click nút tìm kiếm
        private void ThucHienTimKiem_Click(object sender, RoutedEventArgs e)
        {
            ThucHienTimKiem();
        }

        // Phương thức thực hiện tìm kiếm và cập nhật từ khóa
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

        // Phương thức áp dụng bộ lọc trạng thái và từ khóa tìm kiếm
        private void ApDungBoLocVaTimKiem()
        {
            // Bắt đầu từ danh sách gốc
            var ketQua = danhSachGoc.AsEnumerable();

            // Áp dụng bộ lọc trạng thái
            if (!string.IsNullOrEmpty(_trangThaiHienTai))
            {
                string trangThaiCanLoc = _trangThaiHienTai;

                // Ánh xạ giá trị từ RadioButton sang giá trị trong database
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

                if (!string.IsNullOrEmpty(trangThaiCanLoc))
                {
                    var truocKhiLoc = ketQua.Count();
                    // Lọc theo trạng thái, không phân biệt hoa thường
                    ketQua = ketQua.Where(gd =>
      gd.TrangThai.Equals(trangThaiCanLoc, StringComparison.OrdinalIgnoreCase));
                    var sauKhiLoc = ketQua.Count();

                }
            }

            // Áp dụng tìm kiếm - TÌM KIẾM TRONG TẤT CẢ CÁC TRƯỜNG HIỂN THỊ
            if (!string.IsNullOrEmpty(_tuKhoaTimKiem))
            {
                string tuKhoaKhongDau = BoQuyenDau(_tuKhoaTimKiem).ToLower();
                ketQua = ketQua.Where(gd =>
                 BoQuyenDau(gd.HoTen).ToLower().Contains(tuKhoaKhongDau) ||
                 gd.MaHV.ToLower().Contains(_tuKhoaTimKiem.ToLower()) ||
                 gd.MaGD.ToLower().Contains(_tuKhoaTimKiem.ToLower()) ||
                 BoQuyenDau(gd.TenGoi).ToLower().Contains(tuKhoaKhongDau) ||
                 gd.MaTK.ToLower().Contains(_tuKhoaTimKiem.ToLower()) ||
                 gd.TongTienFormatted.ToLower().Contains(_tuKhoaTimKiem.ToLower()) ||
                 gd.DaThanhToanFormatted.ToLower().Contains(_tuKhoaTimKiem.ToLower()) ||
                 gd.SoTienNoFormatted.ToLower().Contains(_tuKhoaTimKiem.ToLower()) ||
                 gd.NgayGDFormatted.ToLower().Contains(_tuKhoaTimKiem.ToLower()) ||
                 gd.TrangThai.ToLower().Contains(_tuKhoaTimKiem.ToLower())
                );
            }

            // === Áp dụng bộ lọc tùy chỉnh từ Window5 ===
            if (_boLocTuyChinh != null && _boLocTuyChinh.IsActive())
            {
                // Lọc theo Mã Học Viên 
                if (!string.IsNullOrWhiteSpace(_boLocTuyChinh.LocMaHV))
                {
                    string locMaHV = _boLocTuyChinh.LocMaHV.ToLower();
                    ketQua = ketQua.Where(gd => gd.MaHV.ToLower().Contains(locMaHV));
                }

                // Lọc theo Mã Gói Tập 
                if (!string.IsNullOrWhiteSpace(_boLocTuyChinh.LocMaGoi))
                {
                    string locMaGoi = _boLocTuyChinh.LocMaGoi.ToLower();
                    ketQua = ketQua.Where(gd => gd.MaGoi.ToLower().Contains(locMaGoi));
                }

                // Lọc theo Mã Nhân Viên 
                if (!string.IsNullOrWhiteSpace(_boLocTuyChinh.LocMaNV))
                {
                    string locMaNV = _boLocTuyChinh.LocMaNV.ToLower();
                    ketQua = ketQua.Where(gd => gd.MaTK.ToLower().Contains(locMaNV));
                }

                // Lọc theo Ngày Giao Dịch
                if (_boLocTuyChinh.TuNgay.HasValue)
                {
                    ketQua = ketQua.Where(gd => gd.NgayGD >= _boLocTuyChinh.TuNgay.Value);
                }

                if (_boLocTuyChinh.DenNgay.HasValue)
                {
                    // Đảm bảo lọc đến cuối ngày
                    DateTime denNgayCuoi = _boLocTuyChinh.DenNgay.Value.Date.AddDays(1).AddSeconds(-1);
                    ketQua = ketQua.Where(gd => gd.NgayGD <= denNgayCuoi);
                }

                // Lọc theo Tổng Tiền
                if (_boLocTuyChinh.KhoangTongTienDuocChon != KhoangTongTien.KhongChon)
                {
                    switch (_boLocTuyChinh.KhoangTongTienDuocChon)
                    {
                        case KhoangTongTien.Duoi3Trieu:
                            ketQua = ketQua.Where(gd => gd.TongTien < 3000000);
                            break;
                        case KhoangTongTien.Tu3Den5Trieu:
                            ketQua = ketQua.Where(gd => gd.TongTien >= 3000000 && gd.TongTien <= 5000000);
                            break;
                        case KhoangTongTien.Tren5Trieu:
                            ketQua = ketQua.Where(gd => gd.TongTien > 5000000);
                            break;
                    }
                }
            }

            // Cập nhật danh sách hiển thị
            tatCaGiaoDich = ketQua.ToList();
            Debug.WriteLine($"Số bản ghi sau khi lọc: {tatCaGiaoDich.Count}");

            _trangHienTai = 1;
            HienThiDuLieuPhanTrang();
        }

        // Loại bỏ dấu tiếng Việt để hỗ trợ tìm kiếm không dấu
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

        #region Xử lý sự kiện chuyển tab trạng thái 
        // Sự kiện khi một RadioButton trạng thái thanh toán được chọn
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
        // Xử lý khi thay đổi số bản ghi hiển thị mỗi trang
        private void cboSoBanGhiMoiTrang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSoBanGhiMoiTrang.SelectedItem != null)
            {
                _soBanGhiMoiTrang = (int)cboSoBanGhiMoiTrang.SelectedItem;
                _trangHienTai = 1;
                HienThiDuLieuPhanTrang();
            }
        }

        // Chuyển đến trang đầu tiên
        private void btnTrangDau_Click(object sender, RoutedEventArgs e)
        {
            _trangHienTai = 1;
            HienThiDuLieuPhanTrang();
        }

        // Chuyển đến trang trước
        private void btnTrangTruoc_Click(object sender, RoutedEventArgs e)
        {
            if (_trangHienTai > 1)
            {
                _trangHienTai--;
                HienThiDuLieuPhanTrang();
            }
        }

        // Chuyển đến trang sau
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

        // Xử lý sự kiện nhấn Enter trong TextBox chuyển trang
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
        // Xử lý sự kiện nhấn nút Làm mới / Menu Item Làm mới
        private void LamMoiGiaoDich_Click (object sender, RoutedEventArgs e)
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

        // Xử lý sự kiện nhấn nút Tạo giao dịch mới / Menu Item Tạo giao dịch mới
        private void YeuCauTaoGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Mở cửa sổ Thêm Giao Dịch dạng modal
                Window1 themGiaoDichWindow = new Window1();
                themGiaoDichWindow.Owner = Window.GetWindow(this);
                themGiaoDichWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                themGiaoDichWindow.ShowDialog();

                // Refresh dữ liệu sau khi thêm giao dịch
                _isInitialLoadComplete = false;
                TaiDanhSachGiaoDich();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ thêm giao dịch: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Button YeuCauLocGiaoDich 
        private void YeuCauLocGiaoDich_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Tạo cửa sổ lọc mới, TRUYỀN TRẠNG THÁI LỌC HIỆN TẠI VÀO
                var filterWindow = new Window5(_boLocTuyChinh);
                filterWindow.Owner = Window.GetWindow(this);
                filterWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // 2. Mở cửa sổ dưới dạng Modal và nhận kết quả
                bool? result = filterWindow.ShowDialog();

                // 3. Xử lý kết quả trả về từ cửa sổ lọc
                if (result == true)
                {
                    // Cập nhật trạng thái bộ lọc TỪ cửa sổ
                    _boLocTuyChinh = filterWindow.KetQuaLoc;

                    // Áp dụng bộ lọc mới lên danh sách dữ liệu
                    ApDungBoLocVaTimKiem();
                }
                else
                {
                    // Người dùng đã hủy
                    Debug.WriteLine("Người dùng đã hủy bộ lọc");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tìm kiếm: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        // Xử lý sự kiện nhấn nút Xóa giao dịch đã chọn
        private void YeuCauXoaGiaoDich_Click(object sender, RoutedEventArgs e)
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
                    using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
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

        // Xử lý sự kiện nhấn nút Xuất Excel
        private void YeuCauXuatGiaoDich_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                var giaoDichDaChon = giaoDichHienThi.Where(gd => gd.IsSelected).ToList();

                if (giaoDichDaChon.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một giao dịch để xuất!", "Thông báo",
                     MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Hiển thị hộp thoại lưu file
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"DanhSachGiaoDich_{DateTime.Now:ddMMyyyy_HH:mm}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    XuatExcel(giaoDichDaChon, saveFileDialog.FileName);
                    MessageBox.Show($"Đã xuất {giaoDichDaChon.Count} giao dịch ra file:\n{saveFileDialog.FileName}",
                     "Xuất dữ liệu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất dữ liệu:\n{ex.Message}", "Lỗi",
                 MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Phương thức xuất dữ liệu ra file Excel (.xlsx) bằng ClosedXML
        private void XuatExcel(List<MoDonDuLieuGiaoDich> danhSachGiaoDich, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Danh sách giao dịch");

                // Tạo header
                worksheet.Cell(1, 1).Value = "Mã GD";
                worksheet.Cell(1, 2).Value = "Mã HV";
                worksheet.Cell(1, 3).Value = "Họ tên";
                worksheet.Cell(1, 4).Value = "Mã Gói";
                worksheet.Cell(1, 5).Value = "Tên gói";
                worksheet.Cell(1, 6).Value = "Mã TK";
                worksheet.Cell(1, 7).Value = "Tổng tiền";
                worksheet.Cell(1, 8).Value = "Đã thanh toán";
                worksheet.Cell(1, 9).Value = "Số tiền nợ";
                worksheet.Cell(1, 10).Value = "Ngày GD";
                worksheet.Cell(1, 11).Value = "Trạng thái";

                // Định dạng header
                var headerRange = worksheet.Range(1, 1, 1, 11);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Điền dữ liệu
                int row = 2;
                foreach (var gd in danhSachGiaoDich)
                {
                    worksheet.Cell(row, 1).Value = gd.MaGD;
                    worksheet.Cell(row, 2).Value = gd.MaHV;
                    worksheet.Cell(row, 3).Value = gd.HoTen;
                    worksheet.Cell(row, 4).Value = gd.MaGoi;
                    worksheet.Cell(row, 5).Value = gd.TenGoi;
                    worksheet.Cell(row, 6).Value = gd.MaTK;
                    worksheet.Cell(row, 7).Value = gd.TongTien;
                    worksheet.Cell(row, 8).Value = gd.DaThanhToan;
                    worksheet.Cell(row, 9).Value = gd.SoTienNo;
                    worksheet.Cell(row, 10).Value = gd.NgayGD;
                    worksheet.Cell(row, 11).Value = gd.TrangThai;
                    row++;
                }

                // Tự động điều chỉnh độ rộng cột
                worksheet.Columns().AdjustToContents();

                // Lưu file
                workbook.SaveAs(filePath);
            }
        }

        // Xử lý sự kiện nhấn nút Xem chi tiết giao dịch
        private void XemChiTietGiaoDich_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null && button.DataContext is MoDonDuLieuGiaoDich transaction)
                {
                    // Mở cửa sổ Xem Giao Dịch dạng modal
                    Window2 xemGiaoDichWindow = new Window2(transaction);
                    xemGiaoDichWindow.Owner = Window.GetWindow(this);
                    xemGiaoDichWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    xemGiaoDichWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn một giao dịch để xem chi tiết.", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ xem giao dịch: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Selection Management
        // Cập nhật trạng thái của CheckBox "Chọn tất cả" dựa trên các CheckBox con
        private void CapNhatTrangThaiSelectAll()
        {
            if (dgGiaoDich == null) return;

            // Tìm CheckBox "Chọn tất cả" trong Header của DataGrid
            var ChonHetCheckBox = FindVisualChild<CheckBox>(dgGiaoDich, "chkSelectAll");

            if (ChonHetCheckBox != null)
            {
                // Tính toán trạng thái
                int totalItems = giaoDichHienThi.Count;
                int selectedItems = giaoDichHienThi.Count(gd => gd.IsSelected);

                if (totalItems > 0 && selectedItems == totalItems)
                {
                    // Chọn tất cả
                    ChonHetCheckBox.IsChecked = true;
                }
                else if (selectedItems > 0)
                {
                    // Chọn một phần (Indeterminate)
                    ChonHetCheckBox.IsChecked = null;
                }
                else
                {
                    // Không chọn gì
                    ChonHetCheckBox.IsChecked = false;
                }
            }
        }

        // Helper method để tìm visual child theo type và name
        private T FindVisualChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                {
                    // Nếu không có tên cụ thể, trả về kết quả đầu tiên
                    if (string.IsNullOrEmpty(childName))
                        return result;

                    // Nếu có FrameworkElement và tên khớp
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                        return result;
                }

                var childResult = FindVisualChild<T>(child, childName);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        // Xử lý khi CheckBox của một dòng được chọn
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.DataContext is MoDonDuLieuGiaoDich giaoDich)
            {
                giaoDich.IsSelected = true;
                // Đồng bộ hóa trạng thái chọn hàng của DataGrid.
                if (dgGiaoDich.SelectedItem == null || dgGiaoDich.SelectedItem != giaoDich)
                {
                    dgGiaoDich.SelectedItem = giaoDich;
                }
            }
            CapNhatTrangThaiSelectAll(); 
            CapNhatThongTinSoLuong();
        }

        // Xử lý khi CheckBox của một dòng bị bỏ chọn
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.DataContext is MoDonDuLieuGiaoDich giaoDich)
            {
                giaoDich.IsSelected = false;

                // Đồng bộ hóa trạng thái chọn hàng của DataGrid.
                if (dgGiaoDich.SelectedItem == giaoDich)
                {
                    dgGiaoDich.SelectedItem = null;
                }
            }
            CapNhatTrangThaiSelectAll(); 
            CapNhatThongTinSoLuong();
        }

        // Xử lý khi CheckBox "Chọn tất cả" được nhấn
        private void ChonHetCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            // Xác định trạng thái mới (True cho Checked, False cho Unchecked)
            bool isChecked = checkBox?.IsChecked ?? false;

            // Xử lý logic chọn/bỏ chọn tất cả trên DataGrid hiện tại
            foreach (var giaoDich in giaoDichHienThi)
            {
                giaoDich.IsSelected = isChecked;
            }

            dgGiaoDich.Items.Refresh();
            CapNhatThongTinSoLuong();
        }

        // Cập nhật thông tin số lượng giao dịch hiển thị và đã chọn ở Footer
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
        // Xử lý sự kiện nhấn Menu Item "Làm mới"
        private void MenuItem_LamMoi_Click(object sender, RoutedEventArgs e)
        {
            LamMoiGiaoDich_Click(sender, e);
        }

        // Xử lý sự kiện nhấn Menu Item "Tạo giao dịch mới"
        private void MenuItem_TaoMoi_Click(object sender, RoutedEventArgs e)
        {
            YeuCauTaoGiaoDich_Click(sender, e);
        }
        #endregion
    }

    // Lớp mô hình dữ liệu cho giao dịch hiển thị trên DataGrid
    public class MoDonDuLieuGiaoDich
    {
        // Các thuộc tính cơ bản
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

        // Các thuộc tính tính toán để format dữ liệu hiển thị (tiền tệ)
        public string TongTienFormatted => $"{TongTien:N0} VNĐ";
        public string DaThanhToanFormatted => $"{DaThanhToan:N0} VNĐ";
        public string SoTienNoFormatted => $"{SoTienNo:N0} VNĐ";

        // Thuộc tính tính toán để format ngày giao dịch
        public string NgayGDFormatted
        {
            get
            {
                // Trả về "Không xác định" nếu ngày không hợp lệ (DateTime.MinValue)
                if (NgayGD == DateTime.MinValue)
                    return "Không xác định";
                // Format ngày theo định dạng dd/MM/yyyy HH:mm
                return NgayGD.ToString("dd/MM/yyyy HH:mm");
            }
        }
    }
    public class MoDonBoLoc
    {
        // Lọc theo Mã Định Danh
        public string LocMaHV { get; set; } = string.Empty;
        public string LocMaGoi { get; set; } = string.Empty;
        public string LocMaNV { get; set; } = string.Empty; 

        // Lọc theo Ngày Giao Dịch
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }

        // Lọc theo Tổng Tiền 
        public KhoangTongTien KhoangTongTienDuocChon { get; set; } = KhoangTongTien.KhongChon;

        // Lưu giá trị Min/Max đã được tính toán từ KhoangTongTienDuocChon 
        public decimal? TuTongTien { get; set; }
        public decimal? DenTongTien { get; set; }

        // Kiểm tra xem có bất kỳ điều kiện lọc nào được áp dụng không
        public bool IsActive()
        {
            return !string.IsNullOrWhiteSpace(LocMaHV) ||
             !string.IsNullOrWhiteSpace(LocMaGoi) ||
             !string.IsNullOrWhiteSpace(LocMaNV) ||
             TuNgay.HasValue ||
             DenNgay.HasValue ||
             KhoangTongTienDuocChon != KhoangTongTien.KhongChon;
        }
    }
}