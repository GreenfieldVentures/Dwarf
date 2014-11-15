using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dwarf.Interfaces;

namespace Dwarf.Extensions
{
    /// <summary>
    /// Holds extension methods for collections of objects derived from IDwarf 
    /// </summary>
    public static class ListExtensions
    {
        #region SaveAll

        /// <summary>
        /// Calls SaveInternal() for all objects in the collection
        /// </summary>
        public static List<T> SaveAll<T>(this List<T> list) where T : IDwarf
        {
            SaveAllInternal<T>(list);

            return list;
        }

        internal static void SaveAllInternal<T>(this IEnumerable list)
        {
            try
            {
                DbContextHelper<T>.BeginOperation();

                foreach (var t1 in list)
                    ((IDwarf)t1).Save();

                DbContextHelper<T>.FinalizeOperation(false);
            }
            catch (Exception e)
            {
                DbContextHelper<T>.FinalizeOperation(true);
                ContextAdapter<T>.GetConfiguration().ErrorLogService.Logg(e);
                throw;
            }
            finally
            {
                DbContextHelper<T>.EndOperation();
            }
        }

        #endregion SaveAll

        #region DeleteAll

        /// <summary>
        /// Deletes all objects in the collection
        /// </summary>
        public static void DeleteAll<T>(this List<T> list) where T : IDwarf
        {
            DeleteAllInternal<T>(list);
        }

        internal static void DeleteAllInternal<T>(this IEnumerable list)
        {
            try
            {
                DbContextHelper<T>.BeginOperation();

                foreach (var t1 in list)
                    ((IDwarf)t1).Delete();

                DbContextHelper<T>.FinalizeOperation(false);
            }
            catch (Exception e)
            {
                DbContextHelper<T>.FinalizeOperation(true);
                ContextAdapter<T>.GetConfiguration().ErrorLogService.Logg(e);
                throw;
            }
            finally
            {
                DbContextHelper<T>.EndOperation();
            }
        }

        #endregion DeleteAll

        #region OrderByCreationTimeStamp

        /// <summary>
        /// Orders the collection by the objects creation timestamps
        /// </summary>
        public static IEnumerable<T> OrderByCreationTimeStamp<T>(this IEnumerable<T> list) where T : Dwarf<T>, new()
        {
            return list.OrderBy(x => AuditLog.GetCreatedEvent(x).TimeStamp);
        }

        #endregion OrderByCreationTimeStamp

        #region Clone

        /// <summary>
        /// Returns a new list with clones of all content
        /// </summary>
        public static List<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        /// <summary>
        /// Returns a new list with clones of all content
        /// </summary>
        public static Dictionary<T, K> Clone<T, K>(this Dictionary<T, K> dictionaryToClone) where K : ICloneable
        {
            var newDict = new Dictionary<T, K>();

            foreach (var kvp in dictionaryToClone)
                newDict[kvp.Key] = (K)kvp.Value.Clone();

            return newDict;
        }

        #endregion Clone

        #region FindById

        /// <summary>
        /// Locates and returns instance within the collection matching the specified Id.
        /// Null is returned if no match is found.
        /// </summary>
        public static T FindById<T>(this IEnumerable<T> list, string id) where T : IDwarf
        {
            if (string.IsNullOrEmpty(id))
                return default(T);

            Guid guid;

            if (Guid.TryParse(id, out guid))
                return list.FindById(guid);

            return default(T);
        }

        /// <summary>
        /// Locates and returns instance within the collection matching the specified Id.
        /// Null is returned if no match is found.
        /// </summary>
        public static T FindById<T>(this IEnumerable<T> list, Guid? id) where T : IDwarf
        {
            return list.FirstOrDefault(x => x.Id == id);
        }

        #endregion FindById
    }
}
