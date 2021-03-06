﻿using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf.Interfaces
{
    public interface IDatabaseScripts
    {
        string GetCreateScript();
        string GetDropScript();
        string GetUpdateScript();
        string GetTransferScript(string otherDatabaseName, params ExpressionProperty[] toSkip);
        void ExecuteCreateScript();
        void ExecuteDropScript();
    }    
}