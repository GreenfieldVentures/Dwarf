using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Dwarf.Extensions;
using Dwarf.Interfaces;

namespace Dwarf
{
    /// <summary>
    /// Helper for the genereic DwarfContext
    /// </summary>
    internal abstract class DwarfContext
    {
        #region Variables

        protected static readonly Dictionary<Assembly, IDwarfConfiguration> configs = new Dictionary<Assembly, IDwarfConfiguration>();

        #endregion Variables

        #region Properties

        #region Items

        [ThreadStatic]
        private static Dictionary<string, object> dic;

        /// <summary>
        /// See HttpContext.Current.Items 
        /// If there is not HttpContext a thread static dictionary will be used
        /// </summary>
        internal static IDictionary Items
        {
            get
            {
                if (HttpContext.Current != null)
                    return HttpContext.Current.Items;

                return dic ?? (dic = new Dictionary<string, object>());
            }
        }

        #endregion Items

        #endregion Properties
        
        #region Methods
        
        #region Configurations

        /// <summary>
        /// Returns a list of all configurations
        /// </summary>
        internal protected static List<IDwarfConfiguration> Configurations()
        {
            return configs.Values.ToList();
        }

        #endregion Configurations

        #region DisposeContexts

        internal static void DisposeContexts()
        {
            if (Items["DbContexts"] != null)
                (Items["DbContexts"] as IDictionary<Assembly, IDbContext>).ForEachX(x => x.Value.Dispose());

            if (dic != null)
            {
                dic.Clear();
                dic = null;
            }
        }

        #endregion DisposeContexts

        #region GetDatabase

        internal static IDatabase GetDatabase(Type type)
        {
            return Cfg.Databases[type.Assembly];
        }

        #endregion GetDatabase

        #endregion Methods
    }

    /// <summary>
    /// Helper class for internal communication between the domain model and the data access layer
    /// </summary>
    internal sealed class DwarfContext<T>: DwarfContext
    {
        #region Methods

        #region AddConfig

        /// <summary>
        /// Adds a configuration
        /// </summary>
        internal static void AddConfig(IDwarfConfiguration config)
        {
            var assembly = typeof (T).Assembly;

            if (!configs.ContainsKey(assembly))
                configs[assembly] = config;
        }

        #endregion AddConfig

        #region IsConfigured

        /// <summary>
        /// Returns true if the type's assembly is congigured
        /// </summary>
        internal static bool IsConfigured()
        {
            return configs.ContainsKey(typeof(T).Assembly);
        }

        #endregion IsConfigured        
        
        #region GetConfiguration

        public static IDwarfConfiguration GetConfiguration()
        {
            return configs.ContainsKey(typeof(T).Assembly) ? configs[typeof(T).Assembly] : null;
        }

        #endregion GetConfiguration

        #region GetDatabase

        internal static IDatabase GetDatabase()
        {
            return GetDatabase(typeof (T));
        }

        #endregion GetDatabase

        #region GetDBContext

        internal static IDbContext GetDBContext()
        {
            var contexts = (Items["DbContexts"] ?? (Items["DbContexts"] = new Dictionary<Assembly, IDbContext>())) as Dictionary<Assembly, IDbContext>;

            var assembly = typeof (T).Assembly;

            if (!contexts.ContainsKey(assembly))
                contexts[assembly] = new DbContext();

            return contexts[assembly];
        }

        #endregion GetDBContext

        #endregion Methods
    }
}
