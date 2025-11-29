using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace TFitnessApp
{
    public partial class DiemDanhPage : Page
    {
        private string connectionString;
        private List<DiemDanhViewModel> allDiemDanh = new List<DiemDanhViewModel>();

        public DiemDanhPage()
        {
            InitializeComponent();

            // Khởi tạo connection string
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitnessDB.db");
            connectionString = $"Data Source={dbPath};";

            // Kiểm tra file database
            if (!File.Exists(dbPath))
            {
                MessageBox.Show($"Không tìm thấy database tại:\n{dbPath}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Load dữ liệu
            LoadStatistics();
            LoadDiemDanhList();
        }

        private void LoadStatistics()
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string today = DateTime.Today.ToString("yyyy-MM-dd");

                    // 1. Điểm danh hôm nay
                    string sql1 = "SELECT COUNT(DISTINCT MaHV) FROM DiemDanh WHERE date(NgayDD) = date(@today)";
                    using (SqliteCommand cmd = new SqliteCommand(sql1, conn))
                    {
                        cmd.Parameters.AddWithValue("@today", today);
                        txtSoDiemDanhHomNay.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // 2. Đang tập luyện (có ThoiGianVao nhưng chưa có ThoiGianRa)
                    string sql2 = @"SELECT COUNT(DISTINCT MaHV) FROM DiemDanh 
                                   WHERE date(NgayDD) = date(@today) 
                                   AND ThoiGianVao IS NOT NULL 
                                   AND (ThoiGianRa IS NULL OR ThoiGianRa = '')";
                    using (SqliteCommand cmd = new SqliteCommand(sql2, conn))
                    {
                        cmd.Parameters.AddWithValue("@today", today);
                        txtDangTapLuyen.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // 3. Đã hoàn thành
                    string sql3 = "SELECT COUNT(*) FROM LichTap WHERE TrangThai = 'Đã Hoàn Thành'";
                    using (SqliteCommand cmd = new SqliteCommand(sql3, conn))
                    {
                        txtDaHoanThanh.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // 4. Thời gian TB (tính bằng phút)
                    string sql4 = @"SELECT AVG(
                                      CAST((julianday(datetime(NgayDD || ' ' || ThoiGianRa)) - 
                                            julianday(datetime(NgayDD || ' ' || ThoiGianVao))) * 24 AS REAL)
                                    ) 
                                    FROM DiemDanh 
                                    WHERE ThoiGianRa IS NOT NULL 
                                    AND ThoiGianRa != '' 
                                    AND ThoiGianVao IS NOT NULL";
                    using (SqliteCommand cmd = new SqliteCommand(sql4, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            double avgHours = Convert.ToDouble(result);
                            txtThoiGianTB.Text = $"{avgHours:F1}h";
                        }
                        else
                        {
                            txtThoiGianTB.Text = "0h";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thống kê: {ex.Message}", "Lỗi");
                SetDefaultStatistics();
            }
        }

        private void SetDefaultStatistics()
        {
            txtSoDiemDanhHomNay.Text = "0";
            txtDangTapLuyen.Text = "0";
            txtDaHoanThanh.Text = "0";
            txtThoiGianTB.Text = "0h";
        }

        private void LoadDiemDanhList()
        {
            try
            {
                allDiemDanh.Clear();

                using (SqliteConnection conn = new SqliteConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT 
                                    d.MaDD,
                                    d.MaHV,
                                    h.HoTen,
                                    d.NgayDD,
                                    d.ThoiGianVao,
                                    d.ThoiGianRa,
                                    CASE 
                                        WHEN d.ThoiGianRa IS NULL OR d.ThoiGianRa = '' THEN 'Check-in'
                                        ELSE 'Check-out'
                                    END as TrangThai
                                  FROM DiemDanh d
                                  INNER JOIN HocVien h ON d.MaHV = h.MaHV
                                  ORDER BY d.NgayDD DESC, d.ThoiGianVao DESC";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new DiemDanhViewModel
                            {
                                MaDiemDanh = reader["MaDD"].ToString(),
                                MaHocVien = reader["MaHV"].ToString(),
                                HoTen = reader["HoTen"].ToString(),
                                NgayDiemDanh = DateTime.Parse(reader["NgayDD"].ToString()).ToString("dd/MM/yyyy"),
                                ThoiGianVao = reader["ThoiGianVao"]?.ToString() ?? "",
                                ThoiGianRa = reader["ThoiGianRa"]?.ToString() ?? "",
                                TrangThai = reader["TrangThai"].ToString()
                            };

                            // Lấy chữ cái đầu làm Avatar
                            item.Avatar = string.IsNullOrEmpty(item.HoTen) ? "?" : item.HoTen[0].ToString().ToUpper();

                            // Set màu badge
                            item.TrangThaiBadgeColor = item.TrangThai == "Check-in" ? "#51E689" : "#FF974E";

                            allDiemDanh.Add(item);
                        }
                    }
                }

                lvLichSuDiemDanh.ItemsSource = allDiemDanh;

                MessageBox.Show($"Đã tải {allDiemDanh.Count} bản ghi điểm danh", "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearch == null) return;

            string searchText = txtSearch.Text.ToLower().Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                lvLichSuDiemDanh.ItemsSource = allDiemDanh;
            }
            else
            {
                var filtered = new List<DiemDanhViewModel>();
                foreach (var item in allDiemDanh)
                {
                    if (item.HoTen.ToLower().Contains(searchText) ||
                        item.MaHocVien.ToLower().Contains(searchText))
                    {
                        filtered.Add(item);
                    }
                }
                lvLichSuDiemDanh.ItemsSource = filtered;
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.Tag as DiemDanhViewModel;

            if (item != null)
            {
                MessageBox.Show($"Chỉnh sửa điểm danh:\n\nHọc viên: {item.HoTen}\nMã: {item.MaHocVien}\nNgày: {item.NgayDiemDanh}",
                    "Thông tin");
            }
        }
    }

    // ViewModel class
    public class DiemDanhViewModel
    {
        public string MaDiemDanh { get; set; }
        public string MaHocVien { get; set; }
        public string HoTen { get; set; }
        public string Avatar { get; set; }
        public string NgayDiemDanh { get; set; }
        public string ThoiGianVao { get; set; }
        public string ThoiGianRa { get; set; }
        public string TrangThai { get; set; }
        public string TrangThaiBadgeColor { get; set; }
    }
}