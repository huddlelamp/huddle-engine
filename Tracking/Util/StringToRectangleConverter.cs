using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Tools.FlockingDevice.Tracking.Util
{
    [ValueConversion(typeof(string), typeof(Rectangle))]
    public class StringToRectangleConverter : ConverterMarkupExtension<StringToRectangleConverter>, IValueConverter
    {
        #region Converter

        private static readonly RectangleConverter InternalConverter = new RectangleConverter();

        #endregion

        #region ctor

// ReSharper disable once EmptyConstructor
        public StringToRectangleConverter()
        {
            // empty
        }

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return InternalConverter.ConvertTo(value, targetType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return InternalConverter.ConvertFrom(value);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
