using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using TFitnessApp.Windows; // <<< ĐÃ THÊM DÒNG NÀY ĐỂ KHẮC PHỤC LỖI CS0234

namespace TFitnessApp
{
    public partial class HopDongPage : Page
    {
        public ObservableCollection<HopDong> DanhSachHopDong { get; set; } = new ObservableCollection<HopDong>();
        private readonly string chuoiKetNoi;

        public HopDongPage()
        {
            InitializeComponent();
            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            chuoiKetNoi = $"Data Source={duongDanDB};";
            if (!File.Exists(duongDanDB))
            {
                MessageBox.Show($"Không tìm thấy cơ sở dữ liệu tại:\n{duongDanDB}\n\nỨng dụng sẽ hoạt động mà không có dữ liệu thực tế.", "Cảnh Báo Database", MessageBoxButton.OK, MessageBoxImage.Warning);
                chuoiKetNoi = string.Empty;
            }
            LoadData();
            HopDongDataGrid.ItemsSource = DanhSachHopDong;
        }

        // =========================================================================
        // PHẦN LOGIC THAO TÁC DATABASE VÀ TÍNH TOÁN
        // =========================================================================

        private void LoadData()
        {
            this.DanhSachHopDong.Clear();
            if (string.IsNullOrEmpty(chuoiKetNoi)) return;

            string sql = @"
                SELECT
                    hd.MaHD,
                    hd.NgayHetHan,    
                    hv.HoTen,        
                    hv.SDT,          
                    hv.Email,
                    hd.MaCN,          
                    gt.ThoiHan,
                    gt.GiaNiemYet
                FROM HopDong hd
                INNER JOIN HocVien hv ON hd.MaHV = hv.MaHV
                INNER JOIN GoiTap gt ON hd.MaGoi = gt.MaGoi;
            ";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime ngayHetHan = ParseNgay(reader["NgayHetHan"].ToString());
                                string trangThai = TinhTrangThai(ngayHetHan);

                                this.DanhSachHopDong.Add(new HopDong
                                {
                                    MaHopDong = reader["MaHD"].ToString(),
                                    TenHocVien = reader["HoTen"].ToString(),
                                    SoDienThoai = reader["SDT"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    ChiNhanh = reader["MaCN"].ToString(),
                                    ThoiHan = reader["ThoiHan"].ToString() + " tháng",
                                    GiaTri = reader["GiaNiemYet"].ToString(),
                                    TrangThai = trangThai,
                                    IsSelected = false
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI TẢI DỮ LIỆU:\nSQLite Error: {ex.Message}", "LỖI DATABASE", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<HopDong> SearchData(string keyword)
        {
            List<HopDong> danhSachKetQua = new List<HopDong>();
            if (string.IsNullOrEmpty(chuoiKetNoi)) return danhSachKetQua;

            string sql = @"
                SELECT
                    hd.MaHD,
                    hd.NgayHetHan,
                    hv.HoTen,
                    hv.SDT,
                    hv.Email,
                    hd.MaCN,
                    gt.ThoiHan,
                    gt.GiaNiemYet
                FROM HopDong hd
                INNER JOIN HocVien hv ON hd.MaHV = hv.MaHV
                INNER JOIN GoiTap gt ON hd.MaGoi = gt.MaGoi
                WHERE hd.MaHD LIKE @keyword 
                OR hv.HoTen LIKE @keyword 
                OR hv.SDT LIKE @keyword;
            ";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime ngayHetHan = ParseNgay(reader["NgayHetHan"].ToString());
                                string trangThai = TinhTrangThai(ngayHetHan);

                                danhSachKetQua.Add(new HopDong
                                {
                                    MaHopDong = reader["MaHD"].ToString(),
                                    TenHocVien = reader["HoTen"].ToString(),
                                    SoDienThoai = reader["SDT"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    ChiNhanh = reader["MaCN"].ToString(),
                                    ThoiHan = reader["ThoiHan"].ToString() + " tháng",
                                    GiaTri = reader["GiaNiemYet"].ToString(),
                                    TrangThai = trangThai,
                                    IsSelected = false
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tìm kiếm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return danhSachKetQua;
        }

        private string TinhTrangThai(DateTime ngayHetHan)
        {
            if (ngayHetHan == DateTime.MinValue)
            {
                return "Không xác định";
            }
            DateTime ngayHienTai = DateTime.Today;
            if (ngayHienTai < ngayHetHan)
            {
                return "Còn hiệu lực";
            }
            else
            {
                return "Hết hiệu lực";
            }
        }

        private DateTime ParseNgay(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString)) return DateTime.MinValue;

            string[] formats = { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "d-M-yyyy", "d/M/yyyy" };

            if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                if (result.Year > 1900) return result;
            }

            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                if (result.Year > 1900) return result;
            }

            return DateTime.MinValue;
        }

        // ⭐️ ĐỊNH NGHĨA CHUẨN VÀ DUY NHẤT CHO HÀM DELETE ⭐️
        private bool DeleteHopDong(List<string> maHopDongList)
        {
            if (string.IsNullOrEmpty(chuoiKetNoi) || maHopDongList == null || maHopDongList.Count == 0) return false;

            string ids = string.Join(",", maHopDongList.Select(id => $"'{id.Replace("'", "''")}'"));
            string sql = $"DELETE FROM HopDong WHERE MaHD IN ({ids})";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected == maHopDongList.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        // =========================================================================
        // PHẦN LOGIC GIAO DIỆN VÀ SỰ KIỆN (Tiếp tục)
        // =========================================================================

        private T FindVisualChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
        {
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

        private DataGridColumnHeader GetFirstColumnHeader(DataGrid dataGrid)
        {
            Visual visual = VisualTreeHelper.GetChild(dataGrid, 0) as Visual;
            if (visual == null) return null;

            DataGridColumnHeadersPresenter presenter = visual.Descendants().OfType<DataGridColumnHeadersPresenter>().FirstOrDefault();
            if (presenter == null) return null;

            return (DataGridColumnHeader)presenter.ItemContainerGenerator.ContainerFromIndex(0);
        }

        private void UpdateSelectAllState()
        {
            if (HopDongDataGrid == null || HopDongDataGrid.Columns.Count == 0) return;

            DataGridColumnHeader header = GetFirstColumnHeader(HopDongDataGrid);
            if (header == null) return;

            CheckBox selectAllCheckBox = FindVisualChild<CheckBox>(header, "chkSelectAll");

            if (selectAllCheckBox != null)
            {
                int totalItems = DanhSachHopDong.Count;
                int selectedItems = DanhSachHopDong.Count(gd => gd.IsSelected);

                if (totalItems > 0 && selectedItems == totalItems)
                {
                    selectAllCheckBox.IsChecked = true;
                }
                else if (selectedItems > 0)
                {
                    selectAllCheckBox.IsChecked = null; // Indeterminate
                }
                else
                {
                    selectAllCheckBox.IsChecked = false;
                }
            }
        }

        // PHƯƠNG THỨC NÀY ĐÃ ĐƯỢC SỬA LỖI CS0234
        private void ViewDetailButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag is string maHopDong)
            {
                // Gọi cửa sổ XemHopDongWindow
                XemHopDongWindow viewWindow = new XemHopDongWindow(maHopDong);
                viewWindow.ShowDialog();
            }
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox == null || !checkBox.IsChecked.HasValue) return;

            bool isChecked = checkBox.IsChecked.Value;

            foreach (var hopDong in DanhSachHopDong)
            {
                hopDong.IsSelected = isChecked;
            }
            HopDongDataGrid.Items.Refresh();
            UpdateSelectAllState();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e) { UpdateSelectAllState(); }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) { UpdateSelectAllState(); }


        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (this.SearchTextBox.Text == "Search text")
            {
                this.SearchTextBox.Text = "";
                this.SearchTextBox.Foreground = Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.SearchTextBox.Text))
            {
                this.SearchTextBox.Text = "Search text";
                this.SearchTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = this.SearchTextBox.Text;

            if (string.IsNullOrWhiteSpace(keyword) || keyword == "Search text")
            {
                LoadData();
            }
            else
            {
                DanhSachHopDong.Clear();
                List<HopDong> list = SearchData(keyword);

                foreach (var item in list)
                {
                    DanhSachHopDong.Add(item);
                }
            }
        }

        private void CreateButton_Click(object sender, MouseButtonEventArgs e)
        {
            // Hiển thị cửa sổ tạo hợp đồng
            // Lưu ý: Cần đảm bảo ThemHopDongWindow cũng sử dụng namespace TFitnessApp.Windows hoặc được khai báo đúng cách
            ThemHopDongWindow themHopDongWindow = new ThemHopDongWindow();

            // Thiết lập Action làm mới dữ liệu sau khi thêm hợp đồng thành công
            themHopDongWindow.OnContractAdded = LoadData;

            themHopDongWindow.ShowDialog();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (this.SearchTextBox.Text != "Search text")
            {
                SearchTextBox_LostFocus(SearchTextBox, null);
            }

            MessageBox.Show("Đã làm mới dữ liệu!", "Thông báo",
                               MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = DanhSachHopDong.Where(x => x.IsSelected).ToList();

            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một hợp đồng để xóa!",
                               "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa {selectedItems.Count} hợp đồng đã chọn?\n\n" +
                $"Danh sách:\n" + string.Join("\n", selectedItems.Select(x => $"- {x.MaHopDong}: {x.TenHocVien}")),
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (DeleteHopDong(selectedItems.Select(x => x.MaHopDong).ToList()))
                {
                    MessageBox.Show($"Đã xóa thành công {selectedItems.Count} hợp đồng!",
                                   "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show("Xóa thất bại! Vui lòng thử lại.",
                                   "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    // ĐỊNH NGHĨA LỚP HopDong
    public class HopDong : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string MaHopDong { get; set; }
        public string TenHocVien { get; set; }
        public string SoDienThoai { get; set; }
        public string Email { get; set; }
        public string ChiNhanh { get; set; }
        public string ThoiHan { get; set; }
        public string GiaTri { get; set; }
        public string TrangThai { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ⭐️ KHẮC PHỤC LỖI CS1061: THÊM LỚP VISUAL TREE EXTENSIONS ⭐️
    // Đặt ngoài lớp HopDongPage, nhưng trong namespace TFitnessApp
    public static class VisualTreeExtensions
    {
        // Hàm mở rộng Descendants
        public static IEnumerable<DependencyObject> Descendants(this DependencyObject root)
        {
            if (root == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                yield return child;

                foreach (var descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }

        public static IEnumerable<T> Descendants<T>(this DependencyObject root) where T : DependencyObject
        {
            return Descendants(root).OfType<T>();
        }
    }
}