namespace Dwarf.DataAccess
{
    /// <summary>
    /// Helpers class for converting query enumerator to their SQL representative
    /// </summary>
    public static class QueryStringHelper
    {
        /// <summary>
        /// Converts a SelectOperator to a string
        /// </summary>
        public static string ToQuery(this SelectOperators so)
        {
            switch (so)
            {
                case SelectOperators.Avg:
                    return "AVG";
                case SelectOperators.Count:
                    return "COUNT";
                case SelectOperators.Sum:
                    return "SUM";                
                case SelectOperators.Min:
                    return "MIN";                
                case SelectOperators.Max:
                    return "MAX";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Converts a SelectOperator to a string
        /// </summary>
        public static string ToQuery(this QuerySeparatorOperatons qso)
        {
            switch (qso)
            {
                case QuerySeparatorOperatons.Addition:
                    return "+";
                case QuerySeparatorOperatons.Division:
                    return "/";
                case QuerySeparatorOperatons.Multiplication:
                    return "*";
                case QuerySeparatorOperatons.Subtraction:
                    return "-";
                case QuerySeparatorOperatons.None:
                    return string.Empty;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Converts a QueryOperator to a string
        /// </summary>
        public static string ToQuery(this QueryOperators qe)
        {
            switch (qe)
            {
                case QueryOperators.Equals:
                    return "=";
                case QueryOperators.NotEqualTo:
                    return "<>";
                case QueryOperators.GreaterThan:
                    return ">";
                case QueryOperators.GreaterThanOrEqualTo:
                    return ">=";
                case QueryOperators.LessThan:
                    return "<";
                case QueryOperators.LessThanOrEqualTo:
                    return "<=";
                case QueryOperators.Like:
                    return "LIKE";
                case QueryOperators.NotLike:
                    return "NOT LIKE";
                case QueryOperators.IsNot:
                    return "IS NOT";
                case QueryOperators.Is:
                    return "IS";
                case QueryOperators.In:
                    return "IN";                
                case QueryOperators.NotIn:
                    return "NOT IN";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Converts a QueryOperator to a string
        /// </summary>
        public static string ToQuery(this DateParts dp)
        {
            switch (dp)
            {
                case DateParts.Year:
                    return "yy";
                case DateParts.Quarter:
                    return "qq";
                case DateParts.Dayofyear:
                    return "dy";
                case DateParts.Week:
                    return "wk";
                case DateParts.IsoWeek:
                    return "isoww";
                case DateParts.Month:
                    return "mm";
                case DateParts.Day:
                    return "dd";
                case DateParts.Weekday:
                    return "dw";
                case DateParts.Hour:
                    return "hh";
                case DateParts.Minute:
                    return "mi";
                case DateParts.Second:
                    return "ss";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Converts a QueryMerger to a string
        /// </summary>
        public static string ToQuery(this QueryMergers qm)
        {
            switch (qm)
            {
                case QueryMergers.Union:
                    return "UNION";
                case QueryMergers.Intersect:
                    return "INTERSECT";
                case QueryMergers.Except:
                    return "EXCEPT";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Converts a WhereSeparator to a string
        /// </summary>
        public static string ToQuery(this WhereSeparator ws)
        {
            switch (ws)
            {
                case WhereSeparator.And:
                    return "AND";
                case WhereSeparator.Or:
                    return "OR";
                default:
                    return string.Empty;
            }
        }
    }
}
