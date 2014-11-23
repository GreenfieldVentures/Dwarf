using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.UI.WebControls;
using Dwarf.Attributes;
using Dwarf.Extensions;
using Dwarf.Interfaces;
using Dwarf.Utilities;

namespace Dwarf.DataAccess
{
    internal class QueryBuilder<T>: QueryBuilder
    {
        public QueryBuilder()
        {
            origianlBaseType = baseType = typeof(T);
        }
    }

    /// <summary>
    /// Class for constructing SQL queries
    /// </summary>
    public class QueryBuilder
    {
        #region Variables

        private readonly List<string> selectColumns;
        protected Type baseType;
        protected Type origianlBaseType;
        private QueryTypes queryType;
        private string tableName;
        private bool isDistinctQuery;
        private readonly List<string> whereConditions;
        private readonly List<string> setConditions;
        private readonly List<string> orderByColumns;
        private readonly List<string> groupByColumns;
        private readonly List<JoinCondition> joinConditions;
        private readonly List<InsertIntoValue> insertIntoValues;
        private bool isOrderByDisabled;
        private int? limitOffset;
        private int? limitRows;
        private Type lastType;
        #region QueryTypes

        private enum QueryTypes
        {
            Select,
            Delete,
            Update,
            Insert,
        }

        #endregion QueryTypes

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public QueryBuilder()
        {
            selectColumns = new List<string>();
            setConditions = new List<string>();
            whereConditions = new List<string>();
            joinConditions = new List<JoinCondition>();
            insertIntoValues = new List<InsertIntoValue>();
            groupByColumns = new List<string>();
            orderByColumns = new List<string>();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Sort direction
        /// </summary>
        internal SortDirection? SortDirection { get; set; }

        #endregion properties

        #region Methods

        #region Select

        internal void SelectInternal(string columnName)
        {
            queryType = QueryTypes.Select;
            selectColumns.Add(columnName);
        }

        /// <summary>
        /// Used internally to override any previous select entries, thus making sure that 
        /// the only returned columns are they of the specified type
        /// </summary>
        internal QueryBuilder SelectOverride<T>() where T : IDwarf
        {
            return ClearSelect().Select<T>();
        }

        /// <summary>
        /// Removes all previously selected columns
        /// </summary>
        internal QueryBuilder ClearSelect()
        {
            selectColumns.Clear();
            return this;
        }

        private string BuildSelectClause()
        {
            var selectClause = new StringBuilder();
            selectClause.Append("SELECT ");
            
            selectClause.Append("\t\t" + (Top.HasValue ? ("TOP " + Top.Value) : "") + (isDistinctQuery ? "DISTINCT " : ""));

            var setCounter = 0;

            for (int i = 0; i < selectColumns.Count; i++)
            {
                var separator = (i < selectColumns.Count - 1) ? ", " : " ";

                if (setCounter < 6)
                    selectClause.Append(selectColumns[i] + separator);
                else
                {
                    selectClause.Append("\r\n\t\t\t" + selectColumns[i] + separator);
                    setCounter = 0;
                }

                setCounter++;
            }

            return selectClause.ToString();
        }

        #endregion Select

        #region Top

        /// <summary>
        /// Top
        /// </summary>
        internal int? Top { get; set; }

        #endregion Top

        #region DistinctInternal

        /// <summary>
        /// Adds the DISTINCT keyword to the selection query
        /// </summary>
        internal void DistinctInternal()
        {
            isDistinctQuery = true;
        }

        #endregion DistinctInternal

        #region From

        internal void FromInternal(string table)
        {
            tableName = table;
        }

        internal void FromInternal(Type type)
        {
            baseType = type;
            FromInternal("dbo.[" + type.Name + "]");
        }

        private string BuildFromClause()
        {
            return "\r\n\r\nFROM \t\t" + tableName;
        }

        #endregion From

        #region InsertInto

        internal void InsertIntoInternal(string table)
        {
            queryType = QueryTypes.Insert;
            tableName = table;
        }

        internal void InsertIntoInternal(Type type)
        {
            InsertIntoInternal("dbo.[" + type.Name + "]");
        }

        private string BuildInsertIntoClause()
        {
            return "INSERT INTO " + tableName + "\r\n";
        }

        #endregion InsertInto

        #region Update

        internal void UpdateInternal(string table)
        {
            queryType = QueryTypes.Update;
            tableName = table;
        }

        internal void UpdateInternal(Type type)
        {
            UpdateInternal("dbo.[" + type.Name + "]");
        }

        private string BuildUpdateClause()
        {
            return "UPDATE \t\t" + tableName + "\r\n";
        }

//        internal QueryBuilder SetOwner<T>()
//        {
//            baseType = typeof (T);
//            return this;
//        }

        #endregion Update

        #region SetValue

        internal void SetInternal(string setCondition)
        {
            setConditions.Add(setCondition);
        }

        private string BuildSetClause()
        {
            if (setConditions.Count == 0)
                return string.Empty;

            var setClause = new StringBuilder();
            setClause.AppendLine();

            var first = true;

            for (int i = 0; i < setConditions.Count; i++)
            {
                var setCondition = setConditions[i];

                if (first)
                {
                    setClause.Append("SET");
                    first = false;
                }
                else
                    setClause.Append("\r\n");

                setClause.Append("\t\t\t" + setCondition + (i + 1 < setConditions.Count ? ", " : string.Empty));
            }

            return setClause.ToString();
        }

        #endregion SetValue

        #region Delete

        internal void DeleteFromInternal(string table)
        {
            queryType = QueryTypes.Delete;
            tableName = table;
        }

        internal void DeleteFromInternal(Type type)
        {
            baseType = type;
            DeleteFromInternal("dbo.[" + type.Name + "]");
        }

        private string BuildDeleteClause()
        {
            return "DELETE FROM " + tableName;
        }

        #endregion Delete

        #region Join

        internal void JoinInternal(JoinCondition joinCondition)
        {
            joinConditions.Add(joinCondition);
        }

        private string BuildJoinClause()
        {
            if (joinConditions.Count == 0)
                return string.Empty;

            var joinClause = new StringBuilder();
            joinClause.AppendLine();

            for (var i = 0; i < joinConditions.Count; i++)
            {
                if (i > 0)
                    joinClause.AppendLine();

                var jc = joinConditions[i];

                joinClause.Append(jc.IsLeftOuterJoin ? "LEFT OUTER JOIN " : "INNER JOIN \t");

                joinClause.Append(string.Format("dbo.{0} ON {0}.{1} = {2}.{3}", jc.LeftTypeName, jc.LeftColumnName, jc.RightTypeName, jc.RightColumnName));
            }

            return joinClause.ToString();
        }

        #endregion Join

        #region Where

        internal void WhereInternal(string condition)
        {
            whereConditions.Add(condition);
        }

        internal void WhereWithInnerOrClauseInternal(params string[] conditions)
        {
            var condition = string.Empty;

            for (int i = 0; i < conditions.Length; i++)
            {
                if (i == 0)
                    condition += "(";
                else
                {
                    if (!string.IsNullOrEmpty(conditions[i]))
                        condition += " OR ";
                }

                condition += conditions[i];
            }

            if (conditions.Length > 0)
                WhereInternal(condition + ")");
        }

        private string BuildWhereClause()
        {
            if (whereConditions.Count == 0)
                return string.Empty;

            var whereClause = new StringBuilder();
            whereClause.AppendLine();

            var first = true;

            foreach (var whereCondition in whereConditions)
            {
                if (first)
                {
                    whereClause.Append("\r\nWHERE \t\t" + whereCondition);
                    first = false;
                }
                else
                    whereClause.Append("\r\nAND \t\t" + whereCondition);
            }

            return whereClause.ToString();
        }

        #endregion Where

        #region Values

        internal void ValuesInternal(InsertIntoValue value)
        {
            insertIntoValues.Add(value);
        }

        private string BuildValuesClause()
        {
            var columnClause = new StringBuilder();
            var valuesClause = new StringBuilder();

            columnClause.Append("\t\t\t(");
            valuesClause.Append("VALUES\t\t(");
            
            var counter = 0;

            for (int i = 0; i < insertIntoValues.Count; i++)
            {
                var insertIntoValue = insertIntoValues[i];

                var separator = (i < insertIntoValues.Count - 1) ? ", " : ") ";

                if (counter < 6)
                {
                    columnClause.Append(ConvertToQueryColumn(insertIntoValue.Type, insertIntoValue.GetColumnName()) + separator);
                    valuesClause.Append(DwarfContext.GetDatabase(baseType).ValueToSqlString(insertIntoValue.Value) + separator);
                }
                else
                {
                    columnClause.Append("\r\n\t\t\t" + ConvertToQueryColumn(insertIntoValue.Type, insertIntoValue.GetColumnName()) + separator);
                    valuesClause.Append("\r\n\t\t\t" + DwarfContext.GetDatabase(baseType).ValueToSqlString(insertIntoValue.Value) + separator);
                    counter = 0;
                }

                counter++;
            }

            return new StringBuilder()
                .AppendLine(columnClause.ToString())
                .AppendLine()
                .Append(valuesClause.ToString())
            .ToString();
        }

        #endregion Values

        #region OrderBy

        /// <summary>
        /// Used internally to determine if the query has a manually set order by clause
        /// </summary>
        internal bool HasOrderByClause()
        {
            return orderByColumns.Any();
        }

        /// <summary>
        /// Used internally to override any previous order by entries when they can not be used
        /// </summary>
        public QueryBuilder DisableOrderBy()
        {
            isOrderByDisabled = true;
            return this;
        }

        internal void OrderByInternal(string column)
        {
            orderByColumns.Add(column);
        }

        internal void Limit(int offset, int rows)
        {
            limitOffset = offset;
            limitRows = rows;
        }

        private string BuildOrderByClause()
        {
            if (isOrderByDisabled)
                return string.Empty;

            var sort = string.Empty;

            if (orderByColumns.Count > 0)
            {
                var orderByClause = new StringBuilder();

                var orderByCounter = 0;

                for (int i = 0; i < orderByColumns.Count; i++)
                {
                    var separator = (i < orderByColumns.Count - 1) ? ", " : " ";

                    if (orderByCounter < 6)
                        orderByClause.Append(orderByColumns[i] + separator);
                    else
                    {
                        orderByClause.Append("\r\n\t" + orderByColumns[i] + separator);
                        orderByCounter = 0;
                    }

                    orderByCounter++;
                }

                sort = orderByClause + (SortDirection.HasValue && SortDirection.Value == System.Web.UI.WebControls.SortDirection.Descending ? " DESC" : string.Empty);
            }

            if (string.IsNullOrEmpty(sort))
            {
                if (baseType == null)
                    return string.Empty;

                sort = Cfg.OrderBySql[baseType];
            }

            var limit = string.Empty;

            if (limitOffset.HasValue && !Top.HasValue)
            {
                if (string.IsNullOrEmpty(sort))
                    sort = "[Id]";

                limit = " OFFSET " + limitOffset.Value + " ROWS FETCH NEXT " + limitRows.Value + " ROWS ONLY";
            }

            return string.IsNullOrEmpty(sort) ? sort : ("\r\n\r\n" + string.Format("ORDER BY \t" + sort) + limit);
        }

        #endregion OrderBy

        #region GroupBy

        internal void GroupByInternal(string column)
        {
            groupByColumns.Add(column);
        }

        private string BuildGroupByClause()
        {
            if (groupByColumns.Count == 0)
                return string.Empty;

            var groupByClause = new StringBuilder();
            groupByClause.AppendLine();
            groupByClause.Append("GROUP BY \t");

            var groupByCounter = 0;

            for (int i = 0; i < groupByColumns.Count; i++)
            {
                var separator = (i < groupByColumns.Count - 1) ? ", " : " ";

                if (groupByCounter < 6)
                    groupByClause.Append(groupByColumns[i] + separator);
                else
                {
                    groupByClause.Append("\r\n\t\t\t" + groupByColumns[i] + separator);
                    groupByCounter = 0;
                }

                groupByCounter++;
            }

            return "\r\n" + groupByClause;
        }

        #endregion GroupBy

        #region ToQuery

        /// <summary>
        /// Converts the QueryBuilder to a query string
        /// </summary>
        public string ToQuery()
        {
//            baseType <- switcha på denna?
            switch (queryType)
            {
                case QueryTypes.Update:
                    return new StringBuilder()
                        .Append(BuildUpdateClause())
                        .Append(BuildSetClause())
                        .Append(BuildWhereClause())
                        .ToString();
                case QueryTypes.Delete:
                    return new StringBuilder()
                        .Append(BuildDeleteClause())
                        .Append(BuildJoinClause())
                        .Append(BuildWhereClause())
                        .ToString();
                case QueryTypes.Select:
                    return new StringBuilder()
                        .Append(BuildSelectClause())
                        .Append(BuildFromClause())
                        .Append(BuildJoinClause())
                        .Append(BuildWhereClause())
                        .Append(BuildGroupByClause())
                        .Append(BuildOrderByClause())
                        .ToString();
                case QueryTypes.Insert:
                    return new StringBuilder()
                        .Append(BuildInsertIntoClause())
                        .Append(BuildValuesClause())
                        .ToString();
            }

            return string.Empty;
        }

        #endregion ToQuery

        #region ConvertToQueryCondition

        /// <summary>
        /// Converts the supplied object and property info to an sql compliant string
        /// </summary>
        internal string ConvertToQueryCondition(Type type, string columnName, string value)
        {
            return string.Format("[{0}].[{1}] = {2}", type.Name, columnName, value);
        }

        /// <summary>
        /// Converts the supplied object and property info to an sql compliant string
        /// </summary>
        internal string ConvertToQueryCondition(object obj, PropertyInfo pi)
        {
            return ConvertToQueryCondition(obj.GetType(), GetColumnName(pi), DwarfContext.GetDatabase(baseType).ValueToSqlString(pi.GetValue(obj, null)));
        }

        /// <summary>
        /// Converts the supplied object and property info to an sql compliant string
        /// </summary>
        internal string ConvertToQueryCondition(object obj, ExpressionProperty fp)
        {
            return ConvertToQueryCondition(obj.GetType(), GetColumnName(fp.ContainedProperty), DwarfContext.GetDatabase(baseType).ValueToSqlString(fp.GetValue(obj)));
        }

        /// <summary>
        /// Converts the supplied object and property info to an sql compliant string
        /// </summary>
        internal string ConvertToQueryCondition<T>(Expression<Func<T, object>> expression, object value)
        {
            return ConvertToQueryCondition(typeof(T), ReflectionHelper.GetPropertyInfo(expression).Name, DwarfContext<T>.GetDatabase().ValueToSqlString(value));
        }

        #endregion ConvertToQueryCondition

        #region ConvertToQueryColumn

        /// <summary>
        /// Converts the supplied object and property info to an sql compliant string
        /// </summary>
        internal static string ConvertToQueryColumn(Type type, PropertyInfo pi)
        {
            return string.Format("[{0}].[{1}]", type.Name, GetColumnName(pi));
        }

        /// <summary>
        /// Converts the supplied object and property info to an sql compliant string
        /// </summary>
        internal static string ConvertToQueryColumn(Type type, ExpressionProperty fp)
        {
            return string.Format("[{0}].[{1}]", type.Name, GetColumnName(fp.ContainedProperty));
        }

        /// <summary>
        /// Converts the supplied object and property info to an sql compliant string
        /// </summary>
        internal static string ConvertToQueryColumn(Type type, string columnName)
        {
            if (type == null)
                return "[" + columnName + "]";

            return string.Format("[{0}].[{1}]", type.Name, columnName);
        }

        /// <summary>
        /// Converts the supplied object and expression to an sql compliant string
        /// </summary>
        public static string ConvertToQueryColumn<T>(Expression<Func<T, object>> expression)
        {
            return ConvertToQueryColumn(typeof (T), ReflectionHelper.GetPropertyInfo(expression));
        }

        #endregion ConvertToQueryColumn

        #region WhereConditionToQuery

        /// <summary>
        /// Converts the current WhereCondition to an SQL Query
        /// </summary>
        internal string WhereConditionToQuery<T>(WhereCondition<T> condition, DateParts? datePart = null)
        {
            var bt = origianlBaseType ?? baseType;
            
            if (condition.Operator == null)
                condition.Operator = condition.Value != null ? QueryOperators.Equals : QueryOperators.Is;

            var op = " " + condition.Operator.Value.ToQuery() + " ";

            string conditionValue;

            if (condition.Operator == QueryOperators.Contains)
            {
                condition.Operator = QueryOperators.Like;

                if (condition.Value is IGem)
                    condition.Value = "¶" + ((IGem)condition.Value).Id + "¶";

                return WhereConditionToQuery(condition, datePart);
            }

            if (condition.Operator == QueryOperators.Like)
            {
                conditionValue = DwarfContext.GetDatabase(bt).ValueToSqlString("%" + condition.Value + "%");
            }
            else if (condition.Operator == QueryOperators.In || condition.Operator == QueryOperators.NotIn)
            {
                if (condition.Value is string)
                {
                    if (condition.Value != null && !string.IsNullOrEmpty(condition.Value.ToString()))
                        conditionValue = "(" + condition.Value + ")";
                    else
                        throw new InvalidOperationException("If the condition is \"IN\" or \"NOT IN\" value may not be null or empty");
                }
                else if (condition.Value is IEnumerable)
                {
                    var values = condition.Value as IEnumerable;

                    var aggItems = values.Cast<object>().Aggregate(string.Empty, (current, value) => current + (DwarfContext.GetDatabase(bt).ValueToSqlString(value) + ", "));

                    conditionValue = "(" + (aggItems.Length == 0 ? "''" : aggItems.TruncateEnd(2)) + ")";
                }
                else if (condition.Value is QueryBuilder)
                    conditionValue = "(" + ((QueryBuilder)condition.Value).ToQuery().RemoveAll("\r\n").RemoveAll("\t") + ")";
                else
                    throw new InvalidOperationException("If the condition is \"IN\" or \"NOT IN\" then the value must be an IEnumerable, QueryBuilder or a custom condition as a string");
            }
            else
            {
                conditionValue = DwarfContext.GetDatabase(bt).ValueToSqlString(condition.Value);
            }

            if (!condition.IsTimeIncluded && condition.Value is DateTime)
            {
                conditionValue = DwarfContext.GetDatabase(bt).ValueToSqlString(((DateTime)condition.Value).Date);

                return "DATEADD(dd, 0, DATEDIFF(dd, 0, " + GetColumnName<T>(condition.GetColumn()) + ")) " + op + conditionValue + " ";
            }

            var column = GetColumnName<T>(condition.GetColumn());

            if (datePart.HasValue)
                column = datePart.Value.ToQuery(column);

            return column + op + conditionValue;
        }

        #endregion WhereConditionToQuery

        #region GetColumnName

        /// <summary>
        /// Returns the proper column name for the supplied property info
        /// </summary>
        internal static string GetColumnName<T>(PropertyInfo pi)
        {
            if (DwarfPropertyAttribute.GetAttribute(pi) != null)
                return string.Format("[{0}].[{1}]", typeof(T).Name, pi.Name + ((DwarfPropertyAttribute.IsFK(pi)) ? "Id" : string.Empty));

            if (DwarfProjectionPropertyAttribute.GetAttribute(pi) != null)
                return "(" + DwarfProjectionPropertyAttribute.GetAttribute(pi).Script + ")";

            if (pi.PropertyType.Implements<IGemList>())
                return string.Format("[{0}].[{1}]", typeof(T).Name, pi.Name);

            throw new InvalidOperationException("A queriable property must reside in the database too, right?");
        }

        internal static string GetColumnName(PropertyInfo pi)
        {
            if (DwarfPropertyAttribute.GetAttribute(pi) != null)
                return pi.Name + ((DwarfPropertyAttribute.IsFK(pi)) ? "Id" : string.Empty);

            if (DwarfProjectionPropertyAttribute.GetAttribute(pi) != null)
                return "(" + DwarfProjectionPropertyAttribute.GetAttribute(pi).Script + ")";

            if (pi.PropertyType.Implements<IGemList>())
                return pi.Name;

            throw new InvalidOperationException("A queriable property must reside in the database too, right?");
        }

        #endregion GetColumnName

        #endregion Methods

        internal Type GetLastType()
        {
            return (lastType ?? baseType) ?? origianlBaseType;
        }
        
        internal void SetLastType(Type type)
        {
            lastType = type;
        }
    }

