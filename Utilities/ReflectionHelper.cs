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
        private static string GetMemberName(this LambdaExpression memberSelector)
        {
            Func<Expression, string> nameSelector = null;  //recursive func
            nameSelector = e => //or move the entire thing to a separate recursive method
            {
                switch (e.NodeType)
                {
                    case ExpressionType.Parameter:
                        return ((ParameterExpression)e).Name;
                    case ExpressionType.MemberAccess:
                        return ((MemberExpression)e).Member.Name;
                    case ExpressionType.Call:
                        return ((MethodCallExpression)e).Method.Name;
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        return nameSelector(((UnaryExpression)e).Operand);
                    case ExpressionType.Invoke:
                        return nameSelector(((InvocationExpression)e).Expression);
                    case ExpressionType.ArrayLength:
                        return "Length";
                    default:
                        throw new Exception("not a proper member selector");
                }
            };

            return nameSelector(memberSelector.Body);
        }

        #region GetPropertyName

        /// <summary>
        /// Extracts a PropertyInfo object from the supplied expression and returns the name
        /// </summary>
        public static string GetPropertyName<T>(Expression<Func<T, object>> expression)
        {
            return GetMemberName(expression);
        }       
        
        /// <summary>
        /// Extracts a PropertyInfo object from the supplied expression and returns the name
        /// </summary>
        public static string GetPropertyName<T, TY>(Expression<Func<T, TY>> expression)
        {
            return GetMemberName(expression);
        }
        
        #endregion GetPropertyName

        #region GetPropertyInfo

        /// <summary>
        /// Extracts a PropertyInfo object from the supplied expression and returns the name
        /// </summary>
        public static PropertyInfo GetPropertyInfo<T, TY>(Expression<Func<T, TY>> expression)
        {
            var name = GetMemberName(expression);

            return PropertyHelper.GetProperty<T>(name).ContainedProperty;
        }

        #endregion GetPropertyInfo
    }
}
