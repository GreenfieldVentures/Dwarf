using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarf.DataAccess
{

    /// <summary>
    /// Different operators available for where conditions
    /// </summary>
    public enum QueryOperators
    {
        /// <summary>
        /// Equals
        /// </summary>
        Equals,

        /// <summary>
        /// Not Equals
        /// </summary>
        NotEqualTo,

        /// <summary>
        /// Less Than
        /// </summary>
        LessThan,

        /// <summary>
        /// Greater Than
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Less Than Or Equal To
        /// </summary>
        LessThanOrEqualTo,

        /// <summary>
        /// Greater Than Or Equal To
        /// </summary>
        GreaterThanOrEqualTo,

        /// <summary>
        /// Like
        /// </summary>
        Like,

        /// <summary>
        /// Not Like
        /// </summary>
        NotLike,

        /// <summary>
        /// Is Not
        /// </summary>
        IsNot,

        /// <summary>
        /// Is 
        /// </summary>
        Is,

        /// <summary>
        /// In 
        /// </summary>
        In,

        /// <summary>
        /// Not In 
        /// </summary>
        NotIn,

        /// <summary>
        /// Opposite of "In"
        /// </summary>
        Contains,
    }
}