    #region QueryBuilderExtensions

    /// <summary>
    /// Extensions for the QueryBuilder
    /// </summary>
    public static class QueryBuilderExtensions
    {
        #region Select

        /// <summary>
        /// Adds a column to the select clause
        /// </summary>
        public static QueryBuilder Select(this QueryBuilder qb, string columnName)
        {
            qb.SelectInternal(columnName);
            return qb;
        }

        /// <summary>
        /// Adds a column to the select clause
        /// </summary>
        public static QueryBuilder Select<T>(this QueryBuilder qb, PropertyInfo propertyInfo)
        {
            qb.SelectInternal(QueryBuilder.GetColumnName<T>(propertyInfo));
            return qb;
        }

        /// <summary>
        /// Adds columns to the select clause
        /// </summary>
        public static QueryBuilder Select(this QueryBuilder qb, params string[] columnNames)
        {
            foreach (var columnName in columnNames)
                qb.SelectInternal(columnName);

            return qb;
        }

        /// <summary>
        /// Adds columns of the supplied type to the select clause 
        /// </summary>
        public static QueryBuilder Select(this QueryBuilder qb, Type type)
        {
            qb.SelectInternal("[" + type.Name + "].*");

            foreach (var pi in Cfg.ProjectionProperties[type])
            {
                var script = DwarfProjectionPropertyAttribute.GetAttribute(pi.ContainedProperty).Script;
                qb.SelectInternal("(" + script + ") AS [" + pi.Name + "]");
            }

            return qb;
        }

