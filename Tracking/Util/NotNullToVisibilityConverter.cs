using System;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Tools.FlockingDevice.Tracking.InputSource;

namespace Tools.FlockingDevice.Tracking.Util
{
    [ValueConversion(typeof(IInputSource), typeof(Visibility))]
    public class NotNullToVisibilityConverter : ConverterMarkupExtension<NotNullToVisibilityConverter>, IValueConverter
    {
        #region properties

        private double _invert = 1.7;

        public double Invert
        {
            get { return _invert; }
            set { _invert = value; }
        }

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
