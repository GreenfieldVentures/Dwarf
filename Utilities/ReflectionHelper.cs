using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Evergreen.Dwarf.Utilities
{
    /// <summary>
    /// Helper class for .Net Reflection
    /// </summary>
    public static class ReflectionHelper
    {
        #region GetPropertyInfo

        /// <summary>
        /// Extracts a PropertyInfo object from the supplied expression and returns the name
        /// </summary>
        public static PropertyInfo GetPropertyInfo<T>(Expression<Func<T, object>> expression)
        {
            return ExtractPropertyInfo(expression);
        }        
        
        /// <summary>
        /// Extracts a PropertyInfo object from the supplied expression and returns the name
        /// </summary>
        public static PropertyInfo GetPropertyInfo<T, TY>(Expression<Func<T, TY>> expression)
        {
            return ExtractPropertyInfo(expression);
        }

        /// <summary>
        /// Extracts a PropertyInfo object from the supplied expression
        /// </summary>
        private static PropertyInfo ExtractPropertyInfo(Expression lambdaExpression)
        {
            Expression expressionToCheck = lambdaExpression;

            var done = false;

            while (!done)
            {
                switch (expressionToCheck.NodeType)
                {
                    case ExpressionType.Convert:
                        expressionToCheck = ((UnaryExpression)expressionToCheck).Operand;
                        break;
                    case ExpressionType.Lambda:
                        expressionToCheck = ((LambdaExpression)expressionToCheck).Body;
                        break;
                    case ExpressionType.MemberAccess:
                        var memberExpression = ((MemberExpression)expressionToCheck);

                        if (memberExpression.Expression.NodeType != ExpressionType.Parameter && memberExpression.Expression.NodeType != ExpressionType.Convert)
                        {
                            throw new ArgumentException(string.Format("Expression '{0}' must resolve to top-level member.", lambdaExpression), "lambdaExpression");
                        }

                        var member = memberExpression.Member as PropertyInfo;
                        return member;
                    default:
                        done = true;
                        break;
                }
            }

            return null;
        }

        #endregion GetPropertyInfo

        #region GetPropertyName

        /// <summary>
        /// Extracts a PropertyInfo object from the supplied expression and returns the name
        /// </summary>
        public static string GetPropertyName<T>(Expression<Func<T, object>> expression)
        {
            return ExtractPropertyName(expression);
        }       
        
        /// <summary>
        /// Extracts a PropertyInfo object from the supplied expression and returns the name
        /// </summary>
        public static string GetPropertyName<T, TY>(Expression<Func<T, TY>> expression)
        {
            return ExtractPropertyName(expression);
        }

        /// <summary>
        /// Extracts a PropertyInfo object from the supplied expression and returns the name
        /// </summary>
        private static string ExtractPropertyName(Expression lambdaExpression)
        {
            var pi = ExtractPropertyInfo(lambdaExpression);

            return pi != null ? pi.Name : string.Empty;
        }

        #endregion GetPropertyName
    }
}