        /// <summary>
        /// Adds columns of the supplied type to the select clause 
        /// </summary>
        public static QueryBuilder Select<T>(this QueryBuilder qb) where T: IDwarf
        {
            qb.Select(typeof (T));
            return qb;
        }

        /// <summary>
        /// Adds columns of the supplied type to the select clause 
        /// </summary>
        public static QueryBuilder Select<T>(this QueryBuilder qb, params Expression<Func<T, object>>[] columns)
        {
            foreach (var column in columns)
                qb.Select<T>(ReflectionHelper.GetPropertyInfo(column));

            return qb;
        }

        /// <summary>
        /// Adds columns of the supplied type to the select clause 
        /// </summary>
        public static QueryBuilder Select<T, TY>(this QueryBuilder qb, params Expression<Func<T, TY>>[] columns)
        {
            foreach (var column in columns)
                qb.Select<T>(ReflectionHelper.GetPropertyInfo(column));

            return qb;
        }

        /// <summary>
        /// Adds an operation to the select clause
        /// </summary>
        public static QueryBuilder Select<T>(this QueryBuilder qb, SelectOperators selectOperator, QuerySeparatorOperatons querySeparatorOperaton, params Expression<Func<T, object>>[] expressions)
        {
            if (expressions.Length == 0)
                return qb;

            var clause = selectOperator.ToQuery() + "(";

            clause = expressions.Aggregate(clause, (current, expression) => current + (QueryBuilder.ConvertToQueryColumn(expression) + " " + querySeparatorOperaton.ToQuery() + " "));
            clause = clause.TruncateEnd(2);

            qb.SelectInternal(clause.Trim() + ")");

            return qb;
        } 

