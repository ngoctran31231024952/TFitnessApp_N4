using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using TFitnessApp;
using Microsoft.Data.Sqlite;
using TFitnessApp.Database;

namespace TFitnessApp.Windows
{
    public partial class XemThongTinHocVienWindow : Window
    {
        private string _ChuoiKetNoi;
        private readonly TruyCapDB _dbAccess;

        public XemThongTinHocVienWindow(HocVien hv)
        {
            InitializeComponent();

            // Khởi tạo đối tượng DbAccess
            _dbAccess = new TruyCapDB();
            // Lấy chuỗi kết nối
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;

            if (hv != null)
            {
                txtMaHV.Text = hv.MaHV;
                txtHoTen.Text = hv.HoTen;
                txtNgaySinh.Text = hv.NgaySinh?.ToString("dd/MM/yyyy");
                txtGioiTinh.Text = hv.GioiTinh;
                txtEmail.Text = hv.Email;
                txtSDT.Text = hv.SDT;

                LoadImage(hv.MaHV);

                LoadThongTinHopDong(hv.MaHV);
            }
        }

        private void LoadThongTinHopDong(string maHV)
        {
            try
            {
                using (SqliteConnection conn = TruyCapDB.TaoKetNoi())

                {
                    conn.Open();

                    string sqlNgayThamGia = "SELECT NgayBatDau FROM HopDong WHERE MaHV = @MaHV AND LoaiHopDong = 'Mới' ORDER BY NgayBatDau ASC LIMIT 1";
                    using (var cmd = new SqliteCommand(sqlNgayThamGia, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaHV", maHV);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
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

                    using (var cmd = new SqliteCommand(sqlThongTinGanNhat, conn))
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

