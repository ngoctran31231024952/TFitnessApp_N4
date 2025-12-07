using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        private List<ComboBoxItemData> _allMaHVs = new List<ComboBoxItemData>();
        private List<ComboBoxItemData> _allMaGois = new List<ComboBoxItemData>();
        private List<ComboBoxItemData> _allMaNVs = new List<ComboBoxItemData>();

        private ComboBoxItemData _defaultItem = new ComboBoxItemData { ID = string.Empty, Name = "— Không Áp Dụng —" };

        public Window5(MoDonBoLoc boLocHienTai)
        {
            InitializeComponent();

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

            cboMaHocVien.ItemsSource = _allMaHVs;
            cboMaGoiTap.ItemsSource = _allMaGois;
            cboMaNhanVien.ItemsSource = _allMaNVs;

            // 3. Đồng bộ trạng thái ban đầu
            DongBoTrangThaiVoiUI(KetQuaLoc);
        }

        private void LoadComboBoxDataFromDatabase()
        {
            try
            {
                // Thêm mục mặc định vào danh sách gốc
                _allMaHVs.Add(_defaultItem);
                _allMaGois.Add(_defaultItem);
                _allMaNVs.Add(_defaultItem);

                // Load dữ liệu từ Database
                using (SqliteConnection conn = DbAccess.CreateConnection())
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
                            _allMaHVs.Add(new ComboBoxItemData
                            {
                                ID = maHV,
                                Name = $"{maHV} - {hoTen}"
                            });
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
                            _allMaGois.Add(new ComboBoxItemData
                            {
                                ID = maGoi,
                                Name = $"{maGoi} - {tenGoi}"
                            });
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
                            _allMaNVs.Add(new ComboBoxItemData
                            {
                                ID = maTK,
                                Name = $"{maTK} - {hoTen}"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu từ Database: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // Thêm dữ liệu mẫu nếu không thể kết nối database
                AddSampleData();
            }
        }

        private void AddSampleData()
        {
            // Thêm dữ liệu mẫu nếu không thể kết nối database
            _allMaHVs.Add(new ComboBoxItemData { ID = "HV001", Name = "HV001 - Nguyễn Văn A" });
            _allMaHVs.Add(new ComboBoxItemData { ID = "HV002", Name = "HV002 - Trần Thị B" });
            _allMaHVs.Add(new ComboBoxItemData { ID = "HV003", Name = "HV003 - Lê Văn C" });

            _allMaGois.Add(new ComboBoxItemData { ID = "GT01", Name = "GT01 - Gói 3 tháng" });
            _allMaGois.Add(new ComboBoxItemData { ID = "GT02", Name = "GT02 - Gói 1 năm" });

            _allMaNVs.Add(new ComboBoxItemData { ID = "TK01", Name = "TK01 - PT Minh" });
            _allMaNVs.Add(new ComboBoxItemData { ID = "TK02", Name = "TK02 - NV Hằng" });
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

        private void SetComboBoxSelection(ComboBox combo, List<ComboBoxItemData> allData, string maLoc)
        {
            if (string.IsNullOrWhiteSpace(maLoc))
            {
                combo.SelectedItem = _defaultItem;
                combo.Text = _defaultItem.Name;
                return;
            }

            // Tìm item có ID khớp
            var selectedItem = allData.FirstOrDefault(item => item.ID.Equals(maLoc, StringComparison.OrdinalIgnoreCase));
            if (selectedItem != null)
            {
                combo.SelectedItem = selectedItem;
                combo.Text = selectedItem.Name;
            }
            else
            {
                // Nếu không khớp, để trống
                combo.SelectedItem = null;
                combo.Text = "";
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

        private void Apply_Click(object sender, RoutedEventArgs e)
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

        private string GetComboBoxSelectedIDOrText(ComboBox combo, List<ComboBoxItemData> allData)
        {
            if (combo.SelectedItem is ComboBoxItemData selectedData)
            {
                return selectedData.ID;
            }

            string typedText = combo.Text.Trim();
            if (string.IsNullOrWhiteSpace(typedText) || typedText == _defaultItem.Name)
            {
                return string.Empty;
            }

            // Tìm ID khớp theo Name
            var matchedItem = allData.FirstOrDefault(item => item.Name.Equals(typedText, StringComparison.OrdinalIgnoreCase));
            if (matchedItem != null)
            {
                return matchedItem.ID;
            }

            // Tìm ID khớp theo ID
            matchedItem = allData.FirstOrDefault(item => item.ID.Equals(typedText, StringComparison.OrdinalIgnoreCase));
            if (matchedItem != null)
            {
                return matchedItem.ID;
            }

            // Trả về text gõ vào
            return typedText;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Các sự kiện ComboBox giữ nguyên
        private void ComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter) return;

            string searchText = combo.Text.ToLower().Trim();
            List<ComboBoxItemData> sourceList;

            if (combo == cboMaHocVien) sourceList = _allMaHVs;
            else if (combo == cboMaGoiTap) sourceList = _allMaGois;
            else if (combo == cboMaNhanVien) sourceList = _allMaNVs;
            else return;

            var filtered = sourceList
                .Where(item => item.ID.Equals(string.Empty) ||
                               item.Name.ToLower().Contains(searchText) ||
                               item.ID.ToLower().Contains(searchText))
                .ToList();

            combo.ItemsSource = filtered;
            combo.IsDropDownOpen = true;

            if (combo.IsEditable)
            {
                TextBox textBox = combo.Template.FindName("PART_EditableTextBox", combo) as TextBox;
                if (textBox != null)
                {
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }
        }

        private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null) return;

            if (combo == cboMaHocVien) combo.ItemsSource = _allMaHVs;
            else if (combo == cboMaGoiTap) combo.ItemsSource = _allMaGois;
            else if (combo == cboMaNhanVien) combo.ItemsSource = _allMaNVs;

            combo.IsDropDownOpen = true;
        }

        private void cboMaHocVien_LostFocus(object sender, RoutedEventArgs e)
        {
            HandleLostFocus(cboMaHocVien, _allMaHVs);
        }

        private void cboMaGoiTap_LostFocus(object sender, RoutedEventArgs e)
        {
            HandleLostFocus(cboMaGoiTap, _allMaGois);
        }

        private void cboMaNhanVien_LostFocus(object sender, RoutedEventArgs e)
        {
            HandleLostFocus(cboMaNhanVien, _allMaNVs);
        }

        private void HandleLostFocus(ComboBox combo, List<ComboBoxItemData> allData)
        {
            string typedText = combo.Text.Trim();

            if (string.IsNullOrWhiteSpace(typedText) || typedText == _defaultItem.Name)
            {
                combo.Text = _defaultItem.Name;
                combo.SelectedItem = _defaultItem;
                return;
            }

            var match = allData.FirstOrDefault(item =>
                item.Name.Equals(typedText, StringComparison.OrdinalIgnoreCase) ||
                item.ID.Equals(typedText, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                combo.SelectedItem = match;
                combo.Text = match.Name;
            }
        }
    }
}