        /// <summary>
        /// Adds an operation to the select clause
        /// </summary>
        public static QueryBuilder Select<T>(this QueryBuilder qb, SelectOperators selectOperator, QuerySeparatorOperatons querySeparatorOperaton, params string[] columns)
        {
            if (columns.Length == 0)
                return qb;

            var clause = selectOperator.ToQuery() + "(";

            clause = columns.Aggregate(clause, (current, column) => current + (column + " " + querySeparatorOperaton.ToQuery() + " "));
            clause = clause.TruncateEnd(2);

            qb.SelectInternal(clause.Trim() + ")");

            return qb;
        } 

        /// <summary>
        /// Adds an operation to the select clause
        /// </summary>
        public static QueryBuilder Select(this QueryBuilder qb, SelectOperators selectOperator, QuerySeparatorOperatons querySeparatorOperaton, params string[] columns)
        {
            if (columns.Length == 0)
                return qb;

            var clause = selectOperator.ToQuery() + "(";

            clause = columns.Aggregate(clause, (current, column) => current + (column + " " + querySeparatorOperaton.ToQuery() + " "));
            clause = clause.TruncateEnd(2);

            qb.SelectInternal(clause.Trim() + ")");

            return qb;
        }       
        
        /// <summary>
        /// Adds a datepart operation to the select clause
        /// </summary>
        public static QueryBuilder Select<T>(this QueryBuilder qb, DateParts datePart, Expression<Func<T, object>> expression)
        {
            qb.SelectInternal(datePart.ToQuery(QueryBuilder.ConvertToQueryColumn(expression)));

            return qb;
        }
    
