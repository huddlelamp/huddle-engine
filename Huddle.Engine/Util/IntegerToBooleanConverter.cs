using System;
using System.Windows.Data;

namespace Huddle.Engine.Util
{
    public class IntegerToBooleanConverter : ConverterMarkupExtension<IntegerToBooleanConverter>, IValueConverter
    {
        // ReSharper disable once EmptyConstructor
        public IntegerToBooleanConverter()
        {
            // empty
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var number = (int)value;

            int threshold = 0;

            if (parameter != null)
                threshold = int.Parse((String)parameter);

            return number > threshold;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
