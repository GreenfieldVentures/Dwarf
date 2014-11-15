using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dwarf.DataAccess;
using Dwarf.Extensions;
using Dwarf.Interfaces;

namespace Dwarf
{
    internal class DbContextHelper<T> : DbContextHelper<T, T>
    {
        
    }

    internal class DbContextHelper<T, TY>
    {
        #region IsTransactionless

        private static bool IsTransactionless()
        {
            return typeof (TY).Implements<ITransactionless>();
        }

        #endregion IsTransactionless

        #region DbContext

        internal static IDbContext DbContext
        {
            get { return ContextAdapter<T>.GetDBContext(); }
        }

        #endregion DbContext

        #region Connection

        internal static DbConnection Connection
        {
            get { return IsTransactionless() ? DbContext.TransactionlessConnection : DbContext.Connection; }
            set
            {
                if (IsTransactionless())
                    DbContext.TransactionlessConnection = value;
                else 
                    DbContext.Connection = value;
            }
        }

        #endregion Connection

        #region Transaction

        internal static DbTransaction Transaction
        {
            get { return IsTransactionless() ? null : DbContext.Transaction; }
            set { DbContext.Transaction = value; }
        }

        #endregion Transaction

        #region CommitTransaction

        /// <summary>
        /// Commits the current transcation
        /// </summary>
        internal static void CommitTransaction()
        {
            try
            {
                Transaction.Commit();
                Transaction = null;
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
        }

        #endregion CommitTransaction

        #region RollbackTransaction

        /// <summary>
        /// Rollback the transaction
        /// </summary>
        internal static void RollbackTransaction()
        {
            Transaction.Rollback();
            Transaction = null;
        }

        #endregion RollbackTransaction

        #region FinalizeOperation

        /// <summary>
        /// Commit or rollback the transaction
        /// </summary>
        public static void FinalizeOperation(bool failed)
        {
            if (DbContext.TransactionDepth != 1 || IsTransactionless())
                return;

            if (!failed)
            {
                if (DbContext.InvalidObjects != null && DbContext.InvalidObjects.Count > 0)
                {
                    var aggregatedData = DbContext.InvalidObjects.Aggregate(string.Empty, (s, x) => s + x.Value.Errors).Remove(0, 2);
                    DbContext.InvalidObjects.Clear();
                    throw new DatabaseOperationException("The transaction contains objects with invalid foreign key values! " + aggregatedData, null);
                }

                CommitTransaction();
            }
            else
                RollbackTransaction();

            if (DbContext.CachedTypesToInvalidate != null)
            {
                foreach (var type in DbContext.CachedTypesToInvalidate)
                    CacheManager.ClearCacheForType(type);

                DbContext.CachedTypesToInvalidate.Clear();
            }
        }

        #endregion FinalizeOperation

        #region BeginOperation

        /// <summary>
        /// Increases the transaction depth.
        /// Also responsible for opening the connection
        /// </summary>
        public static void BeginOperation()
        {
            if (IsTransactionless())
                return;

            if (DbContext.TransactionDepth == 0)
            {
                OpenConnection();
                StartTransaction();
            }

            DbContext.TransactionDepth++;
        }

        #endregion BeginOperation

        #region EndOperation

        /// <summary>
        /// Decreases the transaction depth.
        /// Also responsible for closing the connection
        /// </summary>
        public static void EndOperation()
        {
            if (IsTransactionless())
                return;

            DbContext.TransactionDepth--;

            if (DbContext.TransactionDepth == 0)
                CloseConnection();

            if (DbContext.TransactionDepth < 0)
                throw new DataException("The transaction depth went subzero...");
        }

        #endregion EndOperation

        #region OpenConnection

        /// <summary>
        /// Opens and returns the current connection object
        /// </summary>
        internal static DbConnection OpenConnection()
        {
            if (Connection == null)
            {
                Connection = Cfg.Databases[typeof (T).Assembly].CreateConnection<T>();
                Connection.Open();
            }

            if (Connection.State == ConnectionState.Closed)
                Connection.Open();

            return Connection;
        }

        #endregion OpenConnection

        #region CloseConnection

        /// <summary>
        /// Closes the current connection object
        /// </summary>
        internal static void CloseConnection()
        {
            Connection.Close();
        }

        #endregion CloseConnection

        #region StartTransaction

        /// <summary>
        /// Assigns the current transaction
        /// </summary>
        private static void StartTransaction()
        {
            Transaction = Connection.BeginTransaction();
        }

        #endregion StartTransaction

        #region RegisterInvalidObject

        internal static void RegisterInvalidObject(IDwarf obj, List<string> faultyForeignKeys)
        {
            var value = faultyForeignKeys.Aggregate(string.Empty, (s, x) => s + "\r\n " + obj.GetType().Name + " - " + x);

            if (DbContext.InvalidObjects == null)
                DbContext.InvalidObjects = new Dictionary<int, dynamic>();

            DbContext.InvalidObjects[obj.GetHashCode()] = new { Dwarf = obj, Errors = value, };
        }

        #endregion RegisterInvalidObject

        #region UnRegisterInvalidObject

        internal static void UnRegisterInvalidObject(IDwarf obj)
        {
            if (DbContext.InvalidObjects == null)
                DbContext.InvalidObjects = new Dictionary<int, dynamic>();

            DbContext.InvalidObjects.Remove(obj.GetHashCode());
        }

        #endregion UnRegisterInvalidObject

        #region ClearCacheForType

        internal static Type ClearCacheForType(Type type)
        {
            if (DbContext.CachedTypesToInvalidate == null)
                DbContext.CachedTypesToInvalidate = new HashSet<Type>();

            DbContext.CachedTypesToInvalidate.Add(type);
            return type;
        }

        #endregion ClearCacheForType
    }
}