        #endregion Select

        #region Top

        /// <summary>
        /// Limits the query result to the supplied number of rows
        /// </summary>
        public static QueryBuilder Top(this QueryBuilder qb, int rows)
        {
            qb.Top = rows;

            return qb;
        }

        #endregion Top        
        
        #region Limit

        /// <summary>
        /// Limits the query result to the supplied range
        /// ONLY WORKS IN SQL 2012 AND ABOVE!!!
        /// </summary>
        public static QueryBuilder Limit(this QueryBuilder qb, int offset, int rows)
        {
            qb.Limit(offset, rows);
            return qb;
        }

        #endregion Limit

        #region Distinct

        /// <summary>
        /// Adds the DISTINCT keyword to the selection query
        /// </summary>
        public static QueryBuilder Distinct(this QueryBuilder qb)
        {
            qb.DistinctInternal();
            return qb;
        }

        #endregion Distinct

        #region From

        /// <summary>
        /// Adds a table name to the from clause
        /// </summary>
        public static QueryBuilder From(this QueryBuilder qb, string tableName)
        {
            qb.FromInternal(tableName);
            return qb;
        }

        /// <summary>
        /// Adds a table name to the from clause
        /// </summary>
        public static QueryBuilder From(this QueryBuilder qb, Type type)
        {
            qb.FromInternal(type);
            return qb;
        }

        /// <summary>
        /// Adds a table name to the from clause
        /// </summary>
        public static QueryBuilder From<T>(this QueryBuilder qb)
        {
            qb.From(typeof (T));
            return qb;
        }

        #endregion From

        #region InsertInto

        /// <summary>
        /// Adds a table name to the Insert Into clause
        /// </summary>
        public static QueryBuilder InsertInto(this QueryBuilder qb, string tableName)
        {
            qb.InsertIntoInternal(tableName);
            return qb;
        }

        /// <summary>
        /// Adds a table name to the Insert Into clause
        /// </summary>
        public static QueryBuilder InsertInto(this QueryBuilder qb, Type type)
        {
            qb.InsertIntoInternal(type);
            return qb;
        }

        /// <summary>
        /// Adds a table name to the Insert Into clause
        /// </summary>
        public static QueryBuilder InsertInto<T>(this QueryBuilder qb)
        {
            qb.InsertInto(typeof (T));
            return qb;
        }

        #endregion InsertInto

        #region Values

        /// <summary>
        /// Adds and object where its properties and current values will be added to the insert clause
        /// </summary>
        public static QueryBuilder Values<T>(this QueryBuilder qb, Dwarf<T> obj) where T: Dwarf<T>, new()
        {
            foreach (var pi in DwarfHelper.GetDBProperties(obj))
                qb.ValuesInternal(new InsertIntoValue { ColumnProperty = pi, Type = obj.GetType(), Value = pi.GetValue(obj) });

            foreach (var pi in DwarfHelper.GetGemListProperties(obj))
                qb.ValuesInternal(new InsertIntoValue { ColumnProperty = pi, Type = obj.GetType(), Value = pi.GetValue(obj) });

            return qb;
        }        
        
        /// <summary>
        /// Adds and object where its properties and current values will be added to the insert clause
        /// </summary>
        public static QueryBuilder Values(this QueryBuilder qb, string column, object value)
        {
            qb.ValuesInternal(new InsertIntoValue { ColumnName = column, Value = value});

            return qb;
        }     


        /// <summary>
        /// Adds and object where its properties and current values will be added to the insert clause
        /// </summary>
        public static QueryBuilder Values<T>(this QueryBuilder qb, Expression<Func<T, object>> column, object value) where T : Dwarf<T>, new()
        {
            var pi = ReflectionHelper.GetPropertyInfo(column);
            qb.ValuesInternal(new InsertIntoValue { ColumnName = QueryBuilder.ConvertToQueryColumn(typeof(T), pi), Value = value});

            return qb;
        }

        #endregion Values

        #region Update

        /// <summary>
        /// Adds a table name to the update clause
        /// </summary>
        public static QueryBuilder Update(this QueryBuilder qb, string tableName)
        {
            qb.UpdateInternal(tableName);
            return qb;
        }

        /// <summary>
        /// Adds a table name to the update clause
        /// </summary>
        public static QueryBuilder Update(this QueryBuilder qb, Type type)
        {
            qb.UpdateInternal(type);
            return qb;
        }

        /// <summary>
        /// Adds a table name to the update clause
        /// </summary>
        public static QueryBuilder Update<T>(this QueryBuilder qb)
        {
            qb.Update(typeof(T));
            return qb;
        }

        #endregion Update

        #region Delete

        /// <summary>
        /// Adds a table name to the delete clause
        /// </summary>
        public static QueryBuilder DeleteFrom(this QueryBuilder qb, string tableName)
        {
            qb.DeleteFromInternal(tableName);
            return qb;
        }

        /// <summary>
        /// Adds a table name to the delete clause
        /// </summary>
        public static QueryBuilder DeleteFrom(this QueryBuilder qb, Type type)
        {
            qb.DeleteFromInternal(type);
            return qb;
        }

        /// <summary>
        /// Adds a table name to the delete clause
        /// </summary>
        public static QueryBuilder DeleteFrom<T>(this QueryBuilder qb)
        {
            qb.DeleteFrom(typeof(T));
            return qb;
        }

        #endregion Delete

        #region SetValue

