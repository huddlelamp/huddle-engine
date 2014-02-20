using System;
using System.Globalization;
using System.Windows.Data;

namespace Huddle.Engine.Util
{
    [ValueConversion(typeof(object), typeof(object))]
    public class ObjectPropertyConverter : ConverterMarkupExtension<ObjectPropertyConverter>, IValueConverter
    {
        #region properties

        public string Path { get; set; }

        #endregion

        #region ctor

        // ReSharper disable once EmptyConstructor
        public ObjectPropertyConverter()
        {
            // emtpy
        }

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetProperty(value, Path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static object GetProperty(object value, string path)
        {
            var currentType = value.GetType();

            foreach (var propertyName in path.Split('.'))
            {
                var property = currentType.GetProperty(propertyName);
                value = property.GetValue(value, null);
                currentType = property.PropertyType;
            }
            return value;
        }
    }
}
