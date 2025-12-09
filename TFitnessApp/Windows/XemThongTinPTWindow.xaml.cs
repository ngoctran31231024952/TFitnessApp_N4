using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using TFitnessApp;

namespace TFitnessApp.Windows
{
    public partial class XemThongTinPTWindow : Window
    {
        public XemThongTinPTWindow(PT pt)
        {
            InitializeComponent();
            if (pt != null)
            {
                txtMaPT.Text = pt.MaPT;
                txtHoTen.Text = pt.HoTen;
                txtChiNhanh.Text = pt.TenCN;
                txtGioiTinh.Text = pt.GioiTinh;
                txtEmail.Text = pt.Email;
                txtSDT.Text = pt.SDT;
                LoadImage(pt.MaPT);
            }
        }
        private void LoadImage(string maPT)
        {
            try
            {
                string[] extensions = { ".jpg", ".png", ".jpeg" };
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PTImages");
                foreach (string ext in extensions)
                {
                    string filePath = Path.Combine(folderPath, $"{maPT}{ext}");
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