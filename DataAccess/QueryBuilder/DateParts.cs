using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarf.DataAccess
{/// <summary>
    /// Different DatePart operators available for where conditions
    /// </summary>
    public enum DateParts
    {
        /// <summary>
        /// Year
        /// </summary>
        Year,

        /// <summary>
        /// Quarter
        /// </summary>
        Quarter,

        /// <summary>
        /// Month
        /// </summary>
        Month,

        /// <summary>
        /// Dayofyear
        /// </summary>
        Dayofyear,

        /// <summary>
        /// Day
        /// </summary>
        Day,

        /// <summary>
        /// Week
        /// </summary>
        Week,

        /// <summary>
        /// Iso Week
        /// </summary>
        IsoWeek,

        /// <summary>
        /// Weekday
        /// </summary>
        Weekday,

        /// <summary>
        /// Hour
        /// </summary>
        Hour,

        /// <summary>
        /// Minute
        /// </summary>
        Minute,

        /// <summary>
        /// Second
        /// </summary>
        Second,
    }
}
