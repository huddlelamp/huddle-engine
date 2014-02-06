using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Emgu.CV;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.Util
{
    [ValueConversion(typeof(bool), typeof(PipelineMode))]
    public class PipelineModeToBoolConverter : ConverterMarkupExtension<PipelineModeToBoolConverter>, IValueConverter
    {
        #region

        public PipelineMode Mode { get; set; }

        #endregion

        #region ctor

        // ReSharper disable once EmptyConstructor
        public PipelineModeToBoolConverter()
        {
            // empty
        }

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mode = (PipelineMode)value;

            return Equals(Mode, mode);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
