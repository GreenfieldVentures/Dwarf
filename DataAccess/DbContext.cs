using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Dwarf.Interfaces;

namespace Dwarf
{
    internal class DbContext : IDbContext
    {
        public int TransactionDepth { get; set; }
        public bool IsAuditLoggingSuspended { get; set; }

        public Dictionary<int, dynamic> InvalidObjects { get; set; }
        public HashSet<Type> CachedTypesToInvalidate { get; set; }
        public DbConnection Connection { get; set; }
        public DbConnection TransactionlessConnection { get; set; }
        public DbTransaction Transaction { get; set; }
        
        public void Dispose()
        {
            if (Connection != null && Connection.State != ConnectionState.Closed)
                Connection.Close();

            if (TransactionlessConnection != null && TransactionlessConnection.State != ConnectionState.Closed)
                TransactionlessConnection.Close();
        }
    }
}