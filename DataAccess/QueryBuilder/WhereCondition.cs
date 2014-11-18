using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Dwarf.Interfaces;
using Dwarf.Utilities;

namespace Dwarf.DataAccess
{
    /// <summary>
    /// Represents a where condition to an sql query
    /// </summary>
    public class WhereCondition<T> : IWhereCondition
    {
        #region Variables

        private PropertyInfo column;

        #endregion Variables

        #region Constrcutors

        /// <summary>
        /// Default Constrcutor
        /// </summary>
        public WhereCondition() {}

        /// <summary>
        /// Constrcutor with the most common parameters
        /// </summary>
        public WhereCondition(Expression<Func<T, object>> column, object value)
        {
            Column = column;
            Value = value;
        }

        /// <summary>
        /// Constrcutor with the most common parameters
        /// </summary>
        public WhereCondition(Expression<Func<T, object>> column, QueryOperators queryOperator, object value)
        {
            Column = column;
            Value = value;
            Operator = queryOperator;
        }

        #endregion Constrcutors

        #region Properties

        #region Value

        /// <summary>
        /// The value to match
        /// </summary>
        public object Value { get; set; }

        #endregion Value

        #region Column

        /// <summary>
        /// The column to be queried
        /// </summary>
        public Expression<Func<T, object>> Column
        {
            set { column = ReflectionHelper.GetPropertyInfo(value); }
        }

        #endregion Column        
        
        #region ColumnPi

        /// <summary>
        /// The column to be queried
        /// </summary>
        public PropertyInfo ColumnPi
        {
            set { column = value; }
        }

        #endregion ColumnPi

        #region Operator

        /// <summary>
        /// Gets or Sets which operator to use.
        /// Default i "="
        /// </summary>
        public QueryOperators? Operator { get; set; }

        #endregion Operator

        /// <summary>
        /// Gets or Sets if DateTime values should be compared with time
        /// </summary>
        public bool IsTimeIncluded { get; set; }

        #endregion Properties

        #region Methods

        #region GetColumn

        /// <summary>
        /// Returns the specified column as a PropertyInfo
        /// </summary>
        public PropertyInfo GetColumn()
        {
            //ToDo:C# bug? :(
            return PropertyHelper.GetProperty(typeof(T), column.Name).ContainedProperty;
//            return typeof(T).GetProperty(column.Name);
        }

        #endregion GetColumn

        #region ToQuery

        /// <summary>
        /// Converts the current WhereCondition to an SQL Query
        /// </summary>
        public string ToQuery()
        {
            return new QueryBuilder().WhereConditionToQuery(this);
        }

        /// <summary>
        /// Converts the current WhereCondition to an SQL Query
        /// </summary>
        public static string ToQuery(params IWhereCondition[] conditions)
        {
            return ToQuery(WhereSeparator.Or, conditions);
        }

        /// <summary>
        /// Converts the current WhereCondition to an SQL Query
        /// </summary>
        public static string ToQuery(WhereSeparator ws, params IWhereCondition[] conditions)
        {
            if (conditions.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append("(");

            for (var i = 0; i < conditions.Length; i++)
            {
                sb.Append(conditions[i].ToQuery());

                if (i < conditions.Length - 1)
                    sb.Append(" " + ws.ToQuery() + " ");
            }

            sb.Append(")");

            return sb.ToString();
        }

        #endregion ToQuery

        #endregion Methods
    }
}
