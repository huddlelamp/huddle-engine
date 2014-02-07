using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace Tools.FlockingDevice.Tracking.Util
{
    #region Abstract Converter Implementation

    [MarkupExtensionReturnType(typeof(IValueConverter))]
    public abstract class ConverterMarkupExtension<T> : MarkupExtension where T : class, IValueConverter, new()
    {
        //private static T _converter;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            //return _converter ?? (_converter = new T());
            return this;
        }
    }

    #endregion
}
