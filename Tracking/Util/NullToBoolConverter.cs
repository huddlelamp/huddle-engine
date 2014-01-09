using System;
using System.Globalization;
using System.Windows.Data;
using Tools.FlockingDevice.Tracking.InputSource;

namespace Tools.FlockingDevice.Tracking.Util
{
    [ValueConversion(typeof(IInputSource), typeof(bool))]
    public class NullToBoolConverter : ConverterMarkupExtension<NullToBoolConverter>, IValueConverter
    {
        #region ctor

// ReSharper disable once EmptyConstructor
        public NullToBoolConverter()
        {
            // empty
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