        /// <summary>
        /// Adds a condition to the set clause
        /// </summary>
        public static QueryBuilder Set(this QueryBuilder qb, string condition)
        {
            qb.SetInternal(condition);
            return qb;
        }

        /// <summary>
        /// Adds conditions to the set clause
        /// </summary>
        public static QueryBuilder Set<T>(this QueryBuilder qb, Dwarf<T> obj) where T : Dwarf<T>, new()
        {
            foreach (var pi in DwarfHelper.GetDBProperties(obj))
            {
                if (!obj.GetType().Implements<ICompositeId>() && pi.Name.Equals("Id"))
                    continue;

                qb.Set(qb.ConvertToQueryCondition(obj, pi));
            }

            foreach (var pi in DwarfHelper.GetGemListProperties(obj))
            {
                if (!obj.GetType().Implements<ICompositeId>() && pi.Name.Equals("Id"))
                    continue;

                qb.Set(qb.ConvertToQueryCondition(obj, pi));
            }

            return qb;
        }

        /// <summary>
        /// Adds conditions to the set clause
        /// </summary>
        public static QueryBuilder Set<T>(this QueryBuilder qb, Dwarf<T> obj, IEnumerable<ExpressionProperty> properties) where T : Dwarf<T>, new()
        {
            foreach (var pi in properties)
            {
                if (!obj.GetType().Implements<ICompositeId>() && pi.Name.Equals("Id"))
                    continue;

                qb.Set(qb.ConvertToQueryCondition(obj, pi));
            }            
            
            return qb;
        }

        /// <summary>
        /// Adds conditions to the set clause
        /// </summary>
        public static QueryBuilder Set<T>(this QueryBuilder qb, Expression<Func<T, object>> expression, object value)
        {
            qb.Set(qb.ConvertToQueryCondition(expression, value));

            return qb;
        }

        #endregion SetValue

        #region Join

        /// <summary>
        /// Performs an Inner Join between the two specified types. Lets the QueryBuilder best guess the relationship between the types.
        /// </summary>
        public static QueryBuilder InnerJoin<T>(this QueryBuilder qb)
        {
            var orgType = qb.GetLastType();
            
            var o2mRight = Cfg.OneToManyProperties[orgType].FirstOrDefault(x => x.ContainedProperty.PropertyType.GetGenericArguments()[0] == typeof(T));

            if (o2mRight != null)
            {
                qb.JoinInternal(new JoinCondition
                {
                    LeftType = typeof (T),
                    LeftColumnName = "[" + orgType.Name + "Id]",
                    RightColumnName = "[Id]",
                    RightType = orgType,
                });

                qb.SetLastType(typeof(T));
                return qb;
            }

            var o2mLeft = Cfg.OneToManyProperties[typeof(T)].FirstOrDefault(x => x.ContainedProperty.PropertyType.GetGenericArguments()[0] == orgType);

            if (o2mLeft != null)
            {
                qb.JoinInternal(new JoinCondition
                {
                    LeftType = typeof (T),
                    LeftColumnName = "[Id]",
                    RightColumnName = "[" + typeof(T).Name + "Id]",
                    RightType = orgType,
                });
               
                qb.SetLastType(typeof(T));
                return qb;
            }

            string tableName = string.Empty;
            var m2m = Cfg.ManyToManyProperties[orgType].FirstOrDefault(x => x.ContainedProperty.PropertyType.GetGenericArguments()[0] == typeof(T));

            if (m2m != null)
            {
                tableName = ManyToManyAttribute.GetTableName(orgType, m2m.ContainedProperty);
            }
            else
            {
                m2m = Cfg.ManyToManyProperties[typeof(T)].FirstOrDefault(x => x.ContainedProperty.PropertyType.GetGenericArguments()[0] == orgType);

                if (m2m != null)
                    tableName = ManyToManyAttribute.GetTableName(typeof(T), m2m.ContainedProperty);
            }

            if (m2m != null)
            { 
                qb.JoinInternal(new JoinCondition
                {
                    LeftTypeName = "[" + tableName + "]",
                    LeftColumnName = "[" + orgType.Name + "Id]",
                    RightType = orgType,
                    RightColumnName = "[Id]",
                });                
                
                qb.JoinInternal(new JoinCondition
                {
                    LeftType = typeof(T),
                    LeftColumnName = "[Id]",
                    RightTypeName = "[" + tableName + "]",
                    RightColumnName = "[" + typeof(T).Name + "Id]",
                });

                qb.SetLastType(typeof(T));
                return qb;
            }

            throw new InvalidOperationException("A relationship between " + typeof(T).Name + " and " + orgType.Name + " can't be derived. Please specify the columns...");
        }

        /// <summary>
        /// Performs an Inner Join between the two specified types on the specified columns
        /// </summary>
        public static QueryBuilder InnerJoin<T, TY>(this QueryBuilder qb)
        {
            qb.SetLastType(typeof(TY));
            return InnerJoin<T>(qb);
        }

        public static QueryBuilder InnerJoin<T, TY>(this QueryBuilder qb, Expression<Func<T, object>> leftCondition, Expression<Func<TY, object>> rightCondition)
        {
            qb.JoinInternal(new JoinCondition
            {
                LeftType = typeof (T),
                LeftColumn = leftCondition.Body.NodeType == ExpressionType.Parameter
                        ? PropertyHelper.GetProperty(typeof(T), "Id").ContainedProperty
                        : ReflectionHelper.GetPropertyInfo(leftCondition),
                RightType = typeof (TY),
                RightColumn = rightCondition.Body.NodeType == ExpressionType.Parameter
                        ? PropertyHelper.GetProperty(typeof(TY), "Id").ContainedProperty 
                        : ReflectionHelper.GetPropertyInfo(rightCondition),
                IsLeftOuterJoin = false
            });
            qb.SetLastType(typeof(T));
            return qb;
        }        

