using System;
using System.Windows;
using TFitnessApp;

namespace TFitnessApp.Windows
{
    public partial class XemThongTinGoiTapWindow : Window
    {
        public XemThongTinGoiTapWindow(GoiTap gt)
        {
            InitializeComponent();
            if (gt != null)
            {
                txtMaGoi.Text = gt.MaGoi;
                txtTenGoi.Text = gt.TenGoi;
                txtThoiHan.Text = gt.ThoiHan.ToString();
                txtGia.Text = gt.GiaNiemYetFormatted;
                txtSoBuoiPT.Text = gt.SoBuoiPT.ToString();
                txtDichVu.Text = gt.DichVuDacBiet;
                txtTrangThai.Text = gt.TrangThai;
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}