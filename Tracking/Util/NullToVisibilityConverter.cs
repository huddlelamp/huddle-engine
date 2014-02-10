using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tools.FlockingDevice.Tracking.Util
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class NullToVisibilityConverter : ConverterMarkupExtension<NullToVisibilityConverter>, IValueConverter
    {
        #region properties

        public bool Invert { get; set; }

        #endregion

        #region ctor

        // ReSharper disable once EmptyConstructor
        public NullToVisibilityConverter()
        {
            // emtpy
        }

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Invert)
                return value == null ? Visibility.Visible : Visibility.Collapsed;

            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
