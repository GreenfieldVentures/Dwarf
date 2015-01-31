using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Caching;
using System.Web.Management;
using Evergreen.Dwarf.Attributes;
using Evergreen.Dwarf.Extensions;
using Evergreen.Dwarf.Interfaces;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf.DataAccess
{
    /// <summary>
    /// Main database class for handling object/database operations
    /// </summary>
    internal class SqlServerDatabase: IDatabase
    {
        #region Methods

        #region Select

        /// <summary>
        /// Builds and executes a select statement and returns an object of the type T
        /// </summary>
        public virtual T Select<T>(Guid id) where T : Dwarf<T>, new()
        {
            if (typeof(T).Implements<ICompositeId>())
                throw new InvalidOperationException("Use SelectReferencing for CompositeId types");

            return Select<T, T>(id);
        }

        public virtual TY Select<T, TY>(Guid id) where TY : Dwarf<TY>, new()
        {
            if (typeof(TY).Implements<ICompositeId>())
                throw new InvalidOperationException("Use SelectReferencing for CompositeId types");

            var command = new QueryBuilderLight().Select<TY>().From<TY>().Where<TY>("Id", id).ToQuery(true);

            TY result;

            if (!typeof(TY).Implements<ICacheless>() && CacheManager.TryGetCache(id, out result))
                return result;

            var sdr = ExecuteReader<T, TY>(command);

            try
            {
                result = sdr.Read() ? TupleToObject<TY>(sdr) : null;
            }
            catch (Exception e)
            {
                DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(e);
                throw new DatabaseOperationException(e.Message, e);
            }
            finally
            {
                sdr.Close();
            }

            return typeof(TY).Implements<ICacheless>() ? result : CacheManager.SetCache(id, result);
        }


        public T Select<T>(params WhereCondition<T>[] conditions) where T : Dwarf<T>, new()
        {
            return Select<T, T>(conditions);
        }

        public TY Select<T, TY>(params WhereCondition<TY>[] conditions) where TY : Dwarf<TY>, new()
        {
            if (!typeof(TY).Implements<ICompositeId>())
                throw new InvalidOperationException("Use Select for non-CompositeId types");

            var command = new QueryBuilder()
                    .Select<TY>()
                    .From<TY>()
                    .Where(conditions).ToQuery();

            var uniqueKey = DwarfHelper.GetUniqueKeyForCompositeId(conditions);

            TY result = null;

            if (!typeof(TY).Implements<ICacheless>() && CacheManager.TryGetCache(uniqueKey, out result))
                return result;

            var sdr = (SqlDataReader)ExecuteReader<T, TY>(command);

            try
            {
                result = sdr.Read() ? TupleToObject<TY>(sdr) : null;
            }
            catch (Exception e)
            {
                DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(e);
            }
            finally
            {
                sdr.Close();
            }

            if (typeof (TY).Implements<ICacheless>())
                return result;

            return result != null
                       ? CacheManager.SetCache(DwarfHelper.GetUniqueKeyForCompositeId(result), result)
                       : CacheManager.SetCache(DwarfHelper.GetUniqueKeyForCompositeId(conditions), (TY)null);
        }

        #endregion Select

        #region SelectReferencing
        
        /// <summary>
        /// Builds and executes a select statement and returns an List of the type T
        /// </summary>
        public virtual List<T> SelectReferencing<T>(params WhereCondition<T>[] conditions) where T : Dwarf<T>, new() 
        {
            return SelectReferencing<T>(new QueryBuilder().Select<T>().From<T>().Where(conditions).ToQuery());
        }        

        /// <summary>
        /// Builds and executes a select statement and returns an List of the type T
        /// </summary>
        public virtual List<TY> SelectReferencing<T, TY>(params WhereCondition<TY>[] conditions) where TY : Dwarf<TY>, new() 
        {
            return SelectReferencing<T, TY>(new QueryBuilder<T>().Select<TY>().From<TY>().Where(conditions).ToQuery());
        }
        
        /// <summary>
        /// Builds and executes a select statement and returns an List of the type T
        /// </summary>
        public virtual List<T> SelectReferencing<T>(QueryBuilder qb, bool overrideSelect = true, bool bypassCache = false) where T : Dwarf<T>, new()
        {
            return SelectReferencing<T, T>(overrideSelect ? qb.SelectOverride<T>().ToQuery() : qb.ToQuery(), bypassCache);
        }

        /// <summary>
        /// Builds and executes a select statement and returns an List of the type T
        /// </summary>
        public virtual List<T> SelectReferencing<T>(QueryMergers queryMerger, params QueryBuilder[] queryBuilders) where T : Dwarf<T>, new()
        {
            return SelectReferencing<T>(queryBuilders.Aggregate(string.Empty, (s, x) => s + "\r\n\r\n" + queryMerger.ToQuery() + "\r\n\r\n" + x.SelectOverride<T>().DisableOrderBy().ToQuery()).Remove(0, queryMerger.ToQuery().Length + 8));
        }

        /// <summary>
        /// Builds and executes a select statement and returns an List of the type T
        /// </summary>
        protected virtual List<T> SelectReferencing<T>(string command) where T : Dwarf<T>, new()
        {
            return SelectReferencing<T, T>(command);
        }

        /// <summary>
        /// Builds and executes a select statement and returns an List of the type T
        /// </summary>
        protected virtual List<TY> SelectReferencing<T, TY>(string command, bool bypassCache = false) where TY : Dwarf<TY>, new()
        {
            if (!typeof(TY).Implements<ICacheless>() || !bypassCache)
            {
                object cache = CacheManager.GetCacheList<TY>(command);

                if (cache != null)
                    return (List<TY>)cache;
            }
            
            var sdr = ExecuteReader<T, TY>(command);

            var result = new List<TY>();

            try
            {
                while (sdr.Read())
                    result.Add(TupleToObject<TY>(sdr));
            }
            catch (Exception e)
            {
                DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(e);
                throw new DatabaseOperationException(e.Message, e);
            }
            finally
            {
                sdr.Close();
            }

            return bypassCache || typeof(TY).Implements<ICacheless>() ? result : CacheManager.SetCacheList(command, result);
        }

        #endregion SelectReferencing

        #region Update

        /// <summary>
        /// Builds and executes an update statement
        /// </summary>
        public virtual void Update<T>(Dwarf<T> dwarf) where T: Dwarf<T>, new()
        {
            var type = DbContextHelper<T>.ClearCacheForType(DwarfHelper.DeProxyfy(dwarf));

            var command = new QueryBuilder()
                            .Update(type)
                            .Set(dwarf)
                            .Where(dwarf, Cfg.PKProperties[type]);

            ExecuteNonQuery<T>(command);
        }

        /// <summary>
        /// Builds and executes an update statement
        /// </summary>
        public virtual void Update<T>(Dwarf<T> dwarf, IEnumerable<ExpressionProperty> properties) where T: Dwarf<T>, new()
        {
            var type = DbContextHelper<T>.ClearCacheForType(DwarfHelper.DeProxyfy(dwarf));

            var command = new QueryBuilder<T>()
                            .Update(type)
                            .Set(dwarf, properties)
                            .Where(dwarf, Cfg.PKProperties[type]);

            ExecuteNonQuery<T>(command);
        }

        #endregion Update

        #region Insert

        /// <summary>
        /// Builds and executes insert statement
        /// </summary>
        public virtual void Insert<T, TY>(Dwarf<TY> dwarf, Guid? customId = null) where TY : Dwarf<TY>, new()
        {
            var type = DbContextHelper<T>.ClearCacheForType(DwarfHelper.DeProxyfy(dwarf));

            dwarf.Id = customId.HasValue ? customId.Value : Guid.NewGuid();

            ExecuteNonQuery<T, TY>(new QueryBuilder<T>().InsertInto(type).Values(dwarf));

            dwarf.IsSaved = true;
        }

        #endregion Insert

        #region BulkInsert

        /// <summary>
        /// Bulk inserts the supplied objects using SqlBulkCopy
        /// </summary>
        public virtual void BulkInsert<T>(IEnumerable<T> objects) where T : Dwarf<T>, new()
        {
            var list = objects.ToList();

            if (!list.Any())
                return;

            var properties = DwarfHelper.GetDBProperties<T>().ToList();

            var dt = new DataTable();

            foreach (var pi in properties)
            {
                if (pi.PropertyType.Implements<IDwarf>())
                    dt.Columns.Add(pi.Name + "Id", typeof(Guid));
                else if (pi.PropertyType.IsEnum())
                    dt.Columns.Add(pi.Name, typeof(string));
                else
                    dt.Columns.Add(pi.Name, Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType);
            }

            Parallel.ForEach(list, obj =>
            {
                if (obj.IsSaved)
                    return;

                obj.Id = obj.Id.HasValue ? obj.Id.Value : Guid.NewGuid();

                var row = new object[properties.Count];

                for (var i = 0; i < properties.Count; i++)
                {
                    var value = properties[i].GetValue(obj);

                    if (properties[i].PropertyType.Implements<IDwarf>())
                    {
                        if (value != null)
                            row[i] = ((IDwarf)value).Id;
                        else
                            row[i] = DBNull.Value;
                    }
                    else
                        row[i] = value ?? DBNull.Value;
                }

                obj.IsSaved = true;

                lock (dt.Rows.SyncRoot)
                    dt.Rows.Add(row);
            });

            var con = (SqlConnection)DbContextHelper<T>.OpenConnection();

            var copy = new SqlBulkCopy(con, SqlBulkCopyOptions.Default, (SqlTransaction)DbContextHelper<T>.Transaction)
            {
                DestinationTableName = typeof (T).Name,
                BulkCopyTimeout = 10000,
                BatchSize = 2500,
            };

            foreach (DataColumn column in dt.Columns)
                copy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

            copy.WriteToServer(dt);

            if (con != DbContextHelper<T>.Connection)
                con.Dispose();

            objects.ForEachX(x => x.IsSaved = true);
        }

        #endregion BulkInsert        

        #region InsertManyToMany

        /// <summary>
        ///Inserts two foreign key refernces into a mapping table
        /// </summary>
        public virtual void InsertManyToMany<T>(IDwarf obj1, IDwarf obj2, string alternateTableName = null)
        {
            if (!obj1.IsSaved)
                obj1.Save();

            if (!obj2.IsSaved)
                obj2.Save();

            var type1 = obj1.GetType();
            var type2 = obj2.GetType();

            var tableName = ManyToManyAttribute.GetManyToManyTableName(type1, type2, alternateTableName);

            DbContextHelper<T>.ClearCacheForType(type1);
            DbContextHelper<T>.ClearCacheForType(type2);

            var preReq = string.Format("IF NOT EXISTS (SELECT * FROM [{0}] WHERE {1}Id = {2} AND {3}Id = {4}) \r\n", 
                                        tableName, type1.Name, ValueToSqlString(obj1.Id), type2.Name, ValueToSqlString(obj2.Id));

            var query = new QueryBuilder<T>()
                            .InsertInto(tableName)
                            .Values(type1.Name + "Id", obj1.Id)
                            .Values(type2.Name + "Id", obj2.Id)
                         .ToQuery();

            ExecuteNonQuery<T>(preReq + query);
        }

        #endregion InsertManyToMany

        #region SelectManyToMany

        /// <summary>
        ///Inserts two foreign key refernces into a mapping tables
        /// </summary>
        public virtual List<T> SelectManyToMany<T>(IDwarf owner, string alternateTableName = null) where T : Dwarf<T>, new()
        {
            if (owner == null || !owner.IsSaved)
                return new List<T>();

            var targetType = typeof(T);
            var ownerType = owner.GetType();

            var tableName = ManyToManyAttribute.GetManyToManyTableName(targetType, ownerType, alternateTableName);

            var command = new QueryBuilderLight().Select<T>()
                .From<T>()
                .InnerJoin("[" + tableName + "] ON [" + tableName + "].[" + targetType.Name + "Id] = [" + targetType.Name + "].[Id]")
                .Where("[" + tableName + "].[" + ownerType.Name + "Id]", owner.Id, ownerType).ToQuery();

            if (!typeof (T).Implements<ICacheless>())
            {
                var cache = CacheManager.GetCacheList<T>(command);
                if (cache != null)
                    return cache;
            }

            var sdr = ExecuteReader<T>(command);

            var result = new List<T>();

            try
            {
                while (sdr.Read())
                    result.Add(TupleToObject<T>(sdr));
            }
            catch (Exception e)
            {
                DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(e);
                throw new DatabaseOperationException(e.Message, e);
            }
            finally
            {
                sdr.Close();
            }

            return typeof (T).Implements<ICacheless>() ? result : CacheManager.SetCacheList(command, result);
        }

        #endregion SelectManyToMany

        #region Delete

        /// <summary>
        /// Builds and executes a delete statement
        /// </summary>
        public virtual void Delete<T>(Dwarf<T> dwarf) where T:Dwarf<T>, new()
        {
            var type = DbContextHelper<T>.ClearCacheForType(DwarfHelper.DeProxyfy(dwarf));

            var command = new QueryBuilder<T>()
                            .DeleteFrom<T>()
                            .Where(dwarf, Cfg.PKProperties[type]);

            ExecuteNonQuery<T>(command);

            dwarf.IsSaved = false;
        }

        #endregion Delete

        #region DeleteManyToMany

        /// <summary>
        ///Delete two foreign key refernces from the mapping table
        /// </summary>
        public virtual void DeleteManyToMany<T>(IDwarf obj1, IDwarf obj2, string alternateTableName = null)
        {
            var type1 = obj1.GetType();
            var type2 = obj2.GetType();

            var tableName = ManyToManyAttribute.GetManyToManyTableName(type1, type2, alternateTableName);

            DbContextHelper<T>.ClearCacheForType(type1);
            DbContextHelper<T>.ClearCacheForType(type2);

            var query = new QueryBuilder()
                            .DeleteFrom("dbo.[" + tableName + "]")
                            .Where("[" + type1.Name + "Id] = " + ValueToSqlString(obj1.Id))
                            .Where("[" + type2.Name + "Id] = " + ValueToSqlString(obj2.Id));

            ExecuteNonQuery<T>(query);
        }

        #endregion DeleteManyToMany

        #region CreateConnection

        /// <summary>
        /// Creates and returns a new DbConnection
        /// </summary>
        public virtual DbConnection CreateConnection<T>()
        {
            var assembly = typeof(T).Assembly;

            return new SqlConnection(Cfg.ConnectionString[assembly]);
        }

        #endregion CreateConnection

        #region CreateConnectionString

        public virtual DbConnectionStringBuilder CreateConnectionString<T>(string connectionString)
        {
            return new SqlConnectionStringBuilder(connectionString) {MultipleActiveResultSets = true};
        }

        #endregion CreateConnectionString

        #region CreateCommand

        internal virtual DbCommand CreateCommand<T>(string query, DbConnection con)
        {
            return new SqlCommand(query, (SqlConnection)con);
        }

        #endregion CreateCommand

        #region ExecuteReader

        /// <summary>
        /// Calls the SqlCommand's Execute reader, after initializing
        /// all local connections and transactions, and returns an SqlDataReader
        /// </summary>
        protected internal virtual DbDataReader ExecuteReader<T>(string query)
        {
            return ExecuteReader<T, T>(query);
        }

        /// <summary>
        /// Calls the SqlCommand's Execute reader, after initializing
        /// all local connections and transactions, and returns an SqlDataReader
        /// </summary>
        internal protected virtual DbDataReader ExecuteReader<T, TY>(string query)
        {
            var con = DbContextHelper<T, TY>.OpenConnection();

            try
            {
                var command = CreateCommand<T>(query, con);

                if (DbContextHelper<T, TY>.Transaction != null)
                    command.Transaction = DbContextHelper<T, TY>.Transaction;

                return command.ExecuteReader();
            }
            catch (DbException sqle)
            {
                sqle.Source = query;
                throw;
            }
            finally
            {
                if (con != DbContextHelper<T, TY>.Connection)
                    con.Dispose();
            }
        }

        #endregion ExecuteReader

        #region ExecuteNonQuery

        /// <summary>
        /// Calls the SqlCommand's Execute reader after initializing
        /// all local connections and transactions.
        /// </summary>
        public virtual void ExecuteNonQuery<T>(QueryBuilder queryBuilder)
        {
            ExecuteNonQuery<T, T>(queryBuilder);
        }

        /// <summary>
        /// Calls the SqlCommand's Execute reader after initializing
        /// all local connections and transactions.
        /// </summary>
        public virtual void ExecuteNonQuery<T, TY>(QueryBuilder queryBuilder)
        {
            ExecuteNonQuery<T, TY>(queryBuilder.ToQuery());
        }

        /// <summary>
        /// Calls the SqlCommand's Execute reader after initializing
        /// all local connections and transactions.
        /// </summary>
        public void ExecuteNonQuery<T>(string query)
        {
            ExecuteNonQuery<T, T>(query);
        }

        /// <summary>
        /// Calls the SqlCommand's Execute reader after initializing
        /// all local connections and transactions.
        /// </summary>
        public void ExecuteNonQuery<T, TY>(string query)
        {
            var con = DbContextHelper<T, TY>.OpenConnection();

            var command = CreateCommand<T>(query, con);
            
            try
            {
                if (DbContextHelper<T, TY>.Transaction != null)
                    command.Transaction = DbContextHelper<T, TY>.Transaction;
                
                command.ExecuteNonQuery();
            }
            catch (DbException sqle)
            {
                sqle.Source = command.CommandText;
                throw;
            }
            finally
            {
                if (con != DbContextHelper<T, TY>.Connection)
                    con.Dispose();
            }
        }

        #endregion ExecuteNonQuery

        #region ExecuteCustomQuery

        /// <summary>
        /// Executes the supplied query in the database
        /// </summary>
        public virtual List<dynamic> ExecuteCustomQuery<T>(string query)
        {
            var con = DbContextHelper<T>.OpenConnection();
            var command = CreateCommand<T>(query, con);

            var result = new List<dynamic>();

            try
            {
                if (DbContextHelper<T>.Transaction != null)
                    command.Transaction = DbContextHelper<T>.Transaction;

                var sdr = command.ExecuteReader();

                try
                {
                    while (sdr.Read())
                    {
                        IDictionary<string, object> obj = new ExpandoObject();

                        for (var i = 0; i < sdr.FieldCount; i++)
                        {
                            var fieldName = sdr.GetName(i);

                            if (string.IsNullOrEmpty(fieldName))
                                throw new InvalidOperationException("You must name your columns for the auto-binding to work");

                            obj[fieldName] = sdr.GetValue(i);
                        }

                        result.Add(obj);
                    }
                }
                catch (Exception e)
                {
                    DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(e);
                    throw new DatabaseOperationException(e.Message, e);
                }
                finally
                {
                    sdr.Close();
                }
            }
            catch (DbException sqle)
            {
                sqle.Source = query;
                throw;
            }
            finally
            {
                if (con != DbContextHelper<T>.Connection)
                    con.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Executes the supplied query in the database
        /// </summary>
        public virtual List<dynamic> ExecuteCustomQuery<T>(QueryBuilder queryBuilder)
        {
            return ExecuteCustomQuery<T>(queryBuilder.ToQuery());
        }
      

        #endregion ExecuteCustomQuery

        #region ExecuteScalar

        /// <summary>
        /// Calls the SqlCommand's Execute scalar, after initializing
        /// all local connections and transactions, and returns an object
        /// </summary>
        public virtual TY ExecuteScalar<T, TY>(QueryBuilder queryBuilder)
        {
            return ExecuteScalar<T, TY>(queryBuilder.ToQuery());
        }

        public virtual TY ExecuteScalar<T, TY>(string query)
        {
            var con = DbContextHelper<T, TY>.OpenConnection();

            var command = CreateCommand<T>(query, con);

            object result;

            try
            {
                if (DbContextHelper<T, TY>.Transaction != null)
                    command.Transaction = DbContextHelper<T, TY>.Transaction;

                result = command.ExecuteScalar();
            }
            catch (DbException sqle)
            {
                sqle.Source = query;
                throw;
            }
            finally
            {
                if (con != DbContextHelper<T, TY>.Connection)
                    con.Dispose();
            }

            if (result is TY)
                return (TY)result;

            if (result is DBNull || result == null)
                return default(TY);

            return (TY)Convert.ChangeType(result, typeof(TY));
        }

        #endregion ExecuteScalar

        #region TupleToObject

        /// <summary>
        /// Converts an SqlDataReader row into an object of the type T
        /// </summary>
        protected T TupleToObject<T>(DbDataReader sdr) where T : Dwarf<T>, new()
        {
            var type = typeof(T);

            var obj = DwarfHelper.CreateInstance<T>();

            for (var i = 0; i < sdr.FieldCount; i++)
            {
                var propertyName = sdr.GetName(i);
                var propertyValue = sdr.GetValue(i);

                var pi = PropertyHelper.GetProperty(type, propertyName) ?? PropertyHelper.GetProperty(type, propertyName.Replace("Id", string.Empty));

                if (pi == null)
                    continue;

                if (propertyValue is DBNull)
                    propertyValue = pi.ContainedProperty.PropertyType == typeof(string) ? string.Empty : null;
                else if (pi.PropertyType.Implements<IGem>())
                    propertyValue = Cfg.LoadGem[pi.PropertyType](propertyValue);
                else if (pi.PropertyType.IsEnum() && propertyValue != null)
                    propertyValue = Enum.Parse(pi.PropertyType.GetTrueEnumType(), propertyValue.ToString());
                else if (pi.PropertyType.Implements<Type>())
                    propertyValue = typeof(T).Assembly.GetType(propertyValue.ToString());
                else if (pi.PropertyType.Implements<IDwarf>())
                {
                    var att = DwarfPropertyAttribute.GetAttribute(pi.ContainedProperty);

                    if (att != null)
                    {
                        if (DwarfPropertyAttribute.IsFK(pi))
                        {
                            obj.SetOriginalValue(pi.Name, propertyValue);

                            if (att.EagerLoad)
                            {
                                var targetType = pi.PropertyType;

                                if (propertyValue != null)
                                    PropertyHelper.SetValue(obj, pi.Name, Cfg.LoadExpressions[targetType]((Guid)propertyValue));
                            }
                        
                            continue;
                        }
                    }
                }

                if (pi.PropertyType == typeof(string) && propertyValue != null)
                    propertyValue = propertyValue.ToString().Replace("\\r\\n", "\r\n");

                PropertyHelper.SetValue(obj, pi.Name, propertyValue);
                obj.SetOriginalValue(pi.Name, propertyValue);
            }

            obj.IsSaved = true;

            return obj;
        }

        #endregion TupleToObject
        
        #region ValueToSqlString

        /// <summary>
        /// Converts C# type values into an sql friendly representative
        /// </summary>
        public string ValueToSqlString(object value)
        {
            if (value == null)
                return "NULL";
            if (value is IDwarf)
                return ValueToSqlString(((IDwarf)value).Id);
            if (value is int)
                return value.ToString();
            if (value is bool)
                return ((bool)value) ? "1" : "0";
            if (value is Guid)
                return "'" + value + "'";
            if (value is string)
                return "'" + value.ToString().Replace("'", "''").Replace("\r\n", "\\r\\n").Replace("\n", "\\r\\n") + "'";
            if (value is double)
                return value.ToString().Replace(',', '.');            
            if (value is decimal)
                return value.ToString().Replace(',', '.');
            if (value is Enum)
                return ValueToSqlString(value.ToString());            
            if (value is Type)
                return ValueToSqlString(value.ToString());
            if (value is byte[])
                return ("0x" + BitConverter.ToString((byte[]) value).RemoveAll("-"));
            if (value is IGemList)
                return ValueToSqlString(((IEnumerable<IGem>)value).Flatten(x => "¶" + x.Id + "¶"));
            if (value is DateTime)
            {
                var date = (DateTime)value;

                if (date.Year < 1753)
                    date = new DateTime(1753, 1, 1);
                if (date.Year > 9999)
                    date = new DateTime(9999, 12, 31);

                return string.Format("CONVERT(datetime, '{0}', 101)", date.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
            if (value is IGem)
                return ValueToSqlString(((IGem) value).Id.ToString()); 

            throw new Exception("Non-supported type: " + value.GetType().Name);
        }

        #endregion ValueToSqlString
        
        #endregion Methods
    }
}
