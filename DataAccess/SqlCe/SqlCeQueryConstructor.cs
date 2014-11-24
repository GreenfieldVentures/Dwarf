using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dwarf.DataAccess;
using Dwarf.Interfaces;

namespace Dwarf
{
    internal class SqlCeQueryConstructor : IQueryConstructor
    {
        private const string distinct = "DISTINCT ";
        private const string top = "TOP {0}";
        private const string innerJoin = "INNER JOIN ";
        private const string leftOuterJoin = "LEFT OUTER JOIN ";
        private const string limit = " OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY";
        private const string date = "DATEADD(dd, 0, DATEDIFF(dd, 0, {0})) ";
        private const string datePart = "DATEPART({0}, {1})";

        public string Top(int i)
        {
            return string.Format(top, i);
        }

        public string Limit(int offset, int rows)
        {
            return string.Format(limit, offset, rows);
        }

        public string Distinct { get { return distinct; } }
        public string LeftContainer { get { return string.Empty; } }
        public string RightContainer { get { return string.Empty; } }
        public string TableNamePrefix { get { return string.Empty; } }
        public string InnerJoin { get { return innerJoin; } }
        public string LeftOuterJoin { get { return leftOuterJoin; } }

        public string Date(string columnName)
        {
            return string.Format(date, columnName);
        }

        public string DatePart(DateParts dp, string targetColumn)
        {
            return string.Format(datePart, dp.ToQuery(), targetColumn);
        }
    }
}
