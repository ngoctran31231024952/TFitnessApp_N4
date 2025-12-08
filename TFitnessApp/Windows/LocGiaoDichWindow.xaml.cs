using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TFitnessApp;
using Microsoft.Data.Sqlite;
using TFitnessApp.Database;
using static TFitnessApp.GiaoDichPage;

namespace TFitnessApp.Windows
{
    public partial class Window5 : Window
    {
        public MoDonBoLoc KetQuaLoc { get; private set; }

        private readonly Dictionary<string, KhoangTongTien> CacKhoangTongTien = new Dictionary<string, KhoangTongTien>
        {
            { "Không Áp Dụng", KhoangTongTien.KhongChon },
            { "Dưới 3.000.000 VNĐ", KhoangTongTien.Duoi3Trieu },
            { "3.000.000 VNĐ - 5.000.000 VNĐ", KhoangTongTien.Tu3Den5Trieu },
            { "Trên 5.000.000 VNĐ", KhoangTongTien.Tren5Trieu }
        };

        // Danh sách MASTER (lưu trữ tất cả mã)
        private List<string> _allMaHVs = new List<string>();
        private List<string> _allMaGois = new List<string>();
        private List<string> _allMaNVs = new List<string>();

        // Danh sách cho ComboBox tìm kiếm (chỉ hiển thị các mục đã lọc)
        public ObservableCollection<string> DanhSachMaHocVien { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> DanhSachMaGoiTap { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> DanhSachMaNhanVien { get; set; } = new ObservableCollection<string>();

        // Mục mặc định
        private const string DEFAULT_ITEM = "— Không Áp Dụng —";

        public Window5(MoDonBoLoc boLocHienTai)
        {
            InitializeComponent();

            this.DataContext = this;

            // Tạo bản sao của bộ lọc hiện tại để chỉnh sửa
            KetQuaLoc = new MoDonBoLoc();
            if (boLocHienTai != null)
            {
                // Sao chép tất cả thuộc tính
                KetQuaLoc.LocMaHV = boLocHienTai.LocMaHV;
                KetQuaLoc.LocMaGoi = boLocHienTai.LocMaGoi;
                KetQuaLoc.LocMaNV = boLocHienTai.LocMaNV;
                KetQuaLoc.TuNgay = boLocHienTai.TuNgay;
                KetQuaLoc.DenNgay = boLocHienTai.DenNgay;
                KetQuaLoc.KhoangTongTienDuocChon = boLocHienTai.KhoangTongTienDuocChon;
                KetQuaLoc.TuTongTien = boLocHienTai.TuTongTien;
                KetQuaLoc.DenTongTien = boLocHienTai.DenTongTien;
            }

            // 1. Tải dữ liệu ID từ Database
            LoadComboBoxDataFromDatabase();

            // 2. Thiết lập ItemsSource
            cboLocTongTien.ItemsSource = CacKhoangTongTien.Keys.ToList();

            // 3. Đồng bộ trạng thái ban đầu
            DongBoTrangThaiVoiUI(KetQuaLoc);
        }

        private void LoadComboBoxDataFromDatabase()
        {
            try
            {
                // Thêm mục mặc định vào danh sách gốc
                _allMaHVs.Add(DEFAULT_ITEM);
                _allMaGois.Add(DEFAULT_ITEM);
                _allMaNVs.Add(DEFAULT_ITEM);

                // Load dữ liệu từ Database
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())
                {
                    conn.Open();

                    // Load học viên từ bảng HocVien
                    string sqlHV = "SELECT MaHV, HoTen FROM HocVien ORDER BY MaHV";
                    using (SqliteCommand cmd = new SqliteCommand(sqlHV, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string maHV = reader["MaHV"].ToString();
                            string hoTen = reader["HoTen"].ToString();
                            string displayText = $"{maHV} - {hoTen}";
                            _allMaHVs.Add(displayText);
                        }
                    }

                    // Load gói tập từ bảng GoiTap
                    string sqlGoi = "SELECT MaGoi, TenGoi FROM GoiTap ORDER BY MaGoi";
                    using (SqliteCommand cmd = new SqliteCommand(sqlGoi, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string maGoi = reader["MaGoi"].ToString();
                            string tenGoi = reader["TenGoi"].ToString();
                            string displayText = $"{maGoi} - {tenGoi}";
                            _allMaGois.Add(displayText);
                        }
                    }

                    // Load nhân viên từ bảng TaiKhoan
                    string sqlNV = "SELECT MaTK, HoTen FROM TaiKhoan ORDER BY MaTK";
                    using (SqliteCommand cmd = new SqliteCommand(sqlNV, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string maTK = reader["MaTK"].ToString();
                            string hoTen = reader["HoTen"].ToString();
                            string displayText = $"{maTK} - {hoTen}";
                            _allMaNVs.Add(displayText);
                        }
                    }
                }

                // Thêm tất cả vào danh sách hiển thị ban đầu
                foreach (var item in _allMaHVs)
                {
                    DanhSachMaHocVien.Add(item);
                }
                foreach (var item in _allMaGois)
                {
                    DanhSachMaGoiTap.Add(item);
                }
                foreach (var item in _allMaNVs)
                {
                    DanhSachMaNhanVien.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu từ Database: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DongBoTrangThaiVoiUI(MoDonBoLoc boLoc)
        {
            dpTuNgay.SelectedDate = boLoc.TuNgay;
            dpDenNgay.SelectedDate = boLoc.DenNgay;

            // Lọc theo Mã ID
            SetComboBoxSelection(cboMaHocVien, _allMaHVs, boLoc.LocMaHV);
            SetComboBoxSelection(cboMaGoiTap, _allMaGois, boLoc.LocMaGoi);
            SetComboBoxSelection(cboMaNhanVien, _allMaNVs, boLoc.LocMaNV);

            // Lọc theo Tổng Tiền
            var currentKey = CacKhoangTongTien.FirstOrDefault(x => x.Value == boLoc.KhoangTongTienDuocChon).Key;
            if (!string.IsNullOrEmpty(currentKey))
            {
                cboLocTongTien.SelectedItem = currentKey;
            }
            else
            {
                cboLocTongTien.SelectedIndex = 0; // Chọn "Không Áp Dụng"
            }
        }

        private void SetComboBoxSelection(ComboBox combo, List<string> allData, string maLoc)
        {
            if (string.IsNullOrWhiteSpace(maLoc))
            {
                combo.Text = DEFAULT_ITEM;
                combo.SelectedItem = DEFAULT_ITEM;
                return;
            }

            // Tìm item có ID khớp (tìm trong phần mã - trước dấu '-')
            var selectedItem = allData.FirstOrDefault(item =>
            {
                if (item == DEFAULT_ITEM) return false;
                // Tách mã từ chuỗi "Mã - Tên"
                int dashIndex = item.IndexOf('-');
                if (dashIndex > 0)
                {
                    string ma = item.Substring(0, dashIndex).Trim();
                    return ma.Equals(maLoc, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            });

            if (selectedItem != null)
            {
                combo.Text = selectedItem;
                combo.SelectedItem = selectedItem;
            }
            else
            {
                combo.Text = DEFAULT_ITEM;
                combo.SelectedItem = DEFAULT_ITEM;
            }
        }

        private void XacDinhKhoangTien()
        {
            KhoangTongTien selectedEnum = KhoangTongTien.KhongChon;
            if (cboLocTongTien.SelectedItem is string selectedText && CacKhoangTongTien.ContainsKey(selectedText))
            {
                selectedEnum = CacKhoangTongTien[selectedText];
            }

            KetQuaLoc.KhoangTongTienDuocChon = selectedEnum;

            switch (selectedEnum)
            {
                case KhoangTongTien.Duoi3Trieu:
                    KetQuaLoc.TuTongTien = 0m;
                    KetQuaLoc.DenTongTien = 2999999.99m;
                    break;
                case KhoangTongTien.Tu3Den5Trieu:
                    KetQuaLoc.TuTongTien = 3000000m;
                    KetQuaLoc.DenTongTien = 5000000m;
                    break;
                case KhoangTongTien.Tren5Trieu:
                    KetQuaLoc.TuTongTien = 5000000.01m;
                    KetQuaLoc.DenTongTien = null;
                    break;
                case KhoangTongTien.KhongChon:
                default:
                    KetQuaLoc.TuTongTien = null;
                    KetQuaLoc.DenTongTien = null;
                    break;
            }
        }

        private void ApDung_Click (object sender, RoutedEventArgs e)
        {
            try
            {
                // Cập nhật Ngày tháng
                KetQuaLoc.TuNgay = dpTuNgay.SelectedDate;
                KetQuaLoc.DenNgay = dpDenNgay.SelectedDate;

                // Cập nhật Mã ID
                KetQuaLoc.LocMaHV = GetComboBoxSelectedIDOrText(cboMaHocVien, _allMaHVs);
                KetQuaLoc.LocMaGoi = GetComboBoxSelectedIDOrText(cboMaGoiTap, _allMaGois);
                KetQuaLoc.LocMaNV = GetComboBoxSelectedIDOrText(cboMaNhanVien, _allMaNVs);

                // Cập nhật khoảng Tổng Tiền
                XacDinhKhoangTien();

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetComboBoxSelectedIDOrText(ComboBox combo, List<string> allData)
        {
            string typedText = combo.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(typedText) || typedText == DEFAULT_ITEM)
            {
                return string.Empty;
            }

            // Tìm trong danh sách xem có item khớp không
            var matchedItem = allData.FirstOrDefault(item => 
                item.Equals(typedText, StringComparison.OrdinalIgnoreCase));

            if (matchedItem != null && matchedItem != DEFAULT_ITEM)
            {
                // Tách mã từ chuỗi "Mã - Tên"
                int dashIndex = matchedItem.IndexOf('-');
                if (dashIndex > 0)
                {
                    return matchedItem.Substring(0, dashIndex).Trim();
                }
                return matchedItem;
            }

            // Nếu không tìm thấy, trả về text gõ vào
            return typedText;
        }

        // --- Logic Mới: Xử lý GotFocus để mở Dropdown ---
        private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            // Chỉ mở dropdown nếu nó chưa mở
            if (comboBox != null && !comboBox.IsDropDownOpen)
            {
                comboBox.IsDropDownOpen = true;
            }
        }

        // Hàm hỗ trợ tìm TextBox nội bộ của ComboBox (để đặt CaretIndex)
        private TextBox TimKiemTextBoxChinhSua (ComboBox comboBox)
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
            var editableTextBox = TimKiemTextBoxChinhSua (comboBox);
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

            // Lọc danh sách - luôn bao gồm mục mặc định
            var filteredList = new List<string>();
            
            // Thêm mục mặc định nếu text trống hoặc text là mục mặc định
            if (string.IsNullOrWhiteSpace(filterText) || filterText == DEFAULT_ITEM)
            {
                filteredList.Add(DEFAULT_ITEM);
                // Thêm các item khác không lọc
                filteredList.AddRange(masterList.Where(ma => ma != DEFAULT_ITEM));
            }
            else
            {
                // Thêm mục mặc định vào đầu danh sách
                filteredList.Add(DEFAULT_ITEM);
                // Lọc các item khác
                var otherItems = masterList
                    .Where(ma => ma != DEFAULT_ITEM && 
                           ma.ToLower().Contains(filterText.ToLower()))
                    .ToList();
                filteredList.AddRange(otherItems);
            }

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
                FilterComboBox(comboBox, _allMaHVs, DanhSachMaHocVien);
            }
            else if (comboBox == cboMaGoiTap)
            {
                FilterComboBox(comboBox, _allMaGois, DanhSachMaGoiTap);
            }
            else if (comboBox == cboMaNhanVien)
            {
                FilterComboBox(comboBox, _allMaNVs, DanhSachMaNhanVien);
            }
        }

        // --- Logic LostFocus để xử lý khi gõ xong ---
        private void cboMaHocVien_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            string currentText = comboBox.Text.Trim();
            
            // Nếu text rỗng hoặc bằng mặc định
            if (string.IsNullOrWhiteSpace(currentText) || currentText == DEFAULT_ITEM)
            {
                comboBox.Text = DEFAULT_ITEM;
                comboBox.SelectedItem = DEFAULT_ITEM;
                return;
            }

            // Tìm item khớp trong danh sách
            var match = _allMaHVs.FirstOrDefault(item => 
                item.Equals(currentText, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                comboBox.SelectedItem = match;
                comboBox.Text = match;
            }
            else
            {
                // Nếu không tìm thấy, GIỮ NGUYÊN TEXT người dùng đã gõ
                // Không đặt SelectedItem (giữ null)
            }
        }

        private void cboMaGoiTap_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            string currentText = comboBox.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(currentText) || currentText == DEFAULT_ITEM)
            {
                comboBox.Text = DEFAULT_ITEM;
                comboBox.SelectedItem = DEFAULT_ITEM;
                return;
            }

            var match = _allMaGois.FirstOrDefault(item => 
                item.Equals(currentText, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                comboBox.SelectedItem = match;
                comboBox.Text = match;
            }
        }

        private void cboMaNhanVien_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            string currentText = comboBox.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(currentText) || currentText == DEFAULT_ITEM)
            {
                comboBox.Text = DEFAULT_ITEM;
                comboBox.SelectedItem = DEFAULT_ITEM;
                return;
            }

            var match = _allMaNVs.FirstOrDefault(item => 
                item.Equals(currentText, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                comboBox.SelectedItem = match;
                comboBox.Text = match;
            }
        }

        // Sự kiện khi nhấn nút Hủy Bộ Lọc
        private void HuyLoc_Click (object sender, RoutedEventArgs e)
        {
            // Reset tất cả các bộ lọc về mặc định
            dpTuNgay.SelectedDate = DateTime.Today.AddMonths(-1);
            dpDenNgay.SelectedDate = DateTime.Today;

            // Đặt về mặc định
            cboMaHocVien.Text = DEFAULT_ITEM;
            cboMaHocVien.SelectedItem = DEFAULT_ITEM;

            cboMaGoiTap.Text = DEFAULT_ITEM;
            cboMaGoiTap.SelectedItem = DEFAULT_ITEM;

            cboMaNhanVien.Text = DEFAULT_ITEM;
            cboMaNhanVien.SelectedItem = DEFAULT_ITEM;

            cboLocTongTien.SelectedIndex = 0; // Chọn "Không Áp Dụng"

            // Reset danh sách hiển thị
            DanhSachMaHocVien.Clear();
            DanhSachMaGoiTap.Clear();
            DanhSachMaNhanVien.Clear();

            foreach (var item in _allMaHVs)
            {
                DanhSachMaHocVien.Add(item);
            }
            foreach (var item in _allMaGois)
            {
                DanhSachMaGoiTap.Add(item);
            }
            foreach (var item in _allMaNVs)
            {
                DanhSachMaNhanVien.Add(item);
            }
        }
    }
}