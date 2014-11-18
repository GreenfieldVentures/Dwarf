using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Web.UI.WebControls;
using Dwarf.Extensions;
using Dwarf.Interfaces;
using Dwarf.Utilities;

namespace Dwarf.Attributes
{
    /// <summary>
    /// Attribute for properies in Dwarf derived types
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DwarfPropertyAttribute : Attribute
    {
        #region Properties

        #region IsPrimaryKey

        /// <summary>
        /// True if the property is PK for the object/table
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        #endregion IsPrimaryKey

        #region IsNullable

        /// <summary>
        /// True if the property can be null in the database
        /// </summary>
        public bool IsNullable { get; set; }

        #endregion IsNullable

        #region UseMaxLength

        /// <summary>
        /// Gets or Sets if a varchar should be decleared as varchar(max) instead of varchar(255) 
        /// </summary>
        public bool UseMaxLength { get; set; }

        #endregion UseMaxLength

        #region IsUnique

        /// <summary>
        /// Gets or Sets if the column is unique
        /// </summary>
        public bool IsUnique { get; set; }

        #endregion IsUnique

        #region UniqueGroupName

        /// <summary>
        /// Gets or Sets if the name of the unique cluster this column is a member of
        /// </summary>
        public string UniqueGroupName { get; set; }

        #endregion UniqueGroupName

        #region DisableDeleteCascade

        /// <summary>
        /// Gets or Sets if the delete cascades should be disabled
        /// </summary>
        public bool DisableDeleteCascade { get; set; }

        #endregion DisableDeleteCascade

        #region IsDefaultSort

        /// <summary>
        /// Gets or Sets if the column will be used by default for 
        /// sorting queries of the current type
        /// </summary>
        public bool IsDefaultSort { get; set; }

        #endregion IsDefaultSort

        #region SortDirection

        /// <summary>
        /// Gets or Sets how the IsDefaultSort column will be sorted
        /// </summary>
        public SortDirection SortDirection { get; set; }

        #endregion SortDirection

        #region EagerLoad

        /// <summary>
        /// Gets or Sets if the foriegn key property should be loaded eagerly
        /// </summary>
        public bool EagerLoad { get; set; }

        #endregion EagerLoad

        #endregion Properties

        #region Methods

        #region GetAttribute

        /// <summary>
        /// Returns the DwarfPropertyAttribute attribute exists
        /// </summary>
        public static DwarfPropertyAttribute GetAttribute(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(false).OfType<DwarfPropertyAttribute>().FirstOrDefault();
        }

        /// <summary>
        /// Returns the DwarfPropertyAttribute attribute exists
        /// </summary>
        public static DwarfPropertyAttribute GetAttribute(ExpressionProperty propertyInfo)
        {
            return GetAttribute(propertyInfo.ContainedProperty);
        }

        #endregion GetAttribute

        #region IsFK

        /// <summary>
        /// Returns true if the supplied PropertyInfo references an type that implements IDwarf, ergo a Foreign Key
        /// </summary>
        public static bool IsFK(PropertyInfo pi)
        {
            return pi.PropertyType.Implements<IDwarf>();
        }

        /// <summary>
        /// Returns true if the supplied PropertyInfo references an type that implements IDwarf, ergo a Foreign Key
        /// </summary>
        public static bool IsFK(ExpressionProperty ep)
        {
            return ep.ContainedProperty.PropertyType.Implements<IDwarf>();
        }

        #endregion IsFK

        #endregion Methods
    }
}
