using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using TFitnessApp;
using Microsoft.Data.Sqlite; // Cần thiết để kết nối CSDL

namespace TFitnessApp.Windows
{
    public partial class XemThongTinHocVienWindow : Window
    {
        private string _dbPath;

        public XemThongTinHocVienWindow(HocVien hv)
        {
            InitializeComponent();

            // Khởi tạo đường dẫn CSDL
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            if (!File.Exists(_dbPath))
            {
                _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TFitness.db");
            }

            if (hv != null)
            {
                // Hiển thị thông tin cơ bản từ đối tượng HocVien
                txtMaHV.Text = hv.MaHV;
                txtHoTen.Text = hv.HoTen;
                txtNgaySinh.Text = hv.NgaySinh?.ToString("dd/MM/yyyy");
                txtGioiTinh.Text = hv.GioiTinh;
                txtEmail.Text = hv.Email;
                txtSDT.Text = hv.SDT;

                // Tải ảnh
                LoadImage(hv.MaHV);

                // Tải thông tin bổ sung từ CSDL (Hợp đồng, Gói, PT, Chi nhánh)
                LoadThongTinHopDong(hv.MaHV);
            }
        }

        // Hàm truy xuất thông tin bổ sung từ CSDL
        private void LoadThongTinHopDong(string maHV)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();

                    // 1. Lấy Ngày tham gia (Ngày bắt đầu của hợp đồng loại 'Mới' đầu tiên)
                    string sqlNgayThamGia = "SELECT NgayBatDau FROM HopDong WHERE MaHV = @MaHV AND LoaiHopDong = 'Mới' ORDER BY NgayBatDau ASC LIMIT 1";
                    using (var cmd = new SqliteCommand(sqlNgayThamGia, connection))
                    {
                        cmd.Parameters.AddWithValue("@MaHV", maHV);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            // Chuyển đổi format ngày tháng cho đẹp (giả sử DB lưu yyyy-MM-dd)
                            if (DateTime.TryParse(result.ToString(), out DateTime date))
                            {
                                txtNgayThamGia.Text = date.ToString("dd-MM-yyyy");
                            }
                            else
                            {
                                txtNgayThamGia.Text = result.ToString();
                            }
                        }
                        else
                        {
                            txtNgayThamGia.Text = "Chưa có";
                        }
                    }

                    // 2. Lấy thông tin Hợp đồng gần nhất (Gói tập, PT, Chi nhánh)
                    // Kết nối bảng HopDong với GoiTap, PT, ChiNhanh để lấy tên
                    string sqlThongTinGanNhat = @"
                        SELECT 
                            g.TenGoi, 
                            p.HoTen AS TenPT, 
                            c.TenCN
                        FROM HopDong h
                        LEFT JOIN GoiTap g ON h.MaGoi = g.MaGoi
                        LEFT JOIN PT p ON h.MaPT = p.MaPT
                        LEFT JOIN ChiNhanh c ON h.MaCN = c.MaCN
                        WHERE h.MaHV = @MaHV
                        ORDER BY h.NgayBatDau DESC
                        LIMIT 1";

                    using (var cmd = new SqliteCommand(sqlThongTinGanNhat, connection))
                    {
                        cmd.Parameters.AddWithValue("@MaHV", maHV);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtGoiTap.Text = reader["TenGoi"] != DBNull.Value ? reader["TenGoi"].ToString() : "Không có";
                                txtPT.Text = reader["TenPT"] != DBNull.Value ? reader["TenPT"].ToString() : "Không có";
                                txtChiNhanh.Text = reader["TenCN"] != DBNull.Value ? reader["TenCN"].ToString() : "Không có";
                            }
                            else
                            {
                                txtGoiTap.Text = "Chưa đăng ký";
                                txtPT.Text = "--";
                                txtChiNhanh.Text = "--";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu lỗi thì hiện thông báo debug hoặc set giá trị mặc định
                System.Diagnostics.Debug.WriteLine("Lỗi load thông tin hợp đồng: " + ex.Message);
                txtNgayThamGia.Text = "Lỗi tải";
                txtGoiTap.Text = "Lỗi tải";
            }
        }

        private void LoadImage(string maHV)
        {
            try
            {
                string[] extensions = { ".jpg", ".png", ".jpeg" };
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HocVienImages");
                foreach (string ext in extensions)
                {
                    string filePath = Path.Combine(folderPath, $"{maHV}{ext}");
                    if (File.Exists(filePath))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(filePath);
                        bitmap.EndInit();
                        imgAvatar.Source = bitmap;
                        break;
                    }
                }
            }
            catch { }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

