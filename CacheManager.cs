using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using Dwarf.Extensions;
using Dwarf.Interfaces;

namespace Dwarf
{
    /// <summary>
    /// Simple helper class for handling the DAL Cache
    /// </summary>
    public class CacheManager
    {
        #region Properties

        #region Cache

        internal static Cache Cache
        {
            get { return HttpRuntime.Cache; }
        }

        #endregion Cache

        #endregion Properties

        #region Methods

        #region AggregateCacheDependencies

        /// <summary>
        /// Aggregates dependancies into one CacheDependancy
        /// </summary>
        private static AggregateCacheDependency AggregateCacheDependencies<T>()
        {
            var name = typeof (T).Name;

            if (!ContainsKey(GetUserKey(name)))
                Cache.Insert(GetUserKey(name), new object(), new CacheDependency(null, new[] { GetUserCacheRegion() }));

            var acd = new AggregateCacheDependency();

            acd.Add(new CacheDependency(null, new[] { GetUserKey(name) }));
            acd.Add(new CacheDependency(null, new[] { GetUserCacheRegion() }));

            return acd;
        }

        #endregion AggregateCacheDependencies

        #region ClearCacheForType

        /// <summary>
        /// Removes all cached objects and lists of the specified type. See class comment.
        /// </summary>
        internal static void ClearCacheForType(Type t)
        {
            if (Cache == null)
                return;

            Cache.Insert(GetUserKey(t.Name), new object());
        }

        #endregion ClearCacheForType

        #region Insert

        /// <summary>
        /// Inserts an item into the Cache
        /// </summary>
        private static object Insert<T>(string key, object value, CacheDependency dependencies)
        {
            var item = Cache[key];

            if (item is NullObject)
                return value;
            
            if (item != null)
                return Cache[key];

            Cache.Insert(key, value ?? new NullObject(), dependencies, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration);

            return value;
        }

        #endregion Insert

        #region SetCache

        /// <summary>
        /// Cache the supplied Dwarf
        /// </summary>
        internal static T SetCache<T>(Guid key, T value) where T : Dwarf<T>, new()
        {
            return SetCache(key.ToString(), value);
        }

        /// <summary>
        /// Cache the supplied Dwarf
        /// </summary>
        internal static T SetCache<T>(string key, T value) where T : Dwarf<T>, new()
        {
            if (Cache == null)
                return value;

            return (T)Insert<T>(GetUserKey(key), value, AggregateCacheDependencies<T>());
        }

        #endregion SetCache

        #region TryGetCache

        internal static bool TryGetCache<T>(Guid id, out T result) where T : Dwarf<T>, new()
        {
            return TryGetCache(id.ToString(), out result);
        }

        internal static bool TryGetCache<T>(string key, out T result) where T : Dwarf<T>, new()
        {
            result = null;

            if (Cache == null)
                return false;

            var c = Cache[GetUserKey(key)];

            if (c == null || c is NullObject)
                return false;
            
            result = (T)c;
            return true;
        }

        #endregion TryGetCache

        #region GetUserKey

        private static string GetUserKey(string key)
        {
            if (ContextAdapter.Items["UserKey"] == null)
                ContextAdapter.Items["UserKey"] = Guid.NewGuid().ToString();

            return ContextAdapter.Items["UserKey"] + ":" + key;
        }

        internal static string GetUserKey<T>(T obj, Guid? alternateId = null) where T : Dwarf<T>, new()
        {
            var objectKey = typeof(T).Implements<ICompositeId>()
                                   ? DwarfHelper.GetUniqueKeyForCompositeId(obj)
                                   : alternateId.HasValue ? alternateId.Value.ToString() : obj.Id.ToString();

            return GetUserKey(objectKey);
        }

        #endregion GetUserKey

        #region GetUserCacheRegion

        /// <summary>
        /// Returns the name if the user cache region and inserts it into the cache if needed
        /// </summary>
        private static string GetUserCacheRegion()
        {
            var key = GetUserKey("UserCacheRegion");

            if (key != null && Cache != null && !ContainsKey(key))
                Cache.Insert(key, new object());

            return key;
        }

        #endregion GetUserCacheRegion

        #region SetCacheList

        /// <summary>
        /// Cache the supplied list of Dwarfs
        /// </summary>
        internal static List<T> SetCacheList<T>(string key, List<T> list) where T : Dwarf<T>, new()
        {
            if (Cache == null)
                return list;

            var result = list.Select(x => (T)Insert<T>(GetUserKey(x), x, AggregateCacheDependencies<T>())).ToList();

            Insert<T>(GetUserKey(key), result, AggregateCacheDependencies<T>());

            return result;
        }

        /// <summary>
        /// Cache the supplied list of Dwarfs
        /// </summary>
        internal static ForeignDwarfList<T> SetCacheList<T>(string key, ForeignDwarfList<T> list) where T : ForeignDwarf<T>, new()
        {
            if (Cache == null)
                return list;

            Insert<T>(key, list, AggregateCacheDependencies<T>());

            return list;
        }

        #endregion SetCacheList

        #region GetCacheList

        internal static List<T> GetCacheList<T>(string queryKey) where T : Dwarf<T>, new()
        {
            if (Cache == null)
                return null;

            return Cache[GetUserKey(queryKey)] as List<T>;
        }

        #endregion GetCacheList

        #region ContainsKey

        /// <summary>
        /// Returns true if the specified key exists in the cache
        /// </summary>
        public static bool ContainsKey(string key)
        {
            if (Cache == null)
                return false;

            return Cache[key] != null;
        }

        #endregion ContainsKey

        #region RemoveKey

        /// <summary>
        /// Removes the current key from the cache if exists
        /// </summary>
        public static void RemoveKey(string key)
        {
            if (Cache != null && Cache[key] != null)
                Cache.Remove(key);
        }

        #endregion ContainsKey

        #region Clear

        /// <summary>
        /// Clears the Cache
        /// </summary>
        public static void Clear()
        {
            foreach (DictionaryEntry kvp in Cache)
                Cache.Remove(kvp.Key.ToString());
        }

        #endregion Clear

        #region DisposeUserCache

        /// <summary>
        /// Removes the user cache region from the cache,
        /// thus invalidating all user specific instances.
        /// </summary>
        public static void DisposeUserCache()
        {
            var key = GetUserCacheRegion();

            if (key != null)
                RemoveKey(key);
        }

        #endregion DisposeUserCache

        #region GetCollectionCache

        internal static DwarfList<T> GetCollectionCache<T>(string key) where T : Dwarf<T>, new()
        {
            return Cache[key] as DwarfList<T>;
        }

        #endregion GetCollectionCache

        #region SetCollectionCache

        internal static void SetCollectionCache<T>(string key, DwarfList<T> list) where T : Dwarf<T>, new()
        {
            Insert<T>(key, list, AggregateCacheDependencies<T>());
        }

        #endregion SetCollectionCache

        #endregion Methods

        #region Class: NullObject

        private class NullObject
        {
        }

        #endregion Class: NullObject
    }
}
