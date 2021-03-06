﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Evergreen.Dwarf.DataAccess;
using Evergreen.Dwarf.Interfaces;

namespace Evergreen.Dwarf
{
    internal class SqlServerQueryConstructor: IQueryConstructor
    {
        private const string distinct = "DISTINCT ";
        private const string top = "TOP {0}";
        private const string leftContainer = "[";
        private const string rightContainer = "]";
        private const string tableNamePrefix = "dbo.";
        private const string innerJoin = "INNER JOIN ";
        private const string leftOuterJoin = "LEFT OUTER JOIN ";
        private const string limit = " OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY";
        private const string offset = " OFFSET {0} ROWS";
        private const string date = "DATEADD(dd, 0, DATEDIFF(dd, 0, {0})) ";
        private const string datePart = "DATEPART({0}, {1})";

        public string Top(int i)
        {
            return string.Format(top, i);
        }

        public string Limit(int offsetRows, int rows)
        {
            return string.Format(limit, offsetRows, rows);
        }

        public string Offset(int offsetRows)
        {
            return string.Format(offset, offsetRows);
        }

        public string Distinct { get { return distinct; } }
        public string LeftContainer { get { return leftContainer; } }
        public string RightContainer { get { return rightContainer; } }
        public string TableNamePrefix { get { return tableNamePrefix; } }
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
