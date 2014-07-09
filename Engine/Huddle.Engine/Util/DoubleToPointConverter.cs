using System;
using System.Windows;
using System.Windows.Data;

namespace Huddle.Engine.Util
{
    [ValueConversion(typeof(double), typeof(Point))]
    public class DoubleToPointConverter : ConverterMarkupExtension<DoubleToPointConverter>, IValueConverter, IMultiValueConverter
    {
        public DoubleToPointConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var d = (double)value;

            return new Point(d, d);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(values[0] is double))
            {
                return 0.0;
            }

            var x = (double)(values[0]);
            var y = (double)(values[1]);

            return new Point(x, y);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
