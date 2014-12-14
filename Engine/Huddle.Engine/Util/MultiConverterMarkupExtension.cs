using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace Huddle.Engine.Util
{
    #region Abstract Converter Implementation

    [MarkupExtensionReturnType(typeof(IMultiValueConverter))]
    public abstract class MultiConverterMarkupExtension<T> : MarkupExtension where T : class, IMultiValueConverter, new()
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
