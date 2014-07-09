using System;
using System.Globalization;
using System.Windows.Data;

namespace Huddle.Engine.Util
{
    [ValueConversion(typeof(object), typeof(bool))]
    public class NullToBoolConverter : ConverterMarkupExtension<NullToBoolConverter>, IValueConverter
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
        public NullToBoolConverter()
        {
            // emtpy
        }

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
