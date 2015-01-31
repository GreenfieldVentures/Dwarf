using System.Collections.Generic;
using Evergreen.Dwarf.DataAccess;

namespace Evergreen.Dwarf.Interfaces
{
    public interface IDatabaseOperator
    {
        ITransactionWrapper OpenTransaction();
        List<dynamic> ExecuteQuery(QueryBuilder queryBuilder);
        TY ExecuteScalar<TY>(QueryBuilder queryBuilder);
        void ExecuteNonQuery(QueryBuilder queryBuilder);

        List<dynamic> ExecuteQuery(string query);
        TY ExecuteScalar<TY>(string query);
        void ExecuteNonQuery(string query);
    }
}