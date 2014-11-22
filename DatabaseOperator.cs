using System.Collections.Generic;
using Dwarf.DataAccess;
using Dwarf.Interfaces;

namespace Dwarf
{
    internal class DatabaseOperator<T> : IDatabaseOperator
    {
        /// <summary>
        /// Opens a new transction
        /// </summary>
        public ITransactionWrapper OpenTransaction()
        {
            return TransactionWrapper<T>.BeginTransaction();
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns a list of dynamic objects
        /// </summary>
        public List<dynamic> ExecuteQuery(QueryBuilder queryBuilder)
        {
            return DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>(queryBuilder);
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        public TY ExecuteScalar<TY>(QueryBuilder queryBuilder)
        {
            return DwarfContext<T>.GetDatabase().ExecuteScalar<T, TY>(queryBuilder);
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        public void ExecuteNonQuery(QueryBuilder queryBuilder)
        {
            DwarfContext<T>.GetDatabase().ExecuteNonQuery<T>(queryBuilder);
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns a list of dynamic objects
        /// </summary>
        public List<dynamic> ExecuteQuery(string query)
        {
            return DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>(query);
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        public TY ExecuteScalar<TY>(string query)
        {
            return DwarfContext<T>.GetDatabase().ExecuteScalar<T, TY>(query);
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        public void ExecuteNonQuery(string query)
        {
            DwarfContext<T>.GetDatabase().ExecuteNonQuery<T>(query);
        }
    }
}