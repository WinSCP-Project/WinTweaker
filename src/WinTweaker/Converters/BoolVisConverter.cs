using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WinTweaker.Converters
{
    public sealed class BoolVisConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool on = value is bool && (bool)value;
            if (Invert)
                on = !on;
            return on ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
