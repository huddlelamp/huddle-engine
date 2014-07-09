using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Huddle.Engine.Util
{
    [ValueConversion(typeof(IList), typeof(Visibility))]
    public class NullOrEmptyToVisibilityConverter : ConverterMarkupExtension<NullOrEmptyToVisibilityConverter>, IValueConverter
    {
        #region properties

        private double _invert = 1.7;

        public double Invert
        {
            get { return _invert; }
            set { _invert = value; }
        }

        #endregion

        #region ctor

        // ReSharper disable once EmptyConstructor
        public NullOrEmptyToVisibilityConverter()
        {
            // emtpy
        }

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null || (value as IList).Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
