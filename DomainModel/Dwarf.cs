using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Schema;
using Evergreen.Dwarf.Attributes;
using Evergreen.Dwarf.Extensions;
using Evergreen.Dwarf.DataAccess;
using Evergreen.Dwarf.Interfaces;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf
{
    /// <summary>
    /// Base class for all auto perstitent objects. The class also implements
    /// the DAL functionality for its inheritors.
    /// </summary>
    public abstract class Dwarf<T> : IDwarf where T : Dwarf<T>, new()
    {
        #region Variables

        private Dictionary<string, object> originalValues;
        private Dictionary<string, string> oneToManyAlternateKeys;
        private Guid? internallyProvidedCustomId;
        private bool isDeleted;

        #endregion Variables

        #region Properties

        #region Id

        /// <summary>
        /// Id - Auto Persistent
        /// </summary>
        [DwarfProperty(IsPrimaryKey = true)]
        public Guid? Id { get; set; }

        #endregion Id

        #region IsSaved

        /// <summary>
        /// Specifies wether the current object already resides in the database
        /// </summary>
        public bool IsSaved { get; set; }

        #endregion IsSaved

        #region IsDirty

        /// <summary>
        /// Gets if this object has dirty (non-saved) values
        /// </summary>
        [IgnoreDataMember]
        [Unvalidatable]
        public bool IsDirty
        {
            get { return CreateTraceEventsForProperties().Length + CreateTraceEventsForOneToMany().Count() + CreateTraceEventsForManyToMany().Count() > 0; }
        }

        #endregion IsDirty

        #region Database

        /// <summary>
        /// A shortcut to the configuration object's Database property
        /// </summary>
        public static IDatabaseOperator Database
        {
            get { return DwarfContext<T>.GetConfiguration().Database; }
        }

        #endregion Database

        #endregion Properties

        #region Methods

        #region Collection

        /// <summary> 
        /// Helper method for handling GemLists
        /// </summary>
        protected GemList<TY> Gems<TY>(Expression<Func<T, DwarfList<TY>>> property) where TY : Gem<TY>, new()
        {
            var propertyName = ReflectionHelper.GetPropertyName(property);
            var key = GetCollectionKey(propertyName);

            var cacheItem = CacheManager.Cache[key] as GemList<TY>;

            if (cacheItem != null)
                return cacheItem;

            if (originalValues == null || !originalValues.ContainsKey(propertyName) || originalValues[propertyName] == null)
                return CacheManager.SetCacheList(key, new GemList<TY>(x => x.Id));

            return CacheManager.SetCacheList(key, new GemList<TY>(GemList<TY>.ParseValue(originalValues[propertyName].ToString()), x => x.Id));
        }

        #endregion Collection

        #region OneToMany

        /// <summary>
        /// Helper method for handling OneToMany collections
        /// </summary>
        /// <typeparam name="TY">The Type contained by the List</typeparam>
        /// <param name="property">The name of the owning property</param>
        /// <param name="alternatePrimaryKey">An alternate primary key of the containing Type</param>
        /// <param name="alternateReferencingColumn">Specifies an alterante referencing column. Default is the name of the containing type</param>
        protected DwarfList<TY> OneToMany<TY>(Expression<Func<T, DwarfList<TY>>> property, Expression<Func<TY, object>> alternatePrimaryKey = null, Expression<Func<TY, object>> alternateReferencingColumn = null) where TY : Dwarf<TY>, new()
        {
            var propertyName = ReflectionHelper.GetPropertyName(property);

            if (oneToManyAlternateKeys == null)
                oneToManyAlternateKeys = new Dictionary<string, string>();

            if (alternateReferencingColumn != null)
                oneToManyAlternateKeys[propertyName] = ReflectionHelper.GetPropertyName(alternateReferencingColumn);

            var key = GetCollectionKey(propertyName);
            var list = CacheManager.GetCollectionCache<TY>(key);

            if (list == null)
            {
                var resultList = IsSaved ? InitializeOneToMany(alternatePrimaryKey, alternateReferencingColumn) : new DwarfList<TY>(alternatePrimaryKey);
                CacheManager.SetCollectionCache(key, resultList);
                return resultList;
            }

            return list;
        }

        #endregion OneToMany

        #region GenerateQueryBuilderForOneToMany

        private QueryBuilder GenerateQueryBuilderForOneToMany<TY>(Expression<Func<TY, object>> alternateReferencingColumn = null) where TY : IDwarf
        {
            var referencingColumn = PropertyHelper.GetProperty(typeof(TY), alternateReferencingColumn == null
                                                                                      ? typeof (T).Name
                                                                                      : ReflectionHelper.GetPropertyName(alternateReferencingColumn));

            return new QueryBuilder()
                .Select<TY>()
                .From<TY>()
                .Where(new WhereCondition<TY> { ColumnPi = referencingColumn.ContainedProperty, Value = this });
        }

        #endregion GenerateQueryBuilderForOneToMany

        #region GetCollectionKey

        private string GetCollectionKey(string propertyName)
        {
            var id = Id.HasValue ? Id.Value : (internallyProvidedCustomId.HasValue ? internallyProvidedCustomId.Value : (Guid?)null);

            if (!IsSaved && !id.HasValue)
                internallyProvidedCustomId = Guid.NewGuid();

            return CacheManager.GetUserKey((T)this, internallyProvidedCustomId) + ":" + propertyName;
        }

        #endregion GetCollectionKey

        #region ManyToMany

        /// <summary>
        /// Helper method for handling ManyToMany collections
        /// </summary>
        /// <typeparam name="TY">The Type contained by the List</typeparam>
        /// <param name="property">The name of the owning property</param>
        protected DwarfList<TY> ManyToMany<TY>(Expression<Func<T, DwarfList<TY>>> property) where TY : Dwarf<TY>, new()
        {
            var propertyName = ReflectionHelper.GetPropertyName(property);

            var key = GetCollectionKey(propertyName);
            var list = CacheManager.GetCollectionCache<TY>(key);

            if (list == null)
            {
                var resultList = IsSaved ? InitializeManyToMany(property) : new DwarfList<TY>();
                CacheManager.SetCollectionCache(key, resultList);
                return resultList;
            }

            return list;
        }

        #endregion ManyToMany

        #region IsCollectionInitialized

        /// <summary>
        /// Returns true if the collection has been initialized
        /// </summary>
        protected bool IsCollectionInitialized(Expression<Func<T, object>> expression)
        {
            var pi = ReflectionHelper.GetPropertyInfo(expression);
            return IsCollectionInitialized(pi);
        }

        /// <summary>
        /// Returns true if the collection has been initialized
        /// </summary>
        protected bool IsCollectionInitialized(PropertyInfo pi)
        {
            var key = GetCollectionKey(pi.Name);
            return CacheManager.ContainsKey(key);
        }

        #endregion IsCollectionInitialized

        #region Save

        /// <summary>
        /// See base
        /// </summary>
        public void Save()
        {
            if (isDeleted)
                return;

            try
            {
                DbContextHelper<T>.BeginOperation();

                if (!Id.HasValue)
                    Id = internallyProvidedCustomId.HasValue ? internallyProvidedCustomId : Guid.NewGuid();

                OnBeforeSave();

                var faultyForeignKeys = FaultyForeignKeys(this).ToList();

                if (faultyForeignKeys.Count > 0)
                {
                    DbContextHelper<T>.RegisterInvalidObject(this, faultyForeignKeys);
                    DbContextHelper<T>.FinalizeOperation(false);
                    return;
                }
                else
                    DbContextHelper<T>.UnRegisterInvalidObject(this);

                var actionType = AuditLogTypes.Updated;
              
                var traces = CreateTraceEventsForProperties();

                if (IsSaved)
                {
                    if (traces.Length > 0) //IsDirty
                        DwarfContext<T>.GetDatabase().Update(this, traces.Select(x => PropertyHelper.GetProperty<T>(x.PropertyName)));
                }
                else
                {
                    actionType = AuditLogTypes.Created;
                    DwarfContext<T>.GetDatabase().Insert<T, T>(this, Id);
                }

                CreateAuditLog(actionType, traces);
                PersistOneToManyCollections();
                PersistManyToManyCollections(); 

                OnAfterSave();
                DbContextHelper<T>.FinalizeOperation(false);
                OnAfterSaveInternal();
            }
            catch (Exception e)
            {
                DbContextHelper<T>.FinalizeOperation(true);
                DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(e);
                throw;
            }
            finally
            {
                DbContextHelper<T>.EndOperation();
            }
        }

        #endregion Save

        #region BulkInsert

        /// <summary>
        /// A quick way to insert a lot of objects at the same time. No audit logs are created though
        /// </summary>
        protected static void BulkInsert(IEnumerable<T> objects)
        {
            try
            {
                DbContextHelper<T>.BeginOperation();

                DwarfContext<T>.GetDatabase().BulkInsert(objects);

                DbContextHelper<T>.FinalizeOperation(false);
            }
            catch (Exception e)
            {
                DbContextHelper<T>.FinalizeOperation(true);
                DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(e);
                throw;
            }
            finally
            {
                DbContextHelper<T>.EndOperation();
            }
        }

        #endregion BulkInsert

        #region PersistManyToMany

        /// <summary>
        /// Saves a ManyToMany relationship
        /// </summary>
        private static void PersistManyToMany(IDwarf owner, IDwarf child, string alternateTableName = null)
        {
            try
            {
                DbContextHelper<T>.BeginOperation();
                DwarfContext<T>.GetDatabase().InsertManyToMany<T>(owner, child, alternateTableName);
                DbContextHelper<T>.FinalizeOperation(false);
            }
            catch (Exception e)
            {
                DbContextHelper<T>.FinalizeOperation(true);
                DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(e);
                throw;
            }
            finally
            {
                DbContextHelper<T>.EndOperation();
            }
        }

        #endregion PersistManyToMany

        #region DeleteManyToMany

        /// <summary>
        /// Deletes a ManyToMany relationship
        /// </summary>
        private static void DeleteManyToMany(IDwarf owner, IDwarf child, string alternateTableName = null)
        {
            try
            {
                DbContextHelper<T>.BeginOperation();
                DwarfContext<T>.GetDatabase().DeleteManyToMany<T>(owner, child, alternateTableName);
                DbContextHelper<T>.FinalizeOperation(false);
            }
            catch (Exception e)
            {
                DbContextHelper<T>.FinalizeOperation(true);
                DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(e);
                throw;
            }
            finally
            {
                DbContextHelper<T>.EndOperation();
            }
        }

        #endregion DeleteManyToMany

        #region Load

        /// <summary>
        /// Returns an object of the type T with the supplied Id
        /// </summary>
        public static T Load(Guid id)
        {
            if (typeof(T).Implements<ICompositeId>())
                throw new InvalidOperationException("This method may not be used for ICompositeId types");

            return DwarfContext<T>.GetDatabase().Select<T>(id);
        }

        /// <summary>
        /// Returns an object of the type T with the supplied Id
        /// </summary>
        protected static T Load(params WhereCondition<T>[] conditions)
        {
            if (!typeof(T).Implements<ICompositeId>())
                throw new InvalidOperationException("This method may only be used for ICompositeId types");

            return DwarfContext<T>.GetDatabase().Select(conditions);
        }

        #endregion Load

        #region LoadAll

        /// <summary>
        /// Returns a collection of all objects of the type XXX
        /// </summary>
        public static List<T> LoadAll()
        {
            return DwarfContext<T>.GetDatabase().SelectReferencing<T>();
        }

        #endregion LoadAll

        #region LoadReferencing

        /// <summary>
        /// Returns an object collection of the type T where 
        /// the supplied value matches the supplied value(s)
        /// </summary>
        protected static List<TY> LoadReferencing<TY>(params WhereCondition<TY>[] conditions) where TY : Dwarf<TY>, new()
        {
            return DwarfContext<T>.GetDatabase().SelectReferencing<T, TY>(conditions);
        }        
   
        /// <summary>
        /// Returns an object collection of the type T where 
        /// the supplied value matches the supplied value(s)
        /// </summary>
        protected static List<TY> LoadReferencing<TY>(QueryBuilder queryBuilder, bool overrideSelect = true) where TY : Dwarf<TY>, new()
        {
            return DwarfContext<T>.GetDatabase().SelectReferencing<TY>(queryBuilder, overrideSelect);
        }
        /// <summary>
        /// Returns an object collection of the type T where 
        /// the supplied value matches the supplied value(s)
        /// </summary>
        protected static List<TY> LoadReferencing<TY>(QueryMergers queryMerger, params QueryBuilder[] queryBuilders) where TY : Dwarf<TY>, new()
        {
            return DwarfContext<T>.GetDatabase().SelectReferencing<TY>(queryMerger, queryBuilders);
        }

        #endregion LoadReferencing

        #region LoadManyToManyRelation

        /// <summary>
        ///Returns an object collection of the type T where 
        /// the supplied value matches the the target values via a common many to many table
        /// </summary>
        private static List<TY> LoadManyToManyRelation<TY>(IDwarf ownerObject, string alternateTableName = null) where TY : Dwarf<TY>, new()
        {
            return DwarfContext<T>.GetDatabase().SelectManyToMany<TY>(ownerObject, alternateTableName);
        }

        #endregion LoadManyToManyRelation

        #region Delete

        /// <summary>
        /// See base
        /// </summary>
        public void Delete()
        {
            if (!IsSaved)
                return;

            try
            {
                DbContextHelper<T>.BeginOperation();

                OnBeforeDeleteInternal();
                OnBeforeDelete();
                DwarfContext<T>.GetDatabase().Delete(this);
                DbContextHelper<T>.FinalizeOperation(false);
                OnAfterDeleteInternal();
                OnAfterDelete();
            }
            catch (Exception e)
            {
                DbContextHelper<T>.FinalizeOperation(true);
                DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(e);
                throw;
            }
            finally
            {
                DbContextHelper<T>.EndOperation();
            }
        }

        #endregion Delete

        #region Refresh

        /// <summary>
        /// Restores the object's properties to their original values
        /// </summary>
        public void Refresh()
        {
            //Fetch the original values from the database (bypass the cache...)
            var originalObject = DwarfContext<T>.GetDatabase().SelectReferencing<T>(new QueryBuilder().Select<T>().From<T>().Where(this, Cfg.PKProperties[DwarfHelper.DeProxyfy(this)]), false, true).FirstOrDefault();

            if (originalObject != null)
            {
                foreach (var ep in DwarfHelper.GetDBProperties(GetType()))
                    SetOriginalValue(ep.Name, ep.GetValue(originalObject));
            }
            
            //Else should we throw an exception since the object has been deleted from the DB?

            Reset();
        }

        #endregion Refresh

        #region Reset

        /// <summary>
        /// Restores the object's properties to their original values
        /// </summary>
        public void Reset()
        {
            foreach (var kvp in originalValues)
                PropertyHelper.SetValue(this, kvp.Key, kvp.Value);

            ResetFKProperties();

            foreach (var ep in DwarfHelper.GetOneToManyProperties(this))
            {
                var key = GetCollectionKey(ep.Name);
                CacheManager.RemoveKey(key);
            }

            foreach (var ep in DwarfHelper.GetManyToManyProperties(this))
            {
                var key = GetCollectionKey(ep.Name);
                CacheManager.RemoveKey(key);
            }

            if (oneToManyAlternateKeys != null)
                oneToManyAlternateKeys.Clear();
        }

        #endregion Reset

        #region Equals

        /// <summary>
        /// Dwarf comparison is done via the Id property
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is IDwarf)
            {
                var type = DwarfHelper.DeProxyfy(GetType());

                if (type != DwarfHelper.DeProxyfy(obj))
                    return false;

                if (!type.Implements<ICompositeId>())
                {
                    //Should both objects not be stored they'll both have null as Id, thus we check all the properties to find out if they might be the same object
                    //We also dubbelcheck all unique properties (if existant) to make sure they're NOT equal
                    if (!Id.HasValue && !((IDwarf)obj).Id.HasValue)
                    {
                        var propertiesMatch = DwarfHelper.GetDBProperties(type).Where(ep => ep.GetValue(this) != null && ep.GetValue(obj) != null).All(pi => pi.GetValue(this).Equals(pi.GetValue(obj)));

                        var uniqueProps = DwarfHelper.GetUniqueDBProperties<T>(type).ToList();

                        if (uniqueProps.Any())
                        {
                            var uniquePropertiesMatch = uniqueProps.All(ep => ep.GetValue(this) == null ? ep.GetValue(obj) == null : ep.GetValue(this).Equals(ep.GetValue(obj)));

                            return uniquePropertiesMatch && propertiesMatch;
                        }

                        return propertiesMatch;
                    }

                    return Id == ((IDwarf)obj).Id;
                }

                return Cfg.PKProperties[type].All(ep => ep.GetValue(this) == null ? ep.GetValue(obj) == null : ep.GetValue(this).Equals(ep.GetValue(obj)));
            }

            return false;
        }

        /// <summary>
        /// Foreced override by overriding Equals
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion Equals

        #region CompareTo

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates 
        /// whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        public virtual int CompareTo(object obj)
        {
            return obj.ToString().CompareTo(ToString());
        }

        #endregion CompareTo

        #region ==

        /// <summary>
        /// Indicates wether the two Dwarfs are equal
        /// </summary>
        public static bool operator == (Dwarf<T> a, object b)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if (((object)a == null) || (b == null))
                return false;

            return a.Equals(b);
        }

        #endregion ==

        #region !=

        /// <summary>
        /// Indicates wether the two Dwarfs are not equal
        /// </summary>
        public static bool operator != (Dwarf<T> a, object b)
        {
            return !(a == b);
        }

        #endregion !=

        #region OnBeforeSave

        /// <summary>
        /// Prepend the Save method with additional modifications
        /// </summary>
        protected internal virtual void OnBeforeSave()
        {

        }

        #endregion OnBeforeSave

        #region OnAfterSave

        /// <summary>
        /// Append the Save method with additional modifications
        /// </summary>
        protected internal virtual void OnAfterSave()
        {
            
        }

        /// <summary>
        /// Append the Save method with additional modifications
        /// </summary>
        private void OnAfterSaveInternal()
        {
            foreach (var ep in DwarfHelper.GetDBProperties(GetType()))
                SetOriginalValue(ep.Name, ep.GetValue(this));

            foreach (var ep in DwarfHelper.GetGemListProperties(GetType()))
            {
                if (IsCollectionInitialized(ep.ContainedProperty))
                    SetOriginalValue(ep.Name, ep.GetValue(this));
            }

            foreach (var ep in DwarfHelper.GetManyToManyProperties(GetType()))
            {
                if (IsCollectionInitialized(ep.ContainedProperty))
                    SetOriginalValue(ep.Name, ep.GetValue(this));
            }
        }

        #endregion OnAfterSave

        #region OnBeforeDelete

        /// <summary>
        /// Prepend the delete method with additional modifications
        /// </summary>
        protected internal virtual void OnBeforeDelete()
        {
            
        }
        
        private void OnBeforeDeleteInternal()
        {
            //We want to persist all the Inverse OneToManyCollection upon deletion. Let's remove all lists that doesn't 
            //need persistance first (all objects therein will be deleted in the database via delete cascades anyways)
            foreach (var pi in DwarfHelper.GetOneToManyProperties(this))
            {
                var propertyAtt = OneToManyAttribute.GetAttribute(pi.ContainedProperty);

                if (propertyAtt == null)
                    throw new NullReferenceException(pi.Name + " is missing the OneToMany attribute...");

                if (!propertyAtt.IsInverse) 
                    continue;

                var obj = (IDwarfList)pi.GetValue(this);

                var owningProp = oneToManyAlternateKeys.ContainsKey(pi.Name) ? oneToManyAlternateKeys[pi.Name] : GetType().Name;

                obj.Cast<IDwarf>().ForEachX(x => PropertyHelper.SetValue(x, owningProp, null));
                obj.SaveAllInternal<T>();
            }

            if (DbContextHelper<T>.DbContext.IsAuditLoggingSuspended || DwarfContext<T>.GetConfiguration().AuditLogService == null)
                return;

            var traces = (from ep in DwarfHelper.GetDBProperties(GetType()).Where(x => !x.Name.Equals("Id"))
                          let oldValue = originalValues[ep.Name]
                          where oldValue != null && (oldValue is string ? !string.IsNullOrEmpty(oldValue.ToString()) : true)
                          select new AuditLogEventTrace { PropertyName = ep.Name, OriginalValue = oldValue }).ToArray();

            var collectionTraces = (from ep in DwarfHelper.GetGemListProperties(GetType())
                                    let oldValue = (IGemList)ep.GetValue(this)
                                    where oldValue != null && oldValue.Count > 0
                                    select new AuditLogEventTrace { PropertyName = ep.Name, OriginalValue = oldValue }).ToArray();

            var many2ManyTraces = (from ep in DwarfHelper.GetManyToManyProperties(GetType())
                                   let oldValue = ep.GetValue(this)
                                   where oldValue != null && ((IList)oldValue).Count > 0
                                   select new AuditLogEventTrace { PropertyName = ep.Name, OriginalValue = oldValue }).ToArray();

            DwarfContext<T>.GetConfiguration().AuditLogService.Logg(this, AuditLogTypes.Deleted, traces.Union(collectionTraces).Union(many2ManyTraces).ToArray());
        }

        #endregion OnBeforeDelete

        #region OnAfterDelete
        
        /// <summary>
        /// Finalizes the Delete method with additional modifications (occurs outside of the transaction)
        /// </summary>
        private void OnAfterDeleteInternal()
        {
            isDeleted = true;
        }

        /// <summary>
        /// Finalizes the Delete method with additional modifications (occurs outside of the transaction)
        /// </summary>
        protected virtual void OnAfterDelete()
        {
        }

        #endregion OnAfterDelete

        #region CreateAuditLog

        private void CreateAuditLog(AuditLogTypes auditLogType, IEnumerable<AuditLogEventTrace> propertyTraces)
        {
            if (DbContextHelper<T>.DbContext.IsAuditLoggingSuspended || DwarfContext<T>.GetConfiguration().AuditLogService == null)
                return;

            if (auditLogType == AuditLogTypes.Updated)
            {
                var traces = propertyTraces.Union(CreateTraceEventsForManyToMany()).ToArray();

                if (traces.Length > 0)
                    DwarfContext<T>.GetConfiguration().AuditLogService.Logg(this, auditLogType, traces);
            }
            else if (auditLogType == AuditLogTypes.Created)
                DwarfContext<T>.GetConfiguration().AuditLogService.Logg(this, auditLogType);
        }

        #endregion CreateAuditLog

        #region CreateTraceEventsForProperties

        /// <summary>
        /// Creates and returns an AuditLogEventTrace object for every modified property
        /// </summary>
        private AuditLogEventTrace[] CreateTraceEventsForProperties()
        {
            if (!IsSaved)
                return new AuditLogEventTrace[0];

            if (originalValues == null)
            {
                var fromDB = !typeof(T).Implements<ICompositeId>()
                    ? Load(Id.Value)
                    : Load(DwarfHelper.GetPKProperties<T>().Select(x => new WhereCondition<T> { ColumnPi = x.ContainedProperty, Value = x.GetValue(this) }).ToArray());

                if (fromDB == null)
                    return new AuditLogEventTrace[0];

                originalValues = fromDB.originalValues;
            }

            var dbProps = from ep in DwarfHelper.GetDBProperties(GetType())
                          let oldValue = originalValues[ep.Name]
                          let x = ep.GetValue(this)
                          let newValue = x is IDwarf ? ((IDwarf)ep.GetValue(this)).Id : ep.GetValue(this)
                          where (oldValue != null && !oldValue.Equals(newValue)) || (oldValue == null && newValue != null)
                          select new AuditLogEventTrace { PropertyName = ep.Name, OriginalValue = oldValue, NewValue = newValue };

            var collections = from ep in DwarfHelper.GetGemListProperties(GetType())
                              where IsCollectionInitialized(ep.ContainedProperty)
                              let x = (IGemList)ep.GetValue(this)
                              let newValue = x
                              let oldValue = x.Parse((string)originalValues[ep.Name] ?? string.Empty)
                              where (oldValue != null && !oldValue.ComparisonString.Equals(newValue.ComparisonString)) || (oldValue == null && newValue != null)
                              select new AuditLogEventTrace { PropertyName = ep.Name, OriginalValue = oldValue, NewValue = newValue };

            return dbProps.Concat(collections).ToArray();
        }

        #endregion CreateTraceEventsForProperties

        #region CreateTraceEventsForOneToMany

        /// <summary>
        /// Creates and returns an AuditLogEventTrace object for every modified OneToMany relationship
        /// </summary>
        private IEnumerable<AuditLogEventTrace> CreateTraceEventsForOneToMany()
        {
            if (!IsSaved)
                return new AuditLogEventTrace[0];

            return from ep in DwarfHelper.GetOneToManyProperties(this)
                   where IsCollectionInitialized(ep.ContainedProperty)
                   let list = ep.GetValue(this)
                   let adds = ((IList)ep.PropertyType.GetMethod("GetAddedItems").Invoke(list, null)).Cast<IDwarf>().ToList()
                   let dels = ((IList)ep.PropertyType.GetMethod("GetDeletedItems").Invoke(list, null)).Cast<IDwarf>().ToList()
                   where adds.Count > 0 || dels.Count > 0
                   select new AuditLogEventTrace { PropertyName = ep.Name, NewValue = list };
        }

        #endregion CreateTraceEventsForOneToMany

        #region CreateTraceEventsForManyToMany

        /// <summary>
        /// Creates and returns an AuditLogEventTrace object for every modified ManyToMany relationship
        /// </summary>
        private IEnumerable<AuditLogEventTrace> CreateTraceEventsForManyToMany()
        {
            if (!IsSaved)
                return new AuditLogEventTrace[0];

            return from ep in DwarfHelper.GetManyToManyProperties(this)
                   where IsCollectionInitialized(ep.ContainedProperty)
                   let list = ep.GetValue(this)
                   let adds = ((IList)ep.PropertyType.GetMethod("GetAddedItems").Invoke(list, null)).Cast<IDwarf>().ToList()
                   let dels = ((IList)ep.PropertyType.GetMethod("GetDeletedItems").Invoke(list, null)).Cast<IDwarf>().ToList()
                   where adds.Count > 0 || dels.Count > 0
                   let org = MergeLists(list, adds, dels)
                   select new AuditLogEventTrace { PropertyName = ep.Name, OriginalValue = org, NewValue = list };
        }

        #region MergeLists

        private static List<IDwarf> MergeLists(object o, IEnumerable<IDwarf> adds, IEnumerable<IDwarf> dels)
        {
            var list = ((IList)o).Cast<IDwarf>().ToList();

            foreach (var dwarf in adds)
                list.Remove(dwarf);

            list.AddRange(dels);

            return list;
        }

        #endregion MergeLists

        #endregion CreateTraceEventsForManyToMany

        #region Clone

        /// <summary>
        /// Creates a shallow copy of the current object
        /// </summary>
        public virtual object Clone()
        {
            var clone = (T)MemberwiseClone();

            if (clone.oneToManyAlternateKeys != null)
                clone.oneToManyAlternateKeys = new Dictionary<string, string>();

            clone.Id = internallyProvidedCustomId = null;
            clone.IsSaved = clone.isDeleted = false;

            return clone;
        }

        #endregion Clone

        #region PersistOneToManyCollections

        /// <summary>
        /// Automatically handles default persistance operations over DwarfLists for OneToManyRelationships.
        /// Should a manual persistance be used via PersistOneToMany, do so before the base call in AppendStore
        /// </summary>
        private void PersistOneToManyCollections()
        {
            foreach (var pi in DwarfHelper.GetOneToManyProperties(this))
            {
                if (!IsCollectionInitialized(pi.ContainedProperty))
                    continue;

                var propertyName = pi.Name;
                var propertyAtt = OneToManyAttribute.GetAttribute(pi.ContainedProperty);

                if (propertyAtt == null)
                    throw new NullReferenceException(propertyName + " is missing the OneToMany attribute...");

                var obj = (IDwarfList)PropertyHelper.GetValue(this, propertyName);
                
                var list = obj.Cast<IDwarf>().ToList();

                var owningProp = oneToManyAlternateKeys.ContainsKey(propertyName) ? oneToManyAlternateKeys[propertyName] : GetType().Name;
                list.ForEach(x => PropertyHelper.SetValue(x, owningProp, this));
                var deleteObjects = obj.GetDeletedItems();

                if (propertyAtt.IsInverse)
                {
                    deleteObjects.ForEach(x => PropertyHelper.SetValue(x, owningProp, null));
                    deleteObjects.SaveAllInternal<T>();
                }
                else
                    deleteObjects.DeleteAllInternal<T>();

                list.SaveAllInternal<T>();

                oneToManyAlternateKeys.Remove(propertyName);
            }
        }

        #endregion PersistOneToManyCollections

        #region FaultyForeignKeys

        /// <summary>
        /// Returns true if the object contains faulty Foreign Key values
        /// </summary>
        private static IEnumerable<string> FaultyForeignKeys(IDwarf obj)
        {
            foreach (var pi in Cfg.FKProperties[DwarfHelper.DeProxyfy(obj)])
            {
                var att = DwarfPropertyAttribute.GetAttribute(pi.ContainedProperty);

                if (!att.IsNullable && (pi.GetValue(obj) == null || !((IDwarf)pi.GetValue(obj)).IsSaved))
                    yield return pi.Name;
            }
        }

        #endregion FaultyForeignKeys

        #region PersistManyToManyCollections

        /// <summary>
        /// Automatically handles default persistance operations over DwarfLists for ManyToManyRelationships.
        /// Should a manual persistance be used via PersistManyToMany, do so before the base call in AppendStore
        /// </summary>
        private void PersistManyToManyCollections()
        {
            foreach (var pi in DwarfHelper.GetManyToManyProperties(this))
            {
                if (!IsCollectionInitialized(pi.ContainedProperty))
                    continue;

                var tableName = ManyToManyAttribute.GetTableName(GetType(), pi.ContainedProperty);
                
                var obj = (IDwarfList)pi.GetValue(this);

                foreach (var deletedItem in obj.GetDeletedItems())
                    DeleteManyToMany(this, deletedItem, tableName);

                foreach (var addedItem in obj.GetAddedItems())
                    PersistManyToMany(this, addedItem, tableName);
            }
        }

        #endregion PersistManyToManyCollections

        #region InitializeManyToMany

        /// <summary>
        /// Used to shorten the syntax for initializing ManyToMany lists
        /// </summary>
        /// <typeparam name="TY">The type that will populate the DwarfList</typeparam>
        /// <param name="owningProperty">The current property</param>
        private DwarfList<TY> InitializeManyToMany<TY>(Expression<Func<T, DwarfList<TY>>> owningProperty) where TY : Dwarf<TY>, new()
        {
            var tableName = ManyToManyAttribute.GetTableName(GetType(), ReflectionHelper.GetPropertyInfo(owningProperty));

            return new DwarfList<TY>(LoadManyToManyRelation<TY>(this, tableName));
        }

        #endregion InitializeManyToMany        
        
        #region InitializeOneToMany

        /// <summary>
        /// Used to shorten the syntax for initializing OneToMany lists with an alternate primary key
        /// </summary>
        private DwarfList<TY> InitializeOneToMany<TY>(Expression<Func<TY, object>> alternatePrimaryKey = null, Expression<Func<TY, object>> alternateReferencingColumn = null) where TY : Dwarf<TY>, new()
        {
            return new DwarfList<TY>(Dwarf<TY>.LoadReferencing<TY>(GenerateQueryBuilderForOneToMany(alternateReferencingColumn)), alternatePrimaryKey);
        }

        #endregion InitializeOneToMany

        #region SetOriginalValue

        /// <summary>
        /// Used when the object is retrieved from the database to keep track of its original values
        /// </summary>
        internal void SetOriginalValue(string property, object value)
        {
            if (originalValues == null)
                originalValues = new Dictionary<string, object>();

            if (value is IDwarf)
                originalValues[property] = ((IDwarf) value).Id;
            else if (value is IGemList)
                originalValues[property] = ((IEnumerable<IGem>)value).Flatten(x => "¶" + x.Id + "¶");
            else
                originalValues[property] = value;
        }

        #endregion SetOriginalValue

        #region GetOriginalValue

        /// <summary>
        /// Used internally to retrieve original values
        /// </summary>
        internal object GetOriginalValue(string key)
        {
            if (originalValues != null && originalValues.ContainsKey(key))
                return originalValues[key];

            return null;
        }        
        
        /// <summary>
        /// Used internally to retrieve original values
        /// </summary>
        internal protected TY GetOriginalValue<TY>(Expression<Func<T, TY>> property)
        {
            var key = ReflectionHelper.GetPropertyName(property);

            if (originalValues != null && originalValues.ContainsKey(key))
                return (TY)originalValues[key];

            return default(TY);
        }

        #endregion GetOriginalValue
     
        #region GetProperty

        /// <summary>
        /// Helper method for the proxy classes's MSIL generated foreign key methods
        /// </summary>
        protected TY GetProperty<TY>(string propertyName, ref TY backingField, ref bool isAccessed) where TY : Dwarf<TY>, new()
        {
            if (isAccessed)
                return backingField;

            var orgValue = GetOriginalValue(propertyName);

            if (orgValue != null)
                backingField = (TY)Cfg.LoadExpressions[DwarfHelper.DeProxyfy(typeof(TY))]((Guid)orgValue);

            isAccessed = true;

            return backingField;
        }

        #endregion GetProperty        
        
        #region SetProperty

        /// <summary>
        /// Helper method for the proxy classes's MSIL generated foreign key methods
        /// </summary>
        protected void SetProperty<TY>(string propertyName, ref bool isAccessed) where TY : Dwarf<TY>, new()
        {
            isAccessed = true;
        }

        #endregion SetProperty

        #region ResetFKProperties

        /// <summary>
        /// Helper method for the proxy classes's MSIL generated foreign key methods 
        /// to reset all FK fields
        /// Do not override even if tempted :-)
        /// </summary>
        internal protected virtual void ResetFKProperties()
        {

        }

        #endregion ResetFKProperties

        #region IsPropertyDirty

        /// <summary>
        /// Returns true if the supplied property is dirty
        /// </summary>
        public bool IsPropertyDirty(Expression<Func<T, object>> property)
        {
            var piName = ReflectionHelper.GetPropertyName(property);

            var oldValue = GetOriginalValue(piName);
            var newValue = PropertyHelper.GetValue(this, piName);
            
            if (newValue is IDwarf)
                newValue = ((IDwarf)newValue).Id;

            if ((oldValue != null && !oldValue.Equals(newValue)) || (oldValue == null && newValue != null))
                return true;

            return false;
        }

        #endregion IsPropertyDirty

        #endregion Methods
    }
}
