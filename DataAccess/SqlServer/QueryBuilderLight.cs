using System;
using System.Text;
using Dwarf.Attributes;
using Dwarf.DataAccess;

namespace Dwarf
{
    /// <summary>
    /// A simplified version of the QueryBuilder, but for internal use only
    /// </summary>
    internal class QueryBuilderLight
    {
        #region Variables

        private Type type;
        private string select;
        private string from;
        private string orderBy;
        private readonly StringBuilder where;
        private readonly StringBuilder joins;

        #endregion Variables

        #region Constructors

        internal QueryBuilderLight()
        {
            where = new StringBuilder();
            joins = new StringBuilder();
        }

        #endregion Constructors

        #region Methods

        #region Select

        internal QueryBuilderLight Select<T>()
        {
            type = typeof (T);
            select = string.Format("SELECT [{0}].*", typeof(T).Name);

            foreach (var pi in Cfg.ProjectionProperties[type])
            {
                var script = DwarfProjectionPropertyAttribute.GetAttribute(pi.ContainedProperty).Script;
                select += ", " + "(" + script + ") AS [" + pi.Name + "]";
            }

            select += " ";

            return this;
        }

        #endregion Select

        #region From

        internal QueryBuilderLight From<T>()
        {
            from = string.Format("FROM dbo.[{0}] ", typeof(T).Name);
            
            return this;
        }

        #endregion From

        #region Where

        internal QueryBuilderLight Where(string column, object value, Type type)
        {
            where.Append(string.Format("{0} {1} = {2} ", (where.Length == 0 ? "WHERE " : "AND "), column, ContextAdapter.GetDatabase(type).ValueToSqlString(value)));

            return this;
        }

        internal QueryBuilderLight Where<T>(string column, object value)
        {
            where.Append(string.Format("{0} [{1}].[{2}] = {3} ", (where.Length == 0 ? "WHERE " : "AND "), typeof(T).Name, column, ContextAdapter<T>.GetDatabase().ValueToSqlString(value)));
            
            return this;
        }

        #endregion Where

        #region InnerJoin

        internal QueryBuilderLight InnerJoin(string join)
        {
            joins.Append("INNER JOIN " + join + " ");

            return this;
        }

        #endregion InnerJoin

        #region LeftOuterJoin

        internal QueryBuilderLight LeftOuterJoin(string join)
        {
            joins.Append("LEFT OUTER JOIN " + join + " ");

            return this;
        }

        #endregion LeftOuterJoin

        #region ToQuery

        internal string ToQuery(bool disableSort = false)
        {
            if (!disableSort)
            {
                var sort = Cfg.OrderBySql[type];

                if (!string.IsNullOrEmpty(sort))
                    orderBy = "ORDER BY " + sort;
            }
            return select + from + joins + where + orderBy;
        }

        #endregion ToQuery
        
        #endregion Methods
    }
}
