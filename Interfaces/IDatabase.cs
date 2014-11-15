using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dwarf.DataAccess;
using Dwarf.Utilities;

namespace Dwarf.Interfaces
{
    public interface IDatabase
    {
        DbConnection CreateConnection<T>();
        DbConnectionStringBuilder CreateConnectionString<T>(string connectionString);

        T Select<T>(Guid id) where T : Dwarf<T>, new();
        T Select<T>(params WhereCondition<T>[] conditions) where T : Dwarf<T>, new();

        TY Select<T, TY>(Guid id) where TY : Dwarf<TY>, new();
        TY Select<T, TY>(params WhereCondition<TY>[] conditions) where TY : Dwarf<TY>, new();
        
        void Update<T>(Dwarf<T> dwarf) where T : Dwarf<T>, new();
        void Update<T>(Dwarf<T> dwarf, IEnumerable<ExpressionProperty> properties) where T : Dwarf<T>, new();

        void Insert<T, TY>(Dwarf<TY> dwarf, Guid? customId = null) where TY : Dwarf<TY>, new();
        
        TY ExecuteScalar<T, TY>(QueryBuilder queryBuilder);
        TY ExecuteScalar<T, TY>(string query);

        List<T> SelectReferencing<T>(QueryMergers queryMerger, QueryBuilder[] queryBuilders) where T : Dwarf<T>, new();
        List<T> SelectReferencing<T>(QueryBuilder qb, bool overrideSelect = true, bool bypassCache = false) where T : Dwarf<T>, new();
        List<T> SelectReferencing<T>(params WhereCondition<T>[] conditions) where T : Dwarf<T>, new();
        List<TY> SelectReferencing<T, TY>(params WhereCondition<TY>[] conditions) where TY : Dwarf<TY>, new();

        void InsertManyToMany<T>(IDwarf owner, IDwarf child, string alternateTableName);
        List<T> SelectManyToMany<T>(IDwarf ownerObject, string alternateTableName) where T : Dwarf<T>, new();
        void DeleteManyToMany<T>(IDwarf obj1, IDwarf obj2, string alternateTableName = null);
        
        void Delete<T>(Dwarf<T> dwarf) where T : Dwarf<T>, new();
        void BulkInsert<T>(IEnumerable<T> objects) where T : Dwarf<T>, new();

        List<dynamic> ExecuteCustomQuery<T>(string query);
        List<dynamic> ExecuteCustomQuery<T>(QueryBuilder queryBuilder);

        void ExecuteNonQuery<T>(string query);
        void ExecuteNonQuery<T, TY>(string query);
        void ExecuteNonQuery<T>(QueryBuilder queryBuilder);
        void ExecuteNonQuery<T, TY>(QueryBuilder queryBuilder);

        string ValueToSqlString(object value);
    }
}
