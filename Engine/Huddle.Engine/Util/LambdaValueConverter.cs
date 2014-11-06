using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace Huddle.Engine.Util
{
    /// <summary>
    /// Value converter that is created dynamically with a Lambda Expression
    /// </summary>
    [ValueConversion(typeof(String), typeof(Object))]
    public class LambdaValueConverter : IValueConverter
    {
        public LambdaValueConverter(String expression)
        {
            Expression = expression;
        }

        //Lambda parser that will parse the given string into a lambda expression
        LambdaParser parser = null;

        /// <summary>
        /// gets or sets the expression to parse
        /// </summary>
        public string Expression { get; set; }

        Delegate func = null;

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (Expression == null)
                return value;

            if (parser == null)
            {
                parser = new LambdaParser(Expression, true);
                //set a default
                parser.Params = new LambdaParameter[]
                {
                    new LambdaParameter { ParamType = targetType, ParamName = "param" }
                };
                //parse the expression
                func = parser.ParseExpression().Compile();
            }

            //if the user wants to use the value as a string we need to change the object type
            if (targetType == typeof(string))
                value = value.ToString();

            return func.DynamicInvoke(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Markup extension to be able to form a converter from a Lambda expression
    /// </summary>
    [MarkupExtensionReturnType(typeof(IValueConverter))]
    public class LambdaValueConverterExtension : MarkupExtension
    {
        private string expression;
        /// <summary>
        /// constructor to build the expression
        /// </summary>
        /// <param name="expression">The expression to build</param>
        public LambdaValueConverterExtension(string expression)
        {
            this.expression = expression;
        }

        //Returns a LambdaValueConverter
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new LambdaValueConverter(expression);
        }
    }


}
