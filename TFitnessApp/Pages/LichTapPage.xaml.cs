using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using TFitnessApp.Windows;

namespace TFitnessApp
{
    public partial class LichTapPage : Page
    {
        public ObservableCollection<LichTapModel> DanhSachLichTap { get; set; } = new ObservableCollection<LichTapModel>();
        private readonly string chuoiKetNoi;

        public LichTapPage()
        {
            InitializeComponent();
            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            chuoiKetNoi = $"Data Source={duongDanDB};";

            if (!File.Exists(duongDanDB))
            {
                MessageBox.Show($"Không tìm thấy cơ sở dữ liệu tại:\n{duongDanDB}", "Cảnh Báo Database", MessageBoxButton.OK, MessageBoxImage.Warning);
                chuoiKetNoi = string.Empty;
            }

            FilterLichTap();
            LichTapDataGrid.ItemsSource = DanhSachLichTap;
        }

        // =========================================================================
        // PHẦN LOGIC DATABASE & FILTERING
        // =========================================================================

        private void FilterLichTap()
        {
            string keyword = (SearchTextBox != null && SearchTextBox.Text != null) ? SearchTextBox.Text.Trim() : string.Empty;

            DateTime? ngayTapFilter = NgayFilterDatePicker?.SelectedDate;
            string chiNhanhFilter = (ChiNhanhFilterComboBox?.SelectedItem as ComboBoxItem)?.Content.ToString();

            bool isDefaultSearch = keyword.Equals("Tìm kiếm Mã HĐ, Tên HV, Mã PT...") || string.IsNullOrWhiteSpace(keyword);

            this.DanhSachLichTap.Clear();
            if (string.IsNullOrEmpty(chuoiKetNoi)) return;

            // ĐÃ SỬA: Truy vấn các cột mới và chính xác
            string sql = @"
                SELECT
                    lt.MaLichTap, 
                    lt.MaHV,
                    hv.HoTen AS TenHocVien, 
                    hv.SDT AS SoDienThoai,
                    lt.TenBuoiTap,
                    SUBSTR(lt.ThoiGianBatDau, INSTR(lt.ThoiGianBatDau, ' ') + 3) AS NgayTap,
                    SUBSTR(lt.ThoiGianBatDau, 1, INSTR(lt.ThoiGianBatDau, ' ') - 1) AS GioBatDau,
                    SUBSTR(lt.ThoiGianKetThuc, 1, INSTR(lt.ThoiGianKetThuc, ' ') - 1) AS GioKetThuc,
                    lt.TrangThai, 
                    lt.MaCN AS ChiNhanh
                FROM LichTap lt
                INNER JOIN HocVien hv ON lt.MaHV = hv.MaHV
                WHERE 1=1 
            ";

            // Thêm điều kiện lọc theo Ngày
            if (ngayTapFilter.HasValue)
            {
                sql += " AND lt.ThoiGianBatDau LIKE @NgayTapFilter";
            }

            // Thêm điều kiện lọc theo Chi nhánh
            if (!string.IsNullOrWhiteSpace(chiNhanhFilter) && chiNhanhFilter != "Tất cả")
            {
                sql += " AND lt.MaCN = @ChiNhanh";
            }

            // Thêm điều kiện tìm kiếm chung
            if (!isDefaultSearch)
            {
                // Tìm kiếm theo Mã HV, Tên HV
                sql += $@" AND (lt.MaHV LIKE @keyword          
                             OR hv.HoTen LIKE @keyword)";
            }

            sql += " ORDER BY lt.ThoiGianBatDau DESC;";

            try
            {
                using (var connection = new SqliteConnection(chuoiKetNoi))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        if (ngayTapFilter.HasValue)
                        {
                            command.Parameters.AddWithValue("@NgayTapFilter", $"%- {ngayTapFilter.Value.ToString("dd-MM-yyyy")}");
                        }
                        if (!string.IsNullOrWhiteSpace(chiNhanhFilter) && chiNhanhFilter != "Tất cả")
                        {
                            command.Parameters.AddWithValue("@ChiNhanh", chiNhanhFilter);
                        }
                        if (!isDefaultSearch)
                        {
                            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                this.DanhSachLichTap.Add(new LichTapModel
                                {
                                    MaLichTap = reader["MaLichTap"].ToString(),
                                    MaHocVien = reader["MaHV"].ToString(),
                                    TenHocVien = reader["TenHocVien"].ToString(),
                                    SoDienThoai = reader["SoDienThoai"].ToString(),
                                    NgayTap = reader["NgayTap"].ToString(),
                                    GioBatDau = reader["GioBatDau"].ToString(),
                                    GioKetThuc = reader["GioKetThuc"].ToString(),
                                    TenBuoiTap = reader["TenBuoiTap"].ToString(),
                                    ChiNhanh = reader["ChiNhanh"].ToString(),
                                    TrangThai = reader["TrangThai"].ToString(),
                                    IsSelected = false
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LỖI TẢI DỮ LIỆU LỊCH TẬP:\nSQLite Error: {ex.Message}", "LỖI DATABASE", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData() => FilterLichTap();

        // =========================================================================
        // PHẦN LOGIC CHỌN TẤT CẢ (MỚI)
        // =========================================================================
        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox headerCheckBox = sender as CheckBox;
            if (headerCheckBox == null) return;

            bool isChecked = headerCheckBox.IsChecked == true;

            foreach (var lichTap in DanhSachLichTap)
            {
                lichTap.IsSelected = isChecked;
            }

            // Cần làm mới DataGrid để hiển thị sự thay đổi ngay lập tức
            LichTapDataGrid.Items.Refresh();
        }


        // =========================================================================
        // PHẦN LOGIC SỰ KIỆN GIAO DIỆN
        // =========================================================================

        private void NgayFilterDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterLichTap();
        }

        private void ChiNhanhFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterLichTap();
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Tìm kiếm Mã HĐ, Tên HV, Mã PT...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Tìm kiếm Mã HĐ, Tên HV, Mã PT...";
                SearchTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterLichTap();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // KHỞI TẠO VÀ HIỂN THỊ CỬA SỔ
            ThemLichTapWindow themLichTapWindow = new ThemLichTapWindow();

            // Thiết lập Action để làm mới DataGrid sau khi cửa sổ con đóng thành công
            themLichTapWindow.OnScheduleAdded = FilterLichTap; // Hoặc LoadData nếu bạn dùng LoadData

            themLichTapWindow.ShowDialog();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Chức năng Xóa đã được triển khai ở các bước trước (trong phiên bản đầy đủ). 
            // Giữ lại placeholder cũ hoặc logic Xóa hoàn chỉnh nếu bạn đã áp dụng.
            MessageBox.Show("Chức năng Xóa Lịch tập.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (NgayFilterDatePicker != null) NgayFilterDatePicker.SelectedDate = null;
            if (ChiNhanhFilterComboBox != null) ChiNhanhFilterComboBox.SelectedIndex = 0;

            if (SearchTextBox != null)
            {
                SearchTextBox.Text = string.Empty;
                SearchTextBox_LostFocus(SearchTextBox, null);
            }

            FilterLichTap();
            MessageBox.Show("Đã làm mới dữ liệu Lịch tập!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewDetailButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            // Lấy MaLichTap từ Tag của nút
            if (btn != null && btn.Tag is string maLichTap)
            {
                if (string.IsNullOrEmpty(chuoiKetNoi))
                {
                    MessageBox.Show("Không thể mở chi tiết. Lỗi kết nối Database.", "Lỗi Hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // *** LOGIC MỚI: MỞ XEMLICH TAPWINDOW ***
                XemLichTapWindow xemLichTapWindow = new XemLichTapWindow(maLichTap);

                // Thiết lập delegate để làm mới DataGrid khi dữ liệu được lưu/cập nhật
                xemLichTapWindow.OnScheduleUpdated = FilterLichTap;

                xemLichTapWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Không tìm thấy Mã Lịch tập để xem chi tiết.", "Lỗi Logic", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    // =========================================================================
    // LỚP MODEL DỮ LIỆU (Đã cập nhật các thuộc tính mới)
    // =========================================================================

    public class LichTapModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string MaLichTap { get; set; }
        public string MaHocVien { get; set; } // Mã học viên
        public string TenHocVien { get; set; }
        public string SoDienThoai { get; set; } // Số điện thoại
        public string TenBuoiTap { get; set; } // Tên lớp tập
        public string NgayTap { get; set; }
        public string GioBatDau { get; set; } // Bắt đầu
        public string GioKetThuc { get; set; } // Kết thúc
        public string ChiNhanh { get; set; }
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
}