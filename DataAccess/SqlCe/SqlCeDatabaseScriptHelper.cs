using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using Dwarf.DataAccess;
using Dwarf.DataAccess.SqlCe;
using Dwarf.Extensions;
using Dwarf.Utilities;

namespace Dwarf.DataAccess
{
    /// <summary>
    /// Utility class for generating sql ce scripts from the current domain model
    /// </summary>
    internal static class SqlCeDatabaseScriptHelper
    {
        #region GetCreateScript

        /// <summary>
        /// Converts and returns the current domain model as create scripts (incl keys)
        /// </summary>
        internal static string GetCreateScript<T>(string connectionString = null, bool hideOptionalStuff = false)
        {
            return GetCreateScriptArray<T>().Flatten();
        }

        private static IEnumerable<string> GetCreateScriptArray<T>()
        {
            var scripts = SqlServerDatabaseScriptHelper.GetCreateScript<T>();
            var center = scripts.IndexOf("ALTER");

            var tables = scripts.Substring(0, center - 1).Split(new[] { "CREATE" }, StringSplitOptions.RemoveEmptyEntries);
            var alters = scripts.Substring(center, (scripts.Length - 1 - center)).Split(new[] { "ALTER" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < tables.Length; i++)
                tables[i] = SqlCeDatabase.PurifyScriptQuery("CREATE" + tables[i]);

            for (var i = 0; i < alters.Length; i++)
                alters[i] = SqlCeDatabase.PurifyScriptQuery("ALTER" + alters[i]);

            return tables.ToList().Union(alters).ToArray();
        }

        #endregion GetCreateScript

        #region ExecuteCreateScript

        /// <summary>
        /// Executes scripts for creating the database
        /// </summary>
        internal static void ExecuteCreateScript<T>()
        {
            foreach (var s in GetCreateScriptArray<T>())
                DwarfContext<T>.GetDatabase().ExecuteNonQuery<T>(s);
        }

        #endregion ExecuteCreateScript
    }
}