        /// <summary>
        /// Performs an Left Outer Join between the two specified types on the specified columns
        /// </summary>
        public static QueryBuilder LeftOuterJoin<T, TY>(this QueryBuilder qb, Expression<Func<T, object>> leftCondition, Expression<Func<TY, object>> rightCondition)
        {
            qb.JoinInternal(new JoinCondition
            {
                LeftType = typeof(T),
                LeftColumn = leftCondition.Body.NodeType == ExpressionType.Parameter
                        ? PropertyHelper.GetProperty(typeof(T), "Id").ContainedProperty
                        : ReflectionHelper.GetPropertyInfo(leftCondition),
                RightType = typeof(TY),
                RightColumn = rightCondition.Body.NodeType == ExpressionType.Parameter
                        ? PropertyHelper.GetProperty(typeof(T), "Id").ContainedProperty
                        : ReflectionHelper.GetPropertyInfo(rightCondition),
                IsLeftOuterJoin = true
            });
            qb.SetLastType(typeof(T));
            return qb;
        }

        /// <summary>
        /// Performs an Inner Join between the two specified types on the specified columns
        /// </summary>
        public static QueryBuilder LeftOuterJoin<T, TY>(this QueryBuilder qb)
        {
            qb.SetLastType(typeof(TY));
            return LeftOuterJoin<T>(qb);
        }

        /// <summary>
        /// Performs a Left Outer between the two specified types. Lets the QueryBuilder best guess the relationship between the types.
        /// </summary>
        public static QueryBuilder LeftOuterJoin<T>(this QueryBuilder qb)
        {
            var orgType = qb.GetLastType(); 

            var o2mRight = Cfg.OneToManyProperties[orgType].FirstOrDefault(x => x.ContainedProperty.PropertyType.GetGenericArguments()[0] == typeof (T));

            if (o2mRight != null)
            {
                qb.JoinInternal(new JoinCondition
                {
                    LeftType = typeof (T),
                    LeftColumnName = "[" + orgType.Name + "Id]",
                    RightColumnName = "[Id]",
                    RightType = orgType,
                    IsLeftOuterJoin = true,
                });

                qb.SetLastType(typeof (T));
                return qb;
            }

            var o2mLeft =
                Cfg.OneToManyProperties[typeof (T)].FirstOrDefault(x => x.ContainedProperty.PropertyType.GetGenericArguments()[0] == orgType);

            if (o2mLeft != null)
            {
                qb.JoinInternal(new JoinCondition
                {
                    LeftType = typeof (T),
                    LeftColumnName = "[Id]",
                    RightColumnName = "[" + typeof (T).Name + "Id]",
                    RightType = orgType,
                    IsLeftOuterJoin = true,
                });

                qb.SetLastType(typeof (T));
                return qb;
            }

            throw new InvalidOperationException("A \"left outer join\"-able relationship between " + typeof(T).Name + " and " + orgType.Name + " can't be derived. Please specify the columns...");
        }

        #endregion Join

        #region Where

        /// <summary>
        /// Adds conditions to the where clause
        /// </summary>
        public static QueryBuilder Where<T>(this QueryBuilder qb, Expression<Func<T, object>> expression, object value)
        {
            return qb.Where(new WhereCondition<T>(expression, value));
        }

        /// <summary>
        /// Adds conditions to the where clause
        /// </summary>
        public static QueryBuilder Where<T>(this QueryBuilder qb, Expression<Func<T, object>> expression, QueryOperators queryOperator, object value)
        {
            return qb.Where(new WhereCondition<T>(expression, queryOperator, value));
        }       
        
        /// <summary>
        /// Adds conditions to the where clause
        /// </summary>
        public static QueryBuilder Where<T>(this QueryBuilder qb, DateParts datePart, Expression<Func<T, object>> expression, object value)
        {
            return qb.Where(datePart, new WhereCondition<T>(expression, value));
        }

        /// <summary>
        /// Adds conditions to the where clause
        /// </summary>
        public static QueryBuilder Where<T>(this QueryBuilder qb, DateParts datePart, Expression<Func<T, object>> expression, QueryOperators queryOperator, object value)
        {
            return qb.Where(datePart, new WhereCondition<T>(expression, queryOperator, value));
        }

        /// <summary>
        /// Adds conditions to the where clause
        /// </summary>
        public static QueryBuilder Where<T>(this QueryBuilder qb, params WhereCondition<T>[] conditions)
        {
            foreach (var whereCondition in conditions)
                qb.WhereInternal(qb.WhereConditionToQuery(whereCondition));

            return qb;
        }
        
        /// <summary>
        /// Adds conditions to the where clause
        /// </summary>
        public static QueryBuilder Where(this QueryBuilder qb, params string[] conditions)
        {
            foreach (var whereCondition in conditions)
                qb.WhereInternal(whereCondition);

            return qb;
        }        

        /// <summary>
        /// Adds conditions to the where clause
        /// </summary>
        public static QueryBuilder Where(this QueryBuilder qb, Type type, ExpressionProperty property, object value)
        {
            qb.Where(qb.ConvertToQueryCondition(type, property.Name, value.ToString()));
            return qb;
        }

        /// <summary>
        /// Adds conditions to the where clause
        /// </summary>
        public static QueryBuilder Where(this QueryBuilder qb, object obj, params PropertyInfo[] propertyInfos)
        {
            foreach (var pi in propertyInfos)
                qb.Where(qb.ConvertToQueryCondition(obj, pi));

            return qb;
        }

        /// <summary>
        /// Adds conditions to the where clause
        /// </summary>
        internal static QueryBuilder Where(this QueryBuilder qb, object obj, params ExpressionProperty[] fastProperties)
        {
            foreach (var fp in fastProperties)
                qb.Where(qb.ConvertToQueryCondition(obj, fp));

            return qb;
        }

        /// <summary>
        /// Adds conditions to the where clause
        /// </summary>
        public static QueryBuilder Where(this QueryBuilder qb, object obj, IEnumerable<PropertyInfo> propertyInfos)
        {
            qb.Where(obj, propertyInfos.ToArray());

            return qb;
        }

        /// <summary>
        /// Adds conditions to the where clause
        /// </summary>
        internal static QueryBuilder Where(this QueryBuilder qb, object obj, IEnumerable<ExpressionProperty> fastProperties)
        {
            qb.Where(obj, fastProperties.ToArray());

            return qb;
        }

        /// <summary>
        /// Adds a condition to the where clause with the supplied conditions wrapped in a paranthasis with an OR separator
        /// </summary>
        public static QueryBuilder WhereWithInnerOrClause(this QueryBuilder qb, params string[] conditions)
        {
            qb.WhereWithInnerOrClauseInternal(conditions);

            return qb;
        }
        
        /// <summary>
        /// Adds a condition to the where clause with the supplied conditions wrapped in a paranthasis with an OR separator
        /// </summary>
        public static QueryBuilder WhereWithInnerOrClause<T>(this QueryBuilder qb, params WhereCondition<T>[] conditions)
        {
            qb.WhereWithInnerOrClauseInternal(conditions.Select(x => qb.WhereConditionToQuery(x)).ToArray());

            return qb;
        }

        /// <summary>
        /// Adds columns to the order by clause
        /// </summary>
        public static QueryBuilder Where<T>(this QueryBuilder qb, DateParts datePart, WhereCondition<T> condition)
        {
            qb.Where(qb.WhereConditionToQuery(condition, datePart));

            return qb;
        }

        #endregion Where

        #region OrderBy

        /// <summary>
        /// Adds columns to the order by clause
        /// </summary>
        public static QueryBuilder OrderBy(this QueryBuilder qb, params string[] columnNames)
        {
            foreach (var columnName in columnNames)
                qb.OrderByInternal(columnName);

            return qb;
        }

        /// <summary>
        /// Adds columns to the order by clause
        /// </summary>
        public static QueryBuilder OrderBy<T>(this QueryBuilder qb, params Expression<Func<T, object>>[] columns)
        {
            qb.OrderBy<T>(columns.Select(ReflectionHelper.GetPropertyInfo).ToArray());
            return qb;
        }

        /// <summary>
        /// Adds columns to the order by clause
        /// </summary>
        public static QueryBuilder OrderBy<T>(this QueryBuilder qb, params PropertyInfo[] propertyInfos)
        {
            foreach (var pi in propertyInfos)
                qb.OrderByInternal(QueryBuilder.ConvertToQueryColumn(typeof(T), pi));

            return qb;
        }        
        
        /// <summary>
        /// Adds columns to the order by clause
        /// </summary>
        public static QueryBuilder OrderBy<T>(this QueryBuilder qb, IEnumerable<PropertyInfo> propertyInfos)
        {
            qb.OrderBy<T>(propertyInfos.ToArray());
            return qb;
        }        
        
        /// <summary>
        /// Adds columns to the order by clause
        /// </summary>
        public static QueryBuilder OrderBy<T>(this QueryBuilder qb, DateParts dateParts, Expression<Func<T, object>> expression)
        {
            qb.OrderByInternal(dateParts.ToQuery(QueryBuilder.ConvertToQueryColumn(expression)));
            return qb;
        }

        #endregion OrderBy

        #region Descendingly

        /// <summary>
        /// Instructs the order by clause to sort descendingly
        /// </summary>
        public static QueryBuilder Descendingly(this QueryBuilder qb)
        {
            qb.SortDirection = SortDirection.Descending;

            return qb;
        }

        #endregion Descendingly

        #region GroupBy

        /// <summary>
        /// Adds columns to the group by clause
        /// </summary>
        public static QueryBuilder GroupBy(this QueryBuilder qb, params string[] columnNames)
        {
            foreach (var columnName in columnNames)
                qb.GroupByInternal(columnName);

            return qb;
        }

        /// <summary>
        /// Adds columns to the group by clause
        /// </summary>
        public static QueryBuilder GroupBy<T>(this QueryBuilder qb, params Expression<Func<T, object>>[] columns)
        {
            qb.GroupBy<T>(columns.Select(ReflectionHelper.GetPropertyInfo).ToArray());
            return qb;
        }

        /// <summary>
        /// Adds columns to the group by clause
        /// </summary>
        public static QueryBuilder GroupBy<T>(this QueryBuilder qb, params PropertyInfo[] propertyInfos)
        {
            foreach (var pi in propertyInfos)
                qb.GroupBy(QueryBuilder.GetColumnName<T>(pi));

            return qb;
        }

        /// <summary>
        /// Adds columns to the group by clause
        /// </summary>
        public static QueryBuilder GroupBy<T>(this QueryBuilder qb, IEnumerable<PropertyInfo> propertyInfos)
        {
            qb.GroupBy<T>(propertyInfos.ToArray());
            return qb;
        }

        /// <summary>
        /// Adds all columns of the type to the group by clause
        /// </summary>
        public static QueryBuilder GroupBy<T>(this QueryBuilder qb)
        {
            qb.GroupBy(typeof (T));

            return qb;
        }

        /// <summary>
        /// Adds all columns of the type to the group by clause
        /// </summary>
        public static QueryBuilder GroupBy(this QueryBuilder qb, Type type)
        {
            foreach (var pi in DwarfHelper.GetDBProperties(type).OrderBy(p => p.Name))
                qb.GroupByInternal(QueryBuilder.ConvertToQueryColumn(type, pi));

            return qb;
        }

        #endregion GroupBy
    }

    #endregion QueryBuilderExtensions

    #region JoinCondition

    /// <summary>
    /// Helper class from join conditions
    /// </summary>
    internal class JoinCondition
    {
        public string LeftTypeName { get; set; }
        public Type LeftType
        {
            set { LeftTypeName = "[" + value.Name + "]"; }
        }

        public string LeftColumnName { get; set; } 
        public PropertyInfo LeftColumn
        {
            set { LeftColumnName = "[" + QueryBuilder.GetColumnName(value) + "]"; }
        }        
        
        public string RightTypeName { get; set; }
        public Type RightType
        {
            set { RightTypeName = "[" + value.Name + "]"; }
        }

        public string RightColumnName { get; set; } 
        public PropertyInfo RightColumn
        {
            set { RightColumnName = "[" + QueryBuilder.GetColumnName(value) + "]"; }
        }

        public bool IsLeftOuterJoin { get; set; }
    }

    #endregion JoinCondition

    #region InsertIntoValue

    internal class InsertIntoValue
    {
        public Type Type { get; set; }

        public ExpressionProperty ColumnProperty { private get; set; }
        
        public string ColumnName { private get; set; }

        public object Value { get; set; }

        public string GetColumnName()
        {
            if (ColumnProperty != null)
                return QueryBuilder.GetColumnName(ColumnProperty.ContainedProperty);

            return ColumnName;
        }
    }

    #endregion InsertIntoValue
}
