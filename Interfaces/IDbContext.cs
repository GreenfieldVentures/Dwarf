using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace Evergreen.Dwarf.Interfaces
{
    internal interface IDbContext :IDisposable
    {
        DbConnection TransactionlessConnection { get; set; }
        bool IsAuditLoggingSuspended { get; set; }
        DbConnection Connection { get; set; }
        DbTransaction Transaction { get; set; }
        int TransactionDepth { get; set; }
        HashSet<Type> CachedTypesToInvalidate { get; set; }
        Dictionary<int, dynamic> InvalidObjects { get; set; }
    }
}