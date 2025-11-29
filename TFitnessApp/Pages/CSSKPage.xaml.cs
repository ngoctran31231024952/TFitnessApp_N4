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
    public partial class CSSKPage : UserControl
    {
        public CSSKPage()
        {
            InitializeComponent();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BackButton_Click(object sender, MouseButtonEventArgs e)
        {

        }

        private void SavePopup_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PageNumber_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SelectAllCheckBox_Changed(object sender, RoutedEventArgs e)
        {

        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void CancelPopup_Click(object sender, RoutedEventArgs e)
        {
            
        }
        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            // Logic xử lý khi người dùng nhấn nút chuyển sang trang tiếp theo
        }
        private void DeleteSingle_Click(object sender, RoutedEventArgs e)
        {
           
            var button = sender as Button;
            var dataItem = button?.Tag;          
        }
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

      
        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        
        private void Add_Click(object sender, RoutedEventArgs e)
        {
          
        }

       
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            
        }

     
        private void ViewDetail_Click(object sender, RoutedEventArgs e)
        {
            
        }


        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}