using System;
using System.Linq.Expressions;

namespace Huddle.Engine.Util
{
    /// <summary>
    /// Data structure to create lambda parameters
    /// </summary>
    public class LambdaParameter
    {
        /// <summary>
        /// Gets or sets the type of parameter
        /// </summary>
        public Type ParamType { get; set; }

        /// <summary>
        /// Gets or sets the name of the parameter
        /// </summary>
        public string ParamName { get; set; }
    }

    /// <summary>
    /// Base class for creating Lambda markup extensions 
    /// </summary>
    public class LambdaParser
    {
        public LambdaParameter[] Params { get; set; }

        string expression;
        /// <summary>
        /// Gets or sets the expression text to compile
        /// Note: If you need to use a string in the expression use the $ sign instead of the " sign
        /// </summary>
        public string Expression
        {
            get { return expression; }
            set
            {
                expression = value.Replace("$", "\"");
            }
        }

        /// <summary>
        /// Gets a flag indicating if this lambda should have a return value
        /// </summary>
        public bool HasReturnValue { get; private set; }

        /// <summary>
        /// constructor to build the expression
        /// </summary>
        /// <param name="expression">The expression to build</param>
        /// <param name="hasReturnValue">Flag indicating if the lambda should have a return value</param>
        public LambdaParser(string expression, bool hasReturnValue)
        {
            Expression = expression;
            HasReturnValue = hasReturnValue;
        }

        //generate a lambda expression from the Expression set
        public LambdaExpression ParseExpression()
        {
            ParameterExpression[] parameters = new ParameterExpression[Params.Length];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = System.Linq.Expressions.Expression.Parameter(
                    Params[i].ParamType, Params[i].ParamName);

            return System.Linq.Dynamic.DynamicExpression.ParseLambda(parameters,
                HasReturnValue ? typeof(object) : null,
                Expression, null);
        }
    }
}