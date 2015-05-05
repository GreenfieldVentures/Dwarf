using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Evergreen.Dwarf.Attributes;
using Evergreen.Dwarf.Extensions;
using Evergreen.Dwarf.DataAccess;
using Evergreen.Dwarf.Interfaces;
using Evergreen.Dwarf.Proxy;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf
{
    /// <summary>
    /// Helper class for extracting metadata about the Dwarfs
    /// </summary>
    public static class DwarfHelper
    {
        #region Methods

        #region DeProxyfy

        /// <summary>
        /// Return the type or it's base type if it implements IProxy
        /// </summary>
        internal static Type DeProxyfy(ref Type type)
        {
            return (type = type.Implements<IProxy>() ? type.BaseType : type);
        }

        /// <summary>
        /// Return the type or it's base type if it implements IProxy
        /// </summary>
        internal static Type DeProxyfy(Type type)
        {
            return type.Implements<IProxy>() ? type.BaseType : type;
        }

        /// <summary>
        /// Return the type or it's base type if it implements IProxy
        /// </summary>
        internal static Type DeProxyfy(object obj)
        {
            return obj is IProxy ? obj.GetType().BaseType : obj.GetType();
        }

        #endregion DeProxyfy

        #region GetDBProperties

        /// <summary>
        /// Returns all properties of the supplied type that's
        /// represented as a table column
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetDBProperties(Type type)
        {
            DeProxyfy(ref type);

            return Cfg.DBProperties[type];
        }      
        
        /// <summary>
        /// Returns all properties of the supplied type that's
        /// represented as a table column
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetDBProperties(IDwarf obj)
        {
            return GetDBProperties(obj.GetType());
        }

        /// <summary>
        /// Returns all properties of the supplied type that's
        /// represented as a table column
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetDBProperties<T>()
        {

            return GetDBProperties(typeof(T));
        }

        #endregion GetDBProperties

        #region GetProjectionProperties

        /// <summary>
        /// Returns all properties of the supplied type that's
        /// a computated value, static value, expression, etc
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetProjectionProperties<T>(Type type)
        {
            foreach (var ep in PropertyHelper.GetProperties(type))
            {
                foreach (var attribute in ep.GetCustomAttributes(false))
                {
                    if (attribute is DwarfProjectionPropertyAttribute)
                        yield return ep;
                }
            }
        }

        /// <summary>
        /// Returns all properties of the supplied type that's
        /// represented as a table column
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetProjectionProperties<T>()
        {
            return GetProjectionProperties<T>(typeof(T));
        }

        #endregion GetProjectionProperties

        #region GetUniqueDBProperties

        /// <summary>
        /// Returns all properties of the supplied type that's
        /// represented as a unique table column
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetUniqueDBProperties<T>(Type type)
        {
            DeProxyfy(ref type);

            foreach (var ep in PropertyHelper.GetProperties(type))
            {
                if (ep.Name.Equals("Id"))
                    continue;

                foreach (var att in ep.GetCustomAttributes(false))
                {
                    if (att is DwarfPropertyAttribute && ((DwarfPropertyAttribute)att).IsUnique)
                        yield return ep;
                }
            }
        }

        #endregion GetUniqueDBProperties

        #region GetPKProperties

        /// <summary>
        /// Returns all Primary keys of the supplied type
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetPKProperties(Type type)
        {
            DeProxyfy(ref type);

            return Cfg.PKProperties[type];
        }

        /// <summary>
        /// Returns all Primary keys of the supplied type
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetPKProperties<T>()
        {
            return GetPKProperties(typeof (T));
        }

        #endregion GetPKProperties

        #region GetManyToManyProperties

        /// <summary>
        /// Returns all ManyToMany properties of the current object
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetManyToManyProperties(IDwarf obj)
        {
            var type = DeProxyfy(obj);
            return Cfg.ManyToManyProperties[type];
        }

        /// <summary>
        /// Returns all ManyToMany properties of the current object
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetManyToManyProperties(Type type)
        {
            DeProxyfy(ref type);
            return Cfg.ManyToManyProperties[type];
        }

        #endregion GetManyToManyProperties

        #region GetOneToManyProperties

        /// <summary>
        /// Returns all OneToMany properties of the current object
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetOneToManyProperties(IDwarf obj)
        {
            var type = DeProxyfy(obj);
            return Cfg.OneToManyProperties[type];
        }

        /// <summary>
        /// Returns all OneToMany properties of the current object
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetOneToManyProperties(Type type)
        {
            DeProxyfy(ref type);
            return Cfg.OneToManyProperties[type];
        }

        #endregion GetOneToManyProperties

        #region GetGemListProperties

        /// <summary>
        /// Returns all Collection properties of the current object
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetGemListProperties(IDwarf obj)
        {
            return GetGemListProperties(obj.GetType());
        }

        /// <summary>
        /// Returns all Collection properties of the current object
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetGemListProperties(Type type)
        {
            DeProxyfy(ref type);
            return Cfg.GemListProperties[type];
        }
        /// <summary
        /// >
        /// Returns all Collection properties of the current object
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetGemListProperties<T>()
        {
            return GetGemListProperties(typeof(T));
        }

        #endregion GetGemListProperties

        #region GetFKProperties

        /// <summary>
        /// Returns all Foreign keys of the current object
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetFKProperties<T>(Type type)
        {
            DeProxyfy(ref type);

            return Cfg.FKProperties[type];
        }

        #endregion GetFKProperties

        #region GetOrderByProperty

        /// <summary>
        /// Returns the first property with the DefaultSort property enabled
        /// </summary>
        internal static ExpressionProperty GetOrderByProperty<T>()
        {
            return GetOrderByProperty(typeof(T));
        }

        /// <summary>
        /// Returns the first property with the DefaultSort property enabled
        /// </summary>
        internal static ExpressionProperty GetOrderByProperty(Type type)
        {
            DeProxyfy(ref type);

            foreach (var ep in PropertyHelper.GetProperties(type))
            {
                var att = DwarfPropertyAttribute.GetAttribute(ep.ContainedProperty);

                if (att != null && att.IsDefaultSort)
                    return ep;
            }

            return null;
        }

        #endregion GetOrderByProperty        
        
        #region GetOrderByDirection

        internal static string GetOrderByDirection<T>()
        {
            return GetOrderByDirection(typeof (T));
        }

        internal static string GetOrderByDirection(Type type)
        {
            DeProxyfy(ref type);

            foreach (var pi in PropertyHelper.GetProperties(type))
            {
                var att = DwarfPropertyAttribute.GetAttribute(pi.ContainedProperty);

                if (att != null && att.IsDefaultSort)
                    return att.SortDirection == SortDirection.Ascending ? string.Empty : "DESC";
            }

            return string.Empty;
        }

        #endregion GetOrderByDirection

        #region GetUniqueGroupNames

        /// <summary>
        /// Returns a list of all UniqueGroup names within the supplied type
        /// </summary>
        internal static IEnumerable<string> GetUniqueGroupNames<T>(Type type)
        {
            DeProxyfy(ref type);

            return (from ep in PropertyHelper.GetProperties(type)
                    from attribute in ep.GetCustomAttributes(false)
                    where attribute is DwarfPropertyAttribute && !string.IsNullOrEmpty(((DwarfPropertyAttribute)attribute).UniqueGroupName)
                    select ((DwarfPropertyAttribute)attribute).UniqueGroupName).Distinct();
        }

        #endregion GetUniqueGroupNames

        #region GetUniqueGroupProperties

        /// <summary>
        /// Returns a list of all UniqueGroup names within the supplied type
        /// </summary>
        internal static IEnumerable<ExpressionProperty> GetUniqueGroupProperties<T>(Type type, string uniqueGroupName)
        {
            DeProxyfy(ref type);

            return from ep in PropertyHelper.GetProperties(type)
                   from attribute in ep.GetCustomAttributes(false)
                   where attribute is DwarfPropertyAttribute && uniqueGroupName.Equals(((DwarfPropertyAttribute)attribute).UniqueGroupName)
                   select ep;
        }

        #endregion GetUniqueGroupProperties

        #region GetUniqueKeyForCompositeId

        /// <summary>
        /// Returns a unique key for the supplied CompositeId IDwarf
        /// </summary>
        internal static string GetUniqueKeyForCompositeId<T>(T obj) where T : Dwarf<T>, new()
        {
            if (!typeof(T).Implements<ICompositeId>())
                throw new InvalidOperationException(typeof(T).Name + " does not implement ICompositeId");

            if (obj == null)
                return null;

            if (Cfg.PKProperties[typeof (T)].IsNullOrEmpty())
                return obj.GetHashCode().ToString();

            var objectKey = "CompositeId";

            foreach (var pi in Cfg.PKProperties[typeof(T)])
            {
                objectKey += ":";

                objectKey += pi.PropertyType.Implements<IDwarf>()
                                 ? obj.GetOriginalValue(pi.Name)
                                 : pi.GetValue(obj).ToString();
            }

            return objectKey;
        }
        
        /// <summary>
        /// Returns a unique key for the specified CompositeId Type based on the supplied conditions
        /// </summary>
        internal static string GetUniqueKeyForCompositeId<T>(WhereCondition<T>[] conditions) where T : IDwarf 
        {
            if (!typeof(T).Implements<ICompositeId>())
                throw new InvalidOperationException(typeof(T).Name + " does not implement ICompositeId");

            var objectKey = "CompositeId";

            foreach (var pi in conditions)
            {
                objectKey += ":";

                objectKey += pi.Value.GetType().Implements<IDwarf>()
                                 ? ((IDwarf)pi.Value).Id.ToString()
                                 : pi.Value.ToString();
            }

            return objectKey;
        }

        #endregion GetUniqueKeyForCompositeId

        #region Load

        /// <summary>
        /// Utilize the compiled Load expresseion (use instead of reflection wherever possible
        /// </summary>
        public static object Load(Type type, Guid id)
        {
            DeProxyfy(ref type);

            return Cfg.LoadExpressions[type](id);
        }

        #endregion Load

        #region CreateInstance

        /// <summary>
        /// Creates and returns a new instance of the specified type
        /// </summary>
        public static object CreateInstance(Type type)
        {
            type = DeProxyfy(type);

            if (!Cfg.ProxyTypeConstructors.ContainsKey(type))
                throw new InvalidOperationException("There's nothing proxified called " + type.Name);

            return Cfg.ProxyTypeConstructors[type].Invoke();
        }

        /// <summary>
        /// Creates and returns a new instance of the specified type
        /// </summary>
        public static T CreateInstance<T>()
        {
            return (T)CreateInstance(typeof (T));
        }

        #endregion CreateInstance

        #region GetValidTypes

        internal static IEnumerable<Type> GetValidTypes<T>()
        {
            var baseType = DeProxyfy(typeof(T));

            if (baseType != typeof(T))
                throw new InvalidOperationException("T is a proxy class... Looks like you need to specify the complete namespace");

            var typeList = baseType.Assembly.GetTypes().Where(type => type.Implements<IDwarf>() && !type.IsAbstract).ToList();

            if (DwarfContext<T>.GetConfiguration().AuditLogService is AuditLogService<T>)
                typeList.Add(typeof(AuditLog));

            if (DwarfContext<T>.GetConfiguration().ErrorLogService is ErrorLogService<T>)
                typeList.Add(typeof(ErrorLog));

            return typeList;
        }

        #endregion GetValidTypes

        public static IDwarfConfiguration GetConfiguration<T>()
        {
            return DwarfContext<T>.GetConfiguration();
        }

        #endregion Methods
    }
}