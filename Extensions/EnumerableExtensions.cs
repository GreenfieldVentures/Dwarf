using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dwarf.Extensions
{
    /// <summary>
    /// Holds extension methods for Enumerables
    /// </summary>
    public static class EnumerableExtensions
    {
        #region IsNullOrEmpty

        /// <summary>
        /// Returns true if the supplied collection is either null or contains no items
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return true;

            return !enumerable.Any();
        }

        #endregion IsNullOrEmpty

        #region Split

        /// <summary> 
        /// Split a list of items into chunks of a specific size 
        /// </summary> 
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }

        #endregion Split

        #region ForEachX

        /// <summary>
        /// Performs the specified action on each element of the collection.
        /// </summary>
        public static List<T> ForEachX<T>(this List<T> list, Action<T> action)
        {
            list.ForEach(action);
            return list;
        }           
        
        /// <summary>
        /// Performs the specified action on each element of the collection.
        /// </summary>
        public static IEnumerable<T> ForEachX<T>(this IEnumerable<T> list, Action<T> action)
        {
            var result = list.ToList();
            result.ForEach(action);

            return result;
        }

        #endregion ForEachX

        #region MoveFirst

        /// <summary>
        /// Moves the item first in the array
        /// </summary>
        public static IList<T> MoveFirst<T>(this IList<T> list, T item)
        {
            return list.Move(item, 0);
        }

        #endregion MoveFirst

        #region MoveLast

        /// <summary>
        /// Moves the item last in the list
        /// </summary>
        public static IList<T> MoveLast<T>(this IList<T> list, T item)
        {
            return list.Move(item, list.Count - 1);
        }

        #endregion MoveLast

        #region Move

        /// <summary>
        /// Moves the item to the specied index of the list
        /// </summary>
        public static IList<T> Move<T>(this IList<T> list, T item, int index)
        {
            list.Remove(item);
            list.Insert(index, item);

            return list;
        }

        #endregion Move

        #region AddX

        /// <summary>
        /// See ICollection.Add()
        /// </summary>
        public static T AddX<T>(this IList<T> list, T obj) where T : class
        {
            list.Add(obj);
            return obj;
        }

        #endregion AddX

        #region RemoveX

        /// <summary>
        /// See ICollection.Remove()
        /// </summary>
        public static T RemoveX<T>(this IList<T> list, T obj) where T : class
        {
            list.Remove(obj);
            return obj;
        }

        #endregion RemoveX

        #region AddRangeX

        /// <summary>
        /// See ICollection.Add()
        /// </summary>
        public static List<T> AddRangeX<T>(this List<T> list, IEnumerable<T> newList) where T : class
        {
            list.AddRange(newList);
            return list;
        }

        #endregion AddRangeX
    }
}
