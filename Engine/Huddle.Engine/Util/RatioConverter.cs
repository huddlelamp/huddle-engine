using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Huddle.Engine.Util
{
    [ValueConversion(typeof(double), typeof(double))]
    public class RatioConverter : MultiConverterMarkupExtension<ScaleConverter>, IMultiValueConverter
    {
        #region ctor

        // ReSharper disable once EmptyConstructor
        public RatioConverter()
        {
            // emtpy
        }

        #endregion

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (Equals(values[0], DependencyProperty.UnsetValue)) return 0;

            var ratio = (double)values[0];
            var scale = (double)values[1];
            return (1 / ratio) * scale;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
