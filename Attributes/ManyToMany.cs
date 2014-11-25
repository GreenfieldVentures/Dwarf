using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dwarf.Extensions;
using Dwarf.Interfaces;
using Dwarf.Utilities;

namespace Dwarf.Attributes
{
    /// <summary>
    /// Attribute for ManyToMany properies in Dwarf derived types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ManyToManyAttribute : Attribute, IUnvalidatable
    {
        #region Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ManyToManyAttribute()
        {

        }

        /// <summary>
        /// Constructor will all parameters
        /// </summary>
        public ManyToManyAttribute(string tableName)
        {
            TableName = tableName;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or Sets the name of the ManyTomany table
        /// </summary>
        public string TableName { get; set; }

        #endregion Properties

        #region Methods

        #region GetAttribute

        /// <summary>
        /// Returns the OneToMany attribute if exists
        /// </summary>
        public static ManyToManyAttribute GetAttribute(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(false).OfType<ManyToManyAttribute>().FirstOrDefault();
        }

        #endregion GetAttribute

        #region GetTableName

        /// <summary>
        /// Returns the ManyToMany table name for the supplied criteria
        /// </summary>
        public static string GetTableName(Type type, PropertyInfo pi)
        {
            var att = GetAttribute(pi);

            if (att == null)
                throw new NullReferenceException(pi.Name + " is missing the OneToMany attribute...");

            if (!string.IsNullOrEmpty(att.TableName))
                return att.TableName;

            return GetManyToManyTableName(type, pi.PropertyType.GetGenericArguments()[0]);
        }

        /// <summary>
        /// Returns the ManyToMany table name for the supplied criteria
        /// </summary>
        public static string GetTableName<T>(Expression<Func<T, object>> exp)
        {
            var pi = PropertyHelper.GetProperty(DwarfHelper.DeProxyfy(typeof(T)), ReflectionHelper.GetPropertyName(exp));

            return GetTableName(typeof(T), pi.ContainedProperty);
        }

        internal static string GetManyToManyTableName(Type type1, Type type2, string alternateTableName = null)
        {
            return alternateTableName ??
                new[] { type1, type2 }.OrderBy(x => x.Name).Flatten(x => "To" + x.Name).TruncateStart(2);
        }

        #endregion GetTableName

        #endregion Methods
    }
}
