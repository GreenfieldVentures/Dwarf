using System;
using System.Linq;
using System.Reflection;

namespace Evergreen.Dwarf.Attributes
{
    /// <summary>
    /// Attribute for properies in Dwarf derived types that 
    /// will not be stored as a table column, but instead server as a 
    /// read-only property propagated via the supplied subquery
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DwarfProjectionPropertyAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public DwarfProjectionPropertyAttribute(string script)
        {
            Script = script;
        }

        #endregion Constructors

        #region Properties

        #region Script

        /// <summary>
        /// Gets or Sets the sql script to use for projection
        /// </summary>
        public string Script { get; set; }

        #endregion Script

        #endregion Properties

        #region Methods

        #region GetAttribute

        /// <summary>
        /// Returns the DwarfPropertyAttribute attribute exists
        /// </summary>
        public static DwarfProjectionPropertyAttribute GetAttribute(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(false).OfType<DwarfProjectionPropertyAttribute>().FirstOrDefault();
        }

        #endregion GetAttribute

        #endregion Methods
    }
}
