using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Dwarf.Interfaces;

namespace Dwarf.Attributes
{
    /// <summary>
    /// Attribute for OneToMany properies in Dwarf derived types.
    /// The attribute is optional for collections that isn't inverse
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OneToManyAttribute : Attribute, IUnvalidatable
    {
        #region Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        public OneToManyAttribute()
        {
        }

        /// <summary>
        /// Constructor will all parameters
        /// </summary>
        public OneToManyAttribute(bool isInverse)
        {
            IsInverse = isInverse;
        }

        #endregion Constructors

        #region Properties

        #region IsInverse

        /// <summary>
        /// Gets or Sets the owner side of the collection (i.e. should the collection be 
        /// deleted upon deletion or its reference set to null and saved)
        /// </summary>
        public bool IsInverse { get; set; }

        #endregion IsInverse

        #endregion Properties

        #region Methods

        #region GetAttribute

        /// <summary>
        /// Returns the OneToMany attribute exists
        /// </summary>
        public static OneToManyAttribute GetAttribute(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(false).OfType<OneToManyAttribute>().FirstOrDefault();
        }

        #endregion GetAttribute

        #endregion Methods
    }
}
