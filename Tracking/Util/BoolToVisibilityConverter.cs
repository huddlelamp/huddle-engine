using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tools.FlockingDevice.Tracking.Util
{
    [ValueConversion(typeof(Visibility), typeof(bool))]
    public class BoolToVisibilityConverter : ConverterMarkupExtension<BoolToVisibilityConverter>, IValueConverter
    {
        #region properties

        public bool Invert { get; set; }

        #endregion

        #region ctor

        // ReSharper disable once EmptyConstructor
        public BoolToVisibilityConverter()
        {
            Invert = false;
            // emtpy
        }

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolean = (bool) value;

            if (Invert)
                return boolean ? Visibility.Collapsed : Visibility.Visible;

            return boolean ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
