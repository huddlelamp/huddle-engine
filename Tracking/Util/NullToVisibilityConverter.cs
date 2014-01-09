using System;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Tools.FlockingDevice.Tracking.InputSource;

namespace Tools.FlockingDevice.Tracking.Util
{
    [ValueConversion(typeof(IInputSource), typeof(Visibility))]
    public class NullToVisibilityConverter : ConverterMarkupExtension<NullToVisibilityConverter>, IValueConverter
    {
        #region ctor

// ReSharper disable once EmptyConstructor
        public NullToVisibilityConverter()
        {
            // empty
        }

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
