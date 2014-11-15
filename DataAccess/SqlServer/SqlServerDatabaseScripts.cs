using System;
using Dwarf.Interfaces;
using Dwarf.Utilities;

namespace Dwarf.DataAccess
{
    public class SqlServerDatabaseScripts<T> : IDatabaseScripts
    { 
        public string GetCreateScript()
        {
            if (!ContextAdapter<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            return SqlServerDatabaseScriptHelper.GetCreateScript<T>();
        }

        public string GetDropScript()
        {
            if (!ContextAdapter<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            return SqlServerDatabaseScriptHelper.GetDropScript<T>();
        }

        public string GetUpdateScript()
        {
            if (!ContextAdapter<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            return SqlServerDatabaseScriptHelper.GetUpdateScript<T>();
        }        
        
        public string GetTransferScript(string otherDatabaseName, params ExpressionProperty[] toSkip)
        {
            if (!ContextAdapter<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            return SqlServerDatabaseScriptHelper.GetTransferScript<T>(otherDatabaseName, toSkip);
        }        
        
        public void ExecuteCreateScript()
        {
            if (!ContextAdapter<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            SqlServerDatabaseScriptHelper.ExecuteCreateScript<T>();
        }

        public void ExecuteDropScript()
        {
            if (!ContextAdapter<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            SqlServerDatabaseScriptHelper.ExecuteDropScript<T>();
        }
    }
}