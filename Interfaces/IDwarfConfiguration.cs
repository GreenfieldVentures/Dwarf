using System;
using System.Collections.Generic;
using Evergreen.Dwarf.DataAccess;
using Evergreen.Dwarf.Interfaces;

namespace Evergreen.Dwarf.Interfaces
{
    public interface IDwarfConfiguration
    {
        IErrorLogService ErrorLogService { get; set; }
        IAuditLogService AuditLogService { get; set; }
        IUserService UserService { get; set; }
        IDatabaseScripts DatabaseScripts { get; }
        IDwarfConfiguration Configure();
        IDatabaseOperator Database { get; }

        IEnumerable<Type> GetDwarfTypes();
        IEnumerable<Type> GetGemTypes();
        void SuspendAuditLogging();
        void ResumeAuditLogging();
    }
}