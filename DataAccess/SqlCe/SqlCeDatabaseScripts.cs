using System;
using Evergreen.Dwarf.Interfaces;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf.DataAccess
{
    public class SqlCeDatabaseScripts<T> : IDatabaseScripts
    { 
        public string GetCreateScript()
        {
            if (!DwarfContext<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            return SqlCeDatabaseScriptHelper.GetCreateScript<T>();
        }

        public string GetDropScript()
        {
            throw new NotImplementedException();
        }

        public string GetUpdateScript()
        {
            throw new NotImplementedException();
        }        
        
        public string GetTransferScript(string otherDatabaseName, params ExpressionProperty[] toSkip)
        {
            throw new NotImplementedException();
        }        
        
        public void ExecuteCreateScript()
        {
            if (!DwarfContext<T>.IsConfigured())
                throw new InvalidOperationException("Assembly must be initialized first");

            SqlCeDatabaseScriptHelper.ExecuteCreateScript<T>();
        }

        public void ExecuteDropScript()
        {
            throw new NotImplementedException();
        }
    }
}