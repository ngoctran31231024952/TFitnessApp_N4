using System;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FontAwesome.Sharp;
using TFitnessApp.Windows;
//using TFitnessApp.Pages;

namespace TFitnessApp
{
    public partial class MainWindow : Window
    {
        private bool isMenuProcessing = false;
        public MainWindow(string hoTen = "Admin", string quyen = "Quản trị viên")
        {
            InitializeComponent();
            DisplayUserInfo(hoTen, quyen);
            MainFrame.Navigate(new TongQuanPage());
            MenuListBox.SelectedItem = ItemTongQuan;
            PageTitle.Text = "Tổng quan";
            this.Title = "TFitness - Tổng quan";
        }

        private void DisplayUserInfo(string hoTen, string quyen)
        {
            txtUserRole.Text = quyen;
            if (!string.IsNullOrWhiteSpace(hoTen))
            {
                string[] parts = hoTen.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2)
                {
                    string ho = parts[0];                   
                    string ten = parts[parts.Length - 1];  
                    txtUserName.Text = $"{ho} {ten}";
                    txtAvatarInitial.Text = ten.Substring(0, 1).ToUpper();
                }
                else if (parts.Length == 1)
                {
                    txtUserName.Text = parts[0];
                    txtAvatarInitial.Text = parts[0].Substring(0, 1).ToUpper();
                }
            }
            else
            {
                txtUserName.Text = "Người dùng";
                txtAvatarInitial.Text = "?";
            }

            string roleCheck = quyen.Trim().ToLower();

            if (roleCheck == "quản trị viên" || roleCheck == "admin")
            {
                ItemTaiKhoan.Visibility = Visibility.Visible;
                ItemBaoCao.Visibility = Visibility.Visible;
                BtnHelp.Visibility = Visibility.Collapsed;
            }
            else
            {
                ItemTaiKhoan.Visibility = Visibility.Collapsed;
                ItemBaoCao.Visibility = Visibility.Collapsed;
                ItemBaoCaoDoanhThu.Visibility = Visibility.Collapsed;
                ItemBaoCaoHocVien.Visibility = Visibility.Collapsed;
                BtnHelp.Visibility = Visibility.Visible;

                if (MainFrame.Content is TaiKhoanPage ||
                    MainFrame.Content is BaoCaoDoanhThuPage ||
                    MainFrame.Content is BaoCaoHocVienPage)
                {
                    MainFrame.Navigate(new TongQuanPage());
                    PageTitle.Text = "Tổng quan";
                    this.Title = "TFitness - Tổng quan";
                    MenuListBox.SelectedItem = ItemTongQuan;
                }
            }
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
            contextMenu.HorizontalOffset = -20;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            SystemSounds.Asterisk.Play();
            var result = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận",
                                         MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.OK)
            {
                LoginWindow loginScreen = new LoginWindow();
                loginScreen.Show();
                this.Close();
            }
        }

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = MenuListBox.SelectedItem as ListBoxItem;
            if (selectedItem == null || PageTitle == null) return;

            if (selectedItem.Name != "ItemBaoCao")
            {
                ResetBaoCaoMenuState();
            }

            string pageTitle = "Tổng quan";
            string windowTitle = "Tổng quan";

            switch (selectedItem.Name)
            {
                case "ItemTongQuan":
                    pageTitle = "Tổng quan";
                    MainFrame.Navigate(new TongQuanPage());
                    break;
                case "ItemGoiTap":
                    pageTitle = "Quản lý Gói tập"; windowTitle = "Gói tập";
                    MainFrame.Navigate(new GoiTapPage());
                    break;
                case "ItemLichTap":
                    pageTitle = "Quản lý Lịch tập"; windowTitle = "Lịch tập";
                    MainFrame.Navigate(new LichTapPage());
                    break;
                case "ItemHocVien":
                    pageTitle = "Quản lý Học viên"; windowTitle = "Học viên";
                    MainFrame.Navigate(new HocVienPage());
                    break;
                case "ItemPT":
                    pageTitle = "Quản lý PT";
                    windowTitle = "PT";
                    MainFrame.Navigate(new PTPage());
                    break;
                case "ItemHopDong":
                    pageTitle = "Quản lý Hợp đồng"; windowTitle = "Hợp đồng";
                    MainFrame.Navigate(new HopDongPage());
                    break;
                case "ItemGiaoDich":
                    pageTitle = "Quản lý Giao dịch"; windowTitle = "Giao dịch";
                    MainFrame.Navigate(new GiaoDichPage());
                    break;
                case "ItemTaiKhoan":
                    pageTitle = "Quản lý Tài khoản"; windowTitle = "Tài khoản";
                    MainFrame.Navigate(new TaiKhoanPage());
                    break;
                case "ItemDiemDanh":
                    pageTitle = "Quản lý Điểm danh"; windowTitle = "Điểm danh";
                    MainFrame.Navigate(new DiemDanhPage());
                    break;
                case "ItemChiSoSucKhoe":
                    pageTitle = "Quản lý Chỉ số sức khỏe"; windowTitle = "Chỉ số sức khỏe";
                    MainFrame.Navigate(new CSSKPage());
                    break;

                case "ItemBaoCaoDoanhThu":
                    pageTitle = "Báo cáo Doanh thu"; windowTitle = "Báo cáo doanh thu";
                    MainFrame.Navigate(new BaoCaoDoanhThuPage());
                    break;
                case "ItemBaoCaoHocVien":
                    pageTitle = "Báo cáo Học viên"; windowTitle = "Báo cáo học viên";
                    MainFrame.Navigate(new BaoCaoHocVienPage());
                    break;

                case "ItemBaoCao":
                    return;
            }

            PageTitle.Text = pageTitle;
            this.Title = "TFitness - " + windowTitle;
        }

        private void HamburgerButton_Checked(object sender, RoutedEventArgs e)
        {
            ItemBaoCaoDoanhThu.Visibility = Visibility.Collapsed;
            ItemBaoCaoHocVien.Visibility = Visibility.Collapsed;
            IconChevronBaoCao.Icon = IconChar.ChevronRight;

            if (MenuListBox.SelectedItem == ItemBaoCaoDoanhThu || MenuListBox.SelectedItem == ItemBaoCaoHocVien)
            {
                MenuListBox.SelectedItem = ItemBaoCao;
            }
        }

        private void HamburgerButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (PageTitle.Text.Contains("Báo cáo"))
            {
                ItemBaoCaoDoanhThu.Visibility = Visibility.Visible;
                ItemBaoCaoHocVien.Visibility = Visibility.Visible;
                IconChevronBaoCao.Icon = IconChar.ChevronDown;

                if (PageTitle.Text == "Báo cáo Doanh thu") MenuListBox.SelectedItem = ItemBaoCaoDoanhThu;
                if (PageTitle.Text == "Báo cáo Học viên") MenuListBox.SelectedItem = ItemBaoCaoHocVien;
            }
        }

        private async void ItemBaoCao_MouseEnter(object sender, MouseEventArgs e)
        {
            if (HamburgerButton.IsChecked != true) return;

            if (isMenuProcessing || (ItemBaoCao.ContextMenu != null && ItemBaoCao.ContextMenu.IsOpen))
                return;

            try
            {
                isMenuProcessing = true;

                var contextMenu = ItemBaoCao.ContextMenu;
                if (contextMenu != null)
                {
                    MenuConDoanhThu.IsChecked = (PageTitle.Text == "Báo cáo Doanh thu");
                    MenuConHocVien.IsChecked = (PageTitle.Text == "Báo cáo Học viên");
                    contextMenu.PlacementTarget = ItemBaoCao;
                    contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
                    contextMenu.HorizontalOffset = 5;
                    contextMenu.VerticalOffset = 0;
                    contextMenu.Visibility = Visibility.Visible;
                    contextMenu.IsOpen = true;
                    await MonitorContextMenuClose(contextMenu);
                }
            }
            finally
            {
                isMenuProcessing = false;
            }
        }

        private async Task MonitorContextMenuClose(ContextMenu menu)
        {
            await Task.Delay(200);

            while (menu.IsOpen)
            {
                await Task.Delay(100);
                if (!IsMouseOverUIElement(ItemBaoCao) && !IsMouseOverUIElement(menu))
                {
                    menu.IsOpen = false;
                    break;
                }
            }
        }

        private bool IsMouseOverUIElement(FrameworkElement element)
        {
            if (element == null || !element.IsVisible) return false;
            Point p = Mouse.GetPosition(element);
            return (p.X >= 0 && p.X <= element.ActualWidth && p.Y >= 0 && p.Y <= element.ActualHeight);
        }


        private void ItemBaoCao_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (HamburgerButton.IsChecked == true)
            {
                ItemBaoCao_MouseEnter(sender, null);
            }
            else
            {
                if (ItemBaoCaoDoanhThu.Visibility == Visibility.Collapsed)
                {
                    ItemBaoCaoDoanhThu.Visibility = Visibility.Visible;
                    ItemBaoCaoHocVien.Visibility = Visibility.Visible;
                    IconChevronBaoCao.Icon = IconChar.ChevronDown;
                }
                else
                {
                    ItemBaoCaoDoanhThu.Visibility = Visibility.Collapsed;
                    ItemBaoCaoHocVien.Visibility = Visibility.Collapsed;
                    IconChevronBaoCao.Icon = IconChar.ChevronRight;
                }
            }
            e.Handled = true;
        }

        private void MenuBaoCaoItem_Click(object sender, RoutedEventArgs e)
        {
            var clickedItem = sender as MenuItem;
            if (clickedItem == null) return;
            e.Handled = true;
            var parentMenu = ItemsControl.ItemsControlFromItemContainer(clickedItem) as ContextMenu;
            if (parentMenu != null)
            {
                foreach (var item in parentMenu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        menuItem.IsChecked = (menuItem == clickedItem);
                    }
                }
                parentMenu.IsOpen = false;
            }
            string header = clickedItem.Header.ToString();
            string newTitle = "";

            if (header == "Báo cáo doanh thu")
            {
                newTitle = "Báo cáo Doanh thu";
                MainFrame.Navigate(new BaoCaoDoanhThuPage());
            }
            else if (header == "Báo cáo học viên")
            {
                newTitle = "Báo cáo Học viên";
                MainFrame.Navigate(new BaoCaoHocVienPage());
            }

            if (!string.IsNullOrEmpty(newTitle))
            {
                PageTitle.Text = newTitle;
                this.Title = "TFitness - " + newTitle;
                MenuListBox.SelectedItem = ItemBaoCao;
            }
        }

        private void ResetBaoCaoMenuState()
        {
            if (ItemBaoCao.ContextMenu != null)
            {
                foreach (var item in ItemBaoCao.ContextMenu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        menuItem.IsChecked = false;
                    }
                }
            }
        }
        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item != null)
            {
                if (item.Name == "ItemBaoCao") return;
                item.IsSelected = true;
                e.Handled = true;
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpPopup.HorizontalOffset = -100;
            HelpPopup.VerticalOffset = 18;
            HelpPopup.IsOpen = true;
        }


    }
}