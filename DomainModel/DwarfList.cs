using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Dwarf.Interfaces;
using Dwarf.Utilities;

namespace Dwarf
{
    /// <summary>
    /// Generic IList extension for handling object relationships. If an alternatePrimary key isn't specified
    /// the default Equality comparer will be used
    /// </summary>
    public class DwarfList<T> : List<T>, IList<T>, IDwarfList
    {
        #region Variables

        private readonly List<T> deletedItems = new List<T>();
        private readonly List<T> addedItems = new List<T>();
        private readonly Expression<Func<T, object>> alternatePrimaryKey;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DwarfList() { }

        /// <summary>
        /// Construtor with unique columns
        /// </summary>
        public DwarfList(Expression<Func<T, object>> alternatePrimaryKey)
            : this()
        {
            this.alternatePrimaryKey = alternatePrimaryKey;
        }

        /// <summary>
        /// Constructor that initializes the collection with a predefined List of Ts
        /// </summary>
        public DwarfList(IEnumerable<T> list)
            : this()
        {
            InitializeList(list);
        }

        /// <summary>
        /// Constructor that initializes the collection with a predefined List of Ts and uniqueColumns
        /// </summary>
        public DwarfList(IEnumerable<T> list, Expression<Func<T, object>> alternatePrimaryKey)
            : this(list)
        {
            this.alternatePrimaryKey = alternatePrimaryKey;
        }

        #endregion Constructors

        #region Properties

        #region IsReadOnly

        /// <summary>
        /// Gets or Sets if the DwarfList should be in ReadOnly mode
        /// </summary>
        public bool IsReadOnly { get; set; }

        #endregion IsReadOnly

        #endregion Properties

        #region Methods

        #region RemoveAll

        /// <summary>
        /// Removes all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <returns>
        /// The number of elements removed from the <see cref="T:Dwarf.Foundation.DwarfList`1"/> .
        /// </returns>
        /// <param name="match">The <see cref="T:System.Predicate`1"/> delegate that defines the conditions of the elements to remove.</param><exception cref="T:System.ArgumentNullException"><paramref name="match"/> is null.</exception>
        public new int RemoveAll(Predicate<T> match)
        {
            var toRemove = this.Where(x => match(x)).ToList();

            foreach (var item in toRemove)
                Remove(item);

            return toRemove.Count();
        }

        #endregion RemoveAll

        #region RemoveRange

        /// <summary>
        /// Removes the supplied items from the collection
        /// </summary>
        public void RemoveRange(IEnumerable<T> list)
        {
            foreach (var item in list)
                Remove(item);
        }

        #endregion RemoveRange

        #region IList Members

        /// <summary>
        /// See base
        /// </summary>
        public new void Add(T item)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("The DwarfList is in a ReadOnly state. The operation is not possible at this point.");

            if (Contains(this, item))
                return;

            if (Contains(deletedItems, item))
            {
                item = GetItem(deletedItems, item);
                deletedItems.Remove(item);
            }
            else
                addedItems.Add(item);

            base.Add(item);
        }

        /// <summary>
        /// See base
        /// </summary>
        public new void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
                Add(item);
        }

        /// <summary>
        /// See base
        /// </summary>
        public new void Clear()
        {
            if (IsReadOnly)
                throw new ReadOnlyException("The DwarfList is in a ReadOnly state. The operation is not possible at this point.");

            foreach (var item in this)
            {
                if (!Contains(deletedItems, item))
                    deletedItems.Add(item);
            }

            base.Clear();
            addedItems.Clear();
        }

        /// <summary>
        /// See base
        /// </summary>
        public new bool Contains(T item)
        {
            return Contains(this, item);
        }

        /// <summary>
        /// See base
        /// </summary>
        protected bool Contains(IEnumerable<T> list, T item)
        {
            if (!typeof(T).IsGenericType && typeof(T).IsValueType) 
                return !GetItem(list, item).Equals(default(T));

            return GetItem(list, item) != null;
        }

        /// <summary>
        /// See base
        /// </summary>
        public new bool Remove(T item)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("The DwarfList is in a ReadOnly state. The operation is not possible at this point.");

            if (!Contains(this, item))
                return false;

            if (!Contains(deletedItems, item))
                deletedItems.Add(item);

            addedItems.Remove(item);
            base.Remove(item);

            return true;
        }

        /// <summary>
        /// See base
        /// </summary>
        public new void Insert(int index, T item)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("The DwarfList is in a ReadOnly state. The operation is not possible at this point.");

            if (Contains(this, item))
                return;

            base.Insert(index, item);
            addedItems.Add(item);
            deletedItems.Remove(item);
        }

        /// <summary>
        /// See base
        /// </summary>
        public new void RemoveAt(int index)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("The DwarfList is in a ReadOnly state. The operation is not possible at this point.");

            var item = this[index];

            deletedItems.Add(item);
            addedItems.Remove(item);
            base.RemoveAt(index);
        }

        #endregion IList Members

        #region ClearAddedItems

        /// <summary>
        /// Clears the added item list
        /// </summary>
        protected void ClearAddedItems()
        {
            addedItems.Clear();
        }

        #endregion ClearAddedItems

        #region ClearDeletedItems

        /// <summary>
        /// Clears the deleted item list
        /// </summary>
        protected void ClearDeletedItems()
        {
            deletedItems.Clear();
        }

        #endregion ClearDeletedItems

        #region GetAddedItems

        /// <summary>
        /// Returns a list of all added items
        /// </summary>
        public List<IDwarf> GetAddedItems()
        {
            return addedItems.Cast<IDwarf>().ToList();
        }

        #endregion GetAddedItems

        #region GetDeletedItems

        /// <summary>
        /// Returns a list of all removed items
        /// </summary>
        public List<IDwarf> GetDeletedItems()
        {
            return deletedItems.Cast<IDwarf>().ToList();
        }

        #endregion GetDeletedItems

        #region RemoveWithoutTrace

        /// <summary>
        /// Removes the supplied item from all lists (current, added and deleted)
        /// </summary>
        protected bool RemoveWithoutTrace(T item)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("The DwarfList is in a ReadOnly state. The operation is not possible at this point.");

            if (!Contains(this, item))
                return false;

            deletedItems.Remove(item);
            addedItems.Remove(item);
            base.Remove(item);

            return true;
        }

        #endregion RemoveWithoutTrace

        #region InitializeList

        /// <summary>
        /// Initializes the list with a collection without triggering status changes in the list (ie added / deleted values)
        /// </summary>
        protected void InitializeList(IEnumerable<T> collection)
        {
            base.AddRange(collection);
        }

        #endregion InitializeList

        #region AddIntersection

        /// <summary>
        /// Works just like AddRange, but dwarfs that already resides in the list who's 
        /// primary keys aren't present in the supplied collection will be removed.
        /// </summary>
        public void AddIntersection(IEnumerable<T> collection)
        {
            var col = collection.ToList();

            foreach (var item in ToArray())
            {
                if (alternatePrimaryKey == null)
                {
                    if (!col.Any(x => x.Equals(item)))
                        Remove(item);
                }
                else
                {
                    if (!col.Any(x => PropertyHelper.GetValue(x, ReflectionHelper.GetPropertyName(alternatePrimaryKey)).Equals(PropertyHelper.GetValue(item, ReflectionHelper.GetPropertyName(alternatePrimaryKey)))))
                        Remove(item);
                }
            }

            foreach (var item in col)
            {
                if (Contains(item))
                    continue;

                base.Add(item);
                addedItems.Add(item);
            }
        }

        #endregion AddIntersection

        #region GetItem

        /// <summary>
        /// If the supplied item (or an equal one) is contained in the supplied list, list's item will be returned
        /// </summary>
        private T GetItem(IEnumerable<T> list, T item)
        {
            if (alternatePrimaryKey == null)
                return list.FirstOrDefault(x => x.Equals(item));

            var propName = ReflectionHelper.GetPropertyName(alternatePrimaryKey);

            return list.FirstOrDefault(x => PropertyHelper.GetValue(x, propName).Equals(PropertyHelper.GetValue(item, propName)));
        }

        /// <summary>
        /// Returns an item from the list with a matching key value to the supplied item
        /// </summary>
        public T GetItem(T item)
        {
            return GetItem(this, item);
        }

        #endregion GetItem

        #region Cast

        /// <summary>
        /// Helper method to dynamically cast objects when invoking stuff via Delegates gives you a hard time
        /// </summary>
        public T Cast(object obj)
        {
            if (obj is T)
                return (T) obj;
            
            throw new InvalidCastException(obj.GetType() + " isn't castable to " + typeof(T));
        }

        #endregion Cast

        #endregion Methods
    }
}