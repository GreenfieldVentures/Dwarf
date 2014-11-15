using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarf.DataAccess.SqlCe
{
    internal class SqlCeDatabase: SqlServerDatabase
    {
        internal override DbCommand CreateCommand<T>(string query, DbConnection con)
        {
            return new SqlCeCommand(PurifyQuery(query), (SqlCeConnection)con);
        }

        public override DbConnection CreateConnection<T>()
        {
            var assembly = typeof(T).Assembly;

            return new SqlCeConnection(Cfg.ConnectionString[assembly]);
        }

        public override DbConnectionStringBuilder CreateConnectionString<T>(string connectionString)
        {
            return new SqlCeConnectionStringBuilder(connectionString);
        }

        #region PurifyQuery

        internal static string PurifyQuery(string query)
        {
            return query
                .Replace("[", "")
                .Replace("]", "")
                .Replace("dbo.", "")
                .Replace("\t\t", " ")
                .Replace("\t", "")
                .Replace("varchar", "nvarchar")
                .Replace("\r\n", " ");
        }

        #endregion PurifyQuery
    }
}
