using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Huddle.Engine.Util
{
    [ValueConversion(typeof(double), typeof(double))]
    public class ScaleConverter : MultiConverterMarkupExtension<ScaleConverter>, IMultiValueConverter
    {
        #region ctor

        // ReSharper disable once EmptyConstructor
        public ScaleConverter()
        {
            // emtpy
        }

        #endregion

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (Equals(values[0], DependencyProperty.UnsetValue)) return 0;

            var relativeValue = (double)values[0];
            var scale = (double)values[1];
            return relativeValue * scale;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
