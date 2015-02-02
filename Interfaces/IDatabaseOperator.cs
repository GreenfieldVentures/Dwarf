using System.Collections.Generic;
using Evergreen.Dwarf.DataAccess;

namespace Evergreen.Dwarf.Interfaces
{
    public interface IDatabaseOperator
    {
        ITransactionWrapper OpenTransaction();
        
        List<dynamic> ExecuteQuery(QueryBuilder queryBuilder);
        List<dynamic> ExecuteQuery(string query);

        TY ExecuteScalar<TY>(QueryBuilder queryBuilder);
        TY ExecuteScalar<TY>(string query);

        void ExecuteNonQuery(QueryBuilder queryBuilder);
        void ExecuteNonQuery(string query);
    }
}