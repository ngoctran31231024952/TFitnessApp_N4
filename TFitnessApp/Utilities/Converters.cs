using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel; // Required for ObservableObject

namespace TFitnessApp.Utilities // ĐÃ SỬA: Namespace bây giờ phải là TFitnessApp.Utilities
{
    // ----------------------------------------------------------------------
    // BASE CLASS: Cần thiết cho mọi lớp Model/ViewModel
    // ----------------------------------------------------------------------
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ----------------------------------------------------------------------
    // CONVERTERS: Dùng để chuyển đổi giá trị cho UI (IValueConverter)
    // ----------------------------------------------------------------------

    /// <summary>
    /// Converts a string value to Visibility. Used for Placeholder Text.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Returns Visible if the string is null/empty/whitespace, otherwise Collapsed.
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// Converts a boolean value to Visibility. Inverts the value.
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue && booleanValue)
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// Converts a Boolean to a text string.
    /// </summary>
    public class BooleanToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue && booleanValue)
            {
                return "Chỉnh Sửa Thông Tin Thành Viên";
            }
            return "Thêm Thành Viên Mới";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}