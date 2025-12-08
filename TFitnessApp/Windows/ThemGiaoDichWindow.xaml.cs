using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using TFitnessApp.Database;

namespace TFitnessApp.Windows
{
    // Lớp Window1 dùng để tạo mới giao dịch
    public partial class Window1 : Window, INotifyPropertyChanged
    {
        #region Trường Dữ liệu Nội bộ
        private readonly TruyCapDB _dbAccess;
        private string _maHocVien;
        private string _hoTenHocVien;
        private string _maGoi;
        private string _tenGoiTap;
        private string _maNhanVien;
        private string _hoTenNhanVien;
        private decimal _tongTien;
        private decimal _daThanhToan;
        private decimal _soTienNo;
        private string _phuongThucThanhToan = "Tiền mặt";
        private string _trangThaiThanhToan = "Chưa Thanh Toán";
        private DateTime _ngayGiaoDich = DateTime.Now;

        // Danh sách MASTER (lưu trữ tất cả mã)
        private List<string> _allMaHocVien = new List<string>();
        private List<string> _allMaGoiTap = new List<string>();
        private List<string> _allMaNhanVien = new List<string>();

        // Danh sách cho ComboBox tìm kiếm (chỉ hiển thị các mục đã lọc)
        public ObservableCollection<string> DanhSachMaHocVien { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> DanhSachMaGoiTap { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> DanhSachMaNhanVien { get; set; } = new ObservableCollection<string>();
        #endregion

        #region Thuộc tính Binding (Public Properties)
        public string MaHocVien
        {
            get => _maHocVien;
            // Chỉ gọi hàm tải thông tin khi giá trị thực sự thay đổi 
            set
            {
                if (_maHocVien == value) return;
                _maHocVien = value;
                OnPropertyChanged(nameof(MaHocVien));
                // LƯU Ý: Hàm TaiThongTinHocVien() sẽ được gọi khi LostFocus hoặc khi chọn từ ComboBox
                TaiThongTinHocVien();
            }
        }

        public string HoTenHocVien
        {
            get => _hoTenHocVien;
            set { _hoTenHocVien = value; OnPropertyChanged(nameof(HoTenHocVien)); }
        }

        public string MaGoi
        {
            get => _maGoi;
            set
            {
                if (_maGoi == value) return;
                _maGoi = value;
                OnPropertyChanged(nameof(MaGoi));
                TaiThongTinGoiTap();
            }
        }

        public string TenGoiTap
        {
            get => _tenGoiTap;
            set { _tenGoiTap = value; OnPropertyChanged(nameof(TenGoiTap)); }
        }

        public string MaNhanVien
        {
            get => _maNhanVien;
            set
            {
                if (_maNhanVien == value) return;
                _maNhanVien = value;
                OnPropertyChanged(nameof(MaNhanVien));
                TaiThongTinNhanVien();
            }
        }

        public string HoTenNhanVien
        {
            get => _hoTenNhanVien;
            set { _hoTenNhanVien = value; OnPropertyChanged(nameof(HoTenNhanVien)); }
        }

        public decimal TongTien
        {
            get => _tongTien;
            // Tính lại số tiền nợ và trạng thái mỗi khi TongTien thay đổi
            set { _tongTien = value; OnPropertyChanged(nameof(TongTien)); TinhSoTienNoVaTrangThai(); }
        }

        public decimal DaThanhToan
        {
            get => _daThanhToan;
            // Tính lại số tiền nợ và trạng thái mỗi khi DaThanhToan thay đổi
            set { _daThanhToan = value; OnPropertyChanged(nameof(DaThanhToan)); TinhSoTienNoVaTrangThai(); }
        }

        public decimal SoTienNo
        {
            get => _soTienNo;
            set { _soTienNo = value; OnPropertyChanged(nameof(SoTienNo)); }
        }

        public string PhuongThucThanhToan
        {
            get => _phuongThucThanhToan;
            set { _phuongThucThanhToan = value; OnPropertyChanged(nameof(PhuongThucThanhToan)); }
        }

        public string TrangThaiThanhToan
        {
            get => _trangThaiThanhToan;
            // Thuộc tính này được tính toán, không cho phép set trực tiếp từ UI
            set { _trangThaiThanhToan = value; OnPropertyChanged(nameof(TrangThaiThanhToan)); }
        }

        // Ngày giao dịch luôn là ngày giờ hiện tại
        public DateTime NgayGiaoDich
        {
            get => _ngayGiaoDich;
            set { _ngayGiaoDich = value; OnPropertyChanged(nameof(NgayGiaoDich)); }
        }
        #endregion

        #region Khởi tạo
        public Window1()
        {
            InitializeComponent();

            // Khởi tạo đối tượng DbAccess
            _dbAccess = new TruyCapDB();
            this.DataContext = this;

            // Tải danh sách mã vào cả master list và ObservableCollection để hiển thị ban đầu
            TaiDanhSachMa("HocVien", "MaHV", _allMaHocVien, DanhSachMaHocVien);
            TaiDanhSachMa("GoiTap", "MaGoi", _allMaGoiTap, DanhSachMaGoiTap);
            TaiDanhSachMa("TaiKhoan", "MaTK", _allMaNhanVien, DanhSachMaNhanVien);

            // Ngày giao dịch được thiết lập mặc định là thời điểm hiện tại
            NgayGiaoDich = DateTime.Now;
        }
        #endregion

        #region Xử lý UI và Tải dữ liệu

        // Phương thức chung để tải danh sách mã vào List (Master) và ObservableCollection (Display)
        private void TaiDanhSachMa(string tableName, string columnName, List<string> masterList, ObservableCollection<string> displayList)
        {
            masterList.Clear();
            displayList.Clear();
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string query = $"SELECT {columnName} FROM {tableName}";
                    using (var command = new SqliteCommand(query, conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string ma = reader[columnName].ToString();
                                masterList.Add(ma);
                                displayList.Add(ma); // Thêm vào danh sách hiển thị ban đầu
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách mã từ bảng {tableName}: {ex.Message}");
            }
        }

        // Đóng cửa sổ
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Xử lý GotFocus để mở Dropdown (Xổ xuống khi nhấn vào) ---
        private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            // Chỉ mở dropdown nếu nó chưa mở
            if (comboBox != null && !comboBox.IsDropDownOpen)
            {
                comboBox.IsDropDownOpen = true;
            }
        }


        // --- Logic Tìm kiếm và Lọc cho ComboBox sử dụng KeyUp ---

        // Hàm hỗ trợ tìm TextBox nội bộ của ComboBox (để đặt CaretIndex)
        private TextBox TimTextBoxChinhSua (ComboBox comboBox)
        {
            if (comboBox.Template == null) return null;

            // Duyệt cây trực quan để tìm TextBox nội bộ (PART_EditableTextBox)
            DependencyObject child = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(comboBox); i++)
            {
                child = VisualTreeHelper.GetChild(comboBox, i);
                if (child is ContentPresenter)
                {
                    // Đôi khi TextBox nằm sâu hơn ContentPresenter
                    if (VisualTreeHelper.GetChildrenCount(child) > 0)
                    {
                        child = VisualTreeHelper.GetChild(child, 0);
                    }
                }
                // Tìm kiếm sâu hơn trong Template
                if (child is Decorator decorator)
                {
                    child = decorator.Child;
                }

                if (child is TextBox textBox) return textBox;
            }
            // Thử tìm bằng cách sử dụng GetTemplateChild nếu ComboBox đã được áp dụng Template
            var editor = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            if (editor != null) return editor;

            return null;
        }

        // Hàm chung để lọc dữ liệu và cập nhật ComboBox
        private void FilterComboBox(ComboBox comboBox, List<string> masterList, ObservableCollection<string> displayList)
        {
            // Lấy văn bản nhập từ ComboBox
            string filterText = comboBox.Text;

            // KIỂM TRA NULL ĐỂ TRÁNH LỖI CRASH: Đảm bảo filterText không null
            if (filterText == null)
            {
                filterText = string.Empty;
            }

            // Lấy vị trí con trỏ hiện tại (từ TextBox nội bộ) trước khi cập nhật ItemSource
            int caretIndex = filterText.Length;
            var editableTextBox = TimTextBoxChinhSua(comboBox);
            if (editableTextBox != null)
            {
                // Sử dụng try-catch để an toàn hơn khi truy cập CaretIndex trên UI Thread
                try
                {
                    caretIndex = editableTextBox.CaretIndex;
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi truy cập CaretIndex trong quá trình lọc nhanh
                    caretIndex = filterText.Length;
                }
            }

            // Lọc danh sách
            var filteredList = masterList.Where(ma => ma.ToLower().Contains(filterText.ToLower())).ToList();

            // Cập nhật ObservableCollection (Display List)
            displayList.Clear();
            foreach (var ma in filteredList)
            {
                displayList.Add(ma);
            }

            // Đặt lại Text (thường là cần thiết sau khi thao tác với ItemSource)
            comboBox.Text = filterText;

            // Đặt lại vị trí con trỏ (CaretIndex) để khắc phục lỗi nhảy con trỏ
            if (editableTextBox != null)
            {
                // Sử dụng try-catch khi đặt CaretIndex
                try
                {
                    // Đảm bảo chỉ đặt CaretIndex nếu nó hợp lệ
                    if (caretIndex >= 0 && caretIndex <= comboBox.Text.Length)
                    {
                        editableTextBox.CaretIndex = caretIndex;
                    }
                    else
                    {
                        editableTextBox.CaretIndex = comboBox.Text.Length;
                    }
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi nếu không thể đặt CaretIndex
                }
            }
            // Giữ cho dropdown mở khi gõ
            comboBox.IsDropDownOpen = true;
        }

        // Hàm xử lý sự kiện KeyUp chung cho cả 3 ComboBox
        private void ComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || comboBox.IsReadOnly) return;

            // Bỏ qua các phím điều khiển (như Shift, Ctrl, Alt, Enter, mũi tên)
            // Enter/Tab sẽ được xử lý qua LostFocus
            if (e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Tab)
            {
                return;
            }

            // Dùng KeyUp để lọc
            if (comboBox == cboMaHocVien)
            {
                FilterComboBox(comboBox, _allMaHocVien, DanhSachMaHocVien);
            }
            else if (comboBox == cboMaGoiTap)
            {
                FilterComboBox(comboBox, _allMaGoiTap, DanhSachMaGoiTap);
            }
            else if (comboBox == cboMaNhanVien)
            {
                FilterComboBox(comboBox, _allMaNhanVien, DanhSachMaNhanVien);
            }
        }

        // Logic LostFocus để kích hoạt tải dữ liệu khi gõ xong (Enter hoặc Tab ra ngoài) 
        private void cboMaHocVien_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                // 1. Cập nhật thuộc tính MaHocVien bằng giá trị Text hiện tại. 
                // Điều này sẽ kích hoạt setter MaHocVien và gọi TaiThongTinHocVien().
                MaHocVien = comboBox.Text;
            }
        }

        private void cboMaGoiTap_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                // 1. Cập nhật MaGoi
                MaGoi = comboBox.Text;

                // 2. Cập nhật thủ công Binding
                var bindingExpression = comboBox.GetBindingExpression(ComboBox.TextProperty);
                bindingExpression?.UpdateSource();
            }
        }

        private void cboMaNhanVien_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                // 1. Cập nhật MaNhanVien
                MaNhanVien = comboBox.Text;

                // 2. Cập nhật thủ công Binding
                var bindingExpression = comboBox.GetBindingExpression(ComboBox.TextProperty);
                bindingExpression?.UpdateSource();
            }
        }

        // Tải thông tin Họ tên học viên từ database dựa trên MaHocVien
        private void TaiThongTinHocVien()
        {
            // Kiểm tra xem mã đã được tìm kiếm có tồn tại trong danh sách master không
            if (!_allMaHocVien.Any(m => m.Equals(MaHocVien, StringComparison.OrdinalIgnoreCase)))
            {
                // Nếu mã không nằm trong danh sách master và không rỗng
                if (!string.IsNullOrEmpty(MaHocVien))
                {
                    HoTenHocVien = "";
                    txtHocVienError.Text = "Không tồn tại mã học viên";
                    txtHocVienError.Visibility = Visibility.Visible;
                }
                else
                {
                    HoTenHocVien = "";
                    txtHocVienError.Visibility = Visibility.Collapsed;
                }
                return;
            }

            if (string.IsNullOrEmpty(MaHocVien))
            {
                HoTenHocVien = "";
                txtHocVienError.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string query = "SELECT HoTen FROM HocVien WHERE MaHV = @MaHV";
                    using (var command = new SqliteCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@MaHV", MaHocVien);
                        var result = command.ExecuteScalar();

                        if (result != null)
                        {
                            HoTenHocVien = result.ToString();
                            txtHocVienError.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            // Trường hợp này hiếm xảy ra nếu đã kiểm tra master list ở trên, nhưng giữ lại phòng ngừa
                            HoTenHocVien = "";
                            txtHocVienError.Text = "Không tồn tại mã học viên";
                            txtHocVienError.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Thay thế MessageBox bằng phương thức thông báo phù hợp trong ứng dụng thực tế
                MessageBox.Show($"Lỗi khi tải thông tin học viên: {ex.Message}");
            }
        }

        // Tải thông tin Tên gói tập và Tổng tiền từ database dựa trên MaGoi
        private void TaiThongTinGoiTap()
        {
            // Kiểm tra master list trước
            if (!_allMaGoiTap.Any(m => m.Equals(MaGoi, StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrEmpty(MaGoi))
                {
                    TenGoiTap = "";
                    TongTien = 0;
                    txtGoiTapError.Text = "Không tồn tại mã gói tập";
                    txtGoiTapError.Visibility = Visibility.Visible;
                }
                else
                {
                    TenGoiTap = "";
                    TongTien = 0;
                    txtGoiTapError.Visibility = Visibility.Collapsed;
                }
                return;
            }

            if (string.IsNullOrEmpty(MaGoi))
            {
                TenGoiTap = "";
                TongTien = 0;
                txtGoiTapError.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string query = "SELECT TenGoi, GiaNiemYet FROM GoiTap WHERE MaGoi = @MaGoi";
                    using (var command = new SqliteCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@MaGoi", MaGoi);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                TenGoiTap = reader["TenGoi"].ToString();
                                TongTien = Convert.ToDecimal(reader["GiaNiemYet"]);
                                txtGoiTapError.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                TenGoiTap = "";
                                TongTien = 0;
                                txtGoiTapError.Text = "Không tồn tại mã gói tập";
                                txtGoiTapError.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Thay thế MessageBox bằng phương thức thông báo phù hợp trong ứng dụng thực tế
                MessageBox.Show($"Lỗi khi tải thông tin gói tập: {ex.Message}");
            }
        }

        // Tải thông tin Họ tên nhân viên từ database dựa trên MaNhanVien
        private void TaiThongTinNhanVien()
        {
            // Kiểm tra master list trước
            if (!_allMaNhanVien.Any(m => m.Equals(MaNhanVien, StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrEmpty(MaNhanVien))
                {
                    HoTenNhanVien = "";
                    txtNhanVienError.Text = "Không tồn tại mã nhân viên";
                    txtNhanVienError.Visibility = Visibility.Visible;
                }
                else
                {
                    HoTenNhanVien = "";
                    txtNhanVienError.Visibility = Visibility.Collapsed;
                }
                return;
            }

            if (string.IsNullOrEmpty(MaNhanVien))
            {
                HoTenNhanVien = "";
                txtNhanVienError.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string query = "SELECT HoTen FROM TaiKhoan WHERE MaTK = @MaTK";
                    using (var command = new SqliteCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@MaTK", MaNhanVien);
                        var result = command.ExecuteScalar();

                        if (result != null)
                        {
                            HoTenNhanVien = result.ToString();
                            txtNhanVienError.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            HoTenNhanVien = "";
                            txtNhanVienError.Text = "Không tồn tại mã nhân viên";
                            txtNhanVienError.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Thay thế MessageBox bằng phương thức thông báo phù hợp trong ứng dụng thực tế
                MessageBox.Show($"Lỗi khi tải thông tin nhân viên: {ex.Message}");
            }
        }

        // Tính toán số tiền nợ (còn lại) VÀ Trạng thái thanh toán
        private void TinhSoTienNoVaTrangThai()
        {
            // 1. Tính số tiền nợ
            SoTienNo = TongTien - DaThanhToan;

            // 2. Tính trạng thái thanh toán dựa trên yêu cầu:
            if (TongTien <= 0 && DaThanhToan == 0)
            {
                // Nếu chưa có gói tập hoặc chưa nhập gì
                TrangThaiThanhToan = "Chưa Thanh Toán";
            }
            else if (SoTienNo <= 0 && TongTien > 0)
            {
                // Nếu số tiền nợ bằng 0 (hoặc âm, tức là trả thừa) -> Đã thanh toán
                TrangThaiThanhToan = "Đã Thanh Toán";
            }
            else if (TongTien > 0 && DaThanhToan == 0)
            {
                // Nếu số tiền đã thanh toán bằng 0 (và có tổng tiền) -> Chưa thanh toán
                TrangThaiThanhToan = "Chưa Thanh Toán";
            }
            else if (SoTienNo > 0 && SoTienNo < TongTien)
            {
                // Nếu số tiền nợ lớn hơn 0 và bé hơn tổng tiền -> Trả một phần
                TrangThaiThanhToan = "Trả Một Phần";
            }
            else
            {
                // Trường hợp khác
                TrangThaiThanhToan = "Chưa Xác Định";
            }
        }
        #endregion

        #region Xử lý Thêm Giao Dịch và Database
        // Sự kiện khi nhấn nút "Tạo" giao dịch
        private void TaoGiaoDichButton_Click(object sender, RoutedEventArgs e)
        {
            // Cập nhật lại ngày giao dịch là thời điểm tạo
            NgayGiaoDich = DateTime.Now;

            // Tính toán số tiền nợ và trạng thái cuối cùng trước khi lưu
            TinhSoTienNoVaTrangThai();

            // Kiểm tra dữ liệu bắt buộc
            if (string.IsNullOrEmpty(MaHocVien) || string.IsNullOrEmpty(MaGoi) || string.IsNullOrEmpty(MaNhanVien))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ mã học viên, mã gói tập và mã nhân viên!");
                return;
            }

            // Kiểm tra mã có tồn tại không (dùng lại hàm KiemTraTonTai)
            if (!KiemTraTonTai("HocVien", "MaHV", MaHocVien))
            {
                MessageBox.Show("Mã học viên không tồn tại!");
                return;
            }

            if (!KiemTraTonTai("GoiTap", "MaGoi", MaGoi))
            {
                MessageBox.Show("Mã gói tập không tồn tại!");
                return;
            }

            if (!KiemTraTonTai("TaiKhoan", "MaTK", MaNhanVien))
            {
                MessageBox.Show("Mã nhân viên không tồn tại!");
                return;
            }

            // Kiểm tra tính hợp lệ của số tiền thanh toán
            if (DaThanhToan < 0)
            {
                MessageBox.Show("Số tiền đã thanh toán không được âm!");
                return;
            }

            if (DaThanhToan > TongTien)
            {
                MessageBox.Show("Số tiền đã thanh toán không được lớn hơn tổng tiền!");
                return;
            }

            if (TongTien <= 0)
            {
                MessageBox.Show("Tổng tiền phải lớn hơn 0 để tạo giao dịch!");
                return;
            }

            // Tạo mã giao dịch tự động
            string maGD = TaoMaGiaoDich();

            // Tạo giao dịch mới (INSERT INTO GiaoDich)
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO GiaoDich (MaGD, TongTien, DaThanhToan, SoTienNo, PhuongThuc, NgayGD, TrangThai, MaHV, MaGoi, MaTK)
                        VALUES (@MaGD, @TongTien, @DaThanhToan, @SoTienNo, @PhuongThuc, @NgayGD, @TrangThai, @MaHV, @MaGoi, @MaTK)";

                    using (var command = new SqliteCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@MaGD", maGD);
                        command.Parameters.AddWithValue("@TongTien", TongTien);
                        command.Parameters.AddWithValue("@DaThanhToan", DaThanhToan);
                        command.Parameters.AddWithValue("@SoTienNo", SoTienNo);
                        command.Parameters.AddWithValue("@PhuongThuc", PhuongThucThanhToan);
                        // Lưu ngày giao dịch với định dạng yyyy-MM-dd HH:mm:ss
                        command.Parameters.AddWithValue("@NgayGD", NgayGiaoDich.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.Parameters.AddWithValue("@TrangThai", TrangThaiThanhToan);
                        command.Parameters.AddWithValue("@MaHV", MaHocVien);
                        command.Parameters.AddWithValue("@MaGoi", MaGoi);
                        command.Parameters.AddWithValue("@MaTK", MaNhanVien);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"Tạo giao dịch thành công!\nMã giao dịch: {maGD}");
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Tạo giao dịch thất bại!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo giao dịch: {ex.Message}");
            }
        }

        // Kiểm tra sự tồn tại của một mã (MaHV, MaGoi, MaTK) trong database
        private bool KiemTraTonTai(string tableName, string columnName, string value)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string query = $"SELECT COUNT(1) FROM {tableName} WHERE {columnName} = @Value";
                    using (var command = new SqliteCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@Value", value);
                        var result = command.ExecuteScalar();
                        int count = result != null ? Convert.ToInt32(result) : 0;
                        return count > 0;
                    }
                }
            }
            catch (Exception)
            {
                // Xử lý lỗi kết nối/query
                return false;
            }
        }

        // Tạo mã giao dịch tự động (GD0001, GD0002,...)
        private string TaoMaGiaoDich()
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();
                    string query = "SELECT MaGD FROM GiaoDich ORDER BY MaGD DESC LIMIT 1";
                    using (var command = new SqliteCommand(query, conn))
                    {
                        var result = command.ExecuteScalar();

                        if (result == null)
                        {
                            return "GD0001";
                        }

                        string lastMa = result.ToString();
                        // Tăng giá trị số lên 1
                        if (lastMa.StartsWith("GD") && int.TryParse(lastMa.Substring(2), out int number))
                        {
                            return $"GD{(number + 1):D4}";
                        }

                        return "GD0001"; // Mã mặc định nếu không parse được
                    }
                }
            }
            catch (Exception)
            {
                return "GD0001"; // Trả về mã mặc định khi có lỗi kết nối/query
            }
        }
        #endregion

        #region Triển khai INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        // Phương thức gọi sự kiện PropertyChanged khi giá trị thuộc tính thay đổi
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}