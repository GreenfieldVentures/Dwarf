using System;
using Evergreen.Dwarf.Interfaces;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf.DataAccess
{
    public class SqlServerDatabaseScripts<T> : IDatabaseScripts
    { 
        public string GetCreateScript()
        {
            if (!DwarfContext<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            return SqlServerDatabaseScriptHelper.GetCreateScript<T>();
        }

        public string GetDropScript()
        {
            if (!DwarfContext<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            return SqlServerDatabaseScriptHelper.GetDropScript<T>();
        }

        public string GetUpdateScript()
        {
            if (!DwarfContext<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            return SqlServerDatabaseScriptHelper.GetUpdateScript<T>();
        }        
        
        public string GetTransferScript(string otherDatabaseName, params ExpressionProperty[] toSkip)
        {
            if (!DwarfContext<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            return SqlServerDatabaseScriptHelper.GetTransferScript<T>(otherDatabaseName, toSkip);
        }        
        
        public void ExecuteCreateScript()
        {
            if (!DwarfContext<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            SqlServerDatabaseScriptHelper.ExecuteCreateScript<T>();
        }

        public void ExecuteDropScript()
        {
            if (!DwarfContext<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            SqlServerDatabaseScriptHelper.ExecuteDropScript<T>();
        }
    }
}