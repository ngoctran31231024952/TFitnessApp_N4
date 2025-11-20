using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TFitnessApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new TongQuanPage());
        }

        private void LogoButton_Click(object sender, RoutedEventArgs e)
        {
            MenuListBox.SelectedIndex = 0;
        }

        private void AvatarButton_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu contextMenu = AvatarButton.ContextMenu;
            contextMenu.PlacementTarget = AvatarButton;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = MenuListBox.SelectedItem as ListBoxItem;
            if (selectedItem == null || PageTitle == null)
            {
                return;
            }

            string pageTitle = "Tổng quan";
            string windowTitle = "Tổng quan";

            switch (selectedItem.Name)
            {
                case "ItemTongQuan":
                    pageTitle = "Tổng quan";
                    windowTitle = "Tổng quan";
                    MainFrame.Navigate(new TongQuanPage());
                    break;

                case "ItemGoiTap":
                    pageTitle = "Quản lý Gói tập";
                    windowTitle = "Gói tập";
                    MainFrame.Navigate(new GoiTapPage());
                    break;

                case "ItemLichTap":
                    pageTitle = "Quản lý Lịch tập";
                    windowTitle = "Lịch tập";
                    MainFrame.Navigate(new LichTapPage());
                    break;

                case "ItemHocVien":
                    pageTitle = "Quản lý Học viên";
                    windowTitle = "Học viên";
                    MainFrame.Navigate(new HocVienPage());
                    break;

                case "ItemHopDong":
                    pageTitle = "Quản lý Hợp đồng";
                    windowTitle = "Hợp đồng";
                    MainFrame.Navigate(new HopDongPage());
                    break;

                case "ItemGiaoDich":
                    pageTitle = "Quản lý Giao dịch";
                    windowTitle = "Giao dịch";
                    MainFrame.Navigate(new GiaoDichPage());
                    break;

                case "ItemTaiKhoan":
                    pageTitle = "Quản lý Tài khoản";
                    windowTitle = "Tài khoản";
                    MainFrame.Navigate(new TaiKhoanPage());
                    break;

                case "ItemDiemDanh":
                    pageTitle = "Quản lý Điểm danh";
                    windowTitle = "Điểm danh";
                    MainFrame.Navigate(new DiemDanhPage());
                    break;

                case "ItemChiSoSucKhoe":
                    pageTitle = "Quản lý Chỉ số sức khỏe";
                    windowTitle = "Chỉ số sức khỏe";
                    MainFrame.Navigate(new CSSKPage());
                    break;
            }

            PageTitle.Text = pageTitle;
            this.Title = "TFitness - " + windowTitle;
    }
    }
}