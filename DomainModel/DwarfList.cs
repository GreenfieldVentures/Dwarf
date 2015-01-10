using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Dwarf.Interfaces;
using Dwarf.Utilities;

namespace Dwarf
{
    //TODO! Sortering fallerar....

    /// <summary>
    /// Generic IList extension for handling object relationships. If an alternatePrimary key isn't specified
    /// the default Equality comparer will be used
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
//    [DebuggerTypeProxy(typeof(Mscorlib_CollectionDebugView<>))]
//    [__DynamicallyInvokable]
//    [Serializable]
    public class DwarfList<T> : /*List<T>, */IList<T>, IDwarfList
    {
        internal protected Dictionary<object, T>.ValueCollection Items { get { return items.Values; } }
//            [NonSerialized]
//            private object _syncRoot;


        //IDE 
        //Nyckel/Index-lista Dic[Key, Index]
        //Lista för själva item
        //Insert

        #region Variables

        private readonly Dictionary<object, T> deletedItems = new Dictionary<object, T>();
        private readonly Dictionary<object, T> addedItems = new Dictionary<object, T>();
        private readonly Dictionary<object, T> items = new Dictionary<object, T>();
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
        public DwarfList(IEnumerable<T> list): this()
        {
            InitializeList(list);
        }

        /// <summary>
        /// Constructor that initializes the collection with a predefined List of Ts and uniqueColumns
        /// </summary>
        public DwarfList(IEnumerable<T> list, Expression<Func<T, object>> alternatePrimaryKey)
        {
            this.alternatePrimaryKey = alternatePrimaryKey;
            InitializeList(list);
        }

        #endregion Constructors

        #region Properties

        #region IsReadOnly

        /// <summary>
        /// Gets or Sets if the DwarfList should be in ReadOnly mode
        /// </summary>
        public bool IsReadOnly { get; set; }

        public bool IsFixedSize { get; private set; }

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
            var toRemove = items.Values.Where(x => match(x)).ToList();

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
        public void Add(T item)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("The DwarfList is in a ReadOnly state. The operation is not possible at this point.");
            
            var key = GetKey(item);

            if (items.ContainsKey(key))
                return;

            if (deletedItems.ContainsKey(key))
            {
                item = deletedItems[key];
                deletedItems.Remove(item);
            }
            else
                addedItems[key] = item;

            items[key] = item;
        }

        /// <summary>
        /// See base
        /// </summary>
        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
                Add(item);
        }

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// See base
        /// </summary>
        public void Clear()
        {
            if (IsReadOnly)
                throw new ReadOnlyException("The DwarfList is in a ReadOnly state. The operation is not possible at this point.");

            foreach (var kvp in items)
                deletedItems[kvp.Key] = kvp.Value;

            items.Clear();
            addedItems.Clear();
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// See base
        /// </summary>
        public bool Contains(T item)
        {
            return items.ContainsKey(GetKey(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// See base
        /// </summary>
        public bool Remove(T item)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("The DwarfList is in a ReadOnly state. The operation is not possible at this point.");

            var key = GetKey(item);

            if (!items.ContainsKey(key))
                return false;

            if (!deletedItems.ContainsKey(key))
                deletedItems[key] = item;

            addedItems.Remove(key);
            items.Remove(key);

            return true;
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// See base
        /// </summary>
        public void Insert(int index, T item)
        {
            throw new Exception("DwarList does not support inserting by index");
        }

        /// <summary>
        /// See base
        /// </summary>
        public void RemoveAt(int index)
        {
            throw new Exception("DwarList does not support removing by index");
        }

        object IList.this[int index]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public T this[int index]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }


//        public int Count
//        {
//            get { return Count; }
//        }

        int ICollection.Count
        {
            get { return items.Values.Count; }
        }

        public object SyncRoot{get { throw new NotImplementedException(); }}
//        {
//            get
//            {
//                return _syncRoot;
//            }
//        }

        public bool IsSynchronized { get { throw new NotImplementedException(); } }
        public int Count
        {
            get { return items.Values.Count; }
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

            var key = GetKey(item);

            if (!items.ContainsKey(key))
                return false;

            deletedItems.Remove(key);
            addedItems.Remove(key);
            items.Remove(key);

            return true;
        }

        #endregion RemoveWithoutTrace

        #region GetKey

        private object GetKey(T item)
        {
            if (alternatePrimaryKey == null)
                return item;

            return PropertyHelper.GetValue(item, ReflectionHelper.GetPropertyName(alternatePrimaryKey));
        }

        #endregion GetKey

        #region InitializeList

        /// <summary>
        /// Initializes the list with a collection without triggering status changes in the list (ie added / deleted values)
        /// </summary>
        protected void InitializeList(IEnumerable<T> collection)
        {
            foreach (var item in collection)
                items[GetKey(item)] = item;
        }

        #endregion InitializeList

        #region GetItem

        private T GetItem(Dictionary<object, T> dictionary, T item)
        {
            var key = GetKey(item);
            return dictionary.ContainsKey(key) ? items[key] : default(T);
        }

        /// <summary>
        /// Returns an item from the list with a matching key value to the supplied item
        /// </summary>
        public T GetItem(T item)
        {
            return GetItem(items, item);
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

        public IEnumerator<T> GetEnumerator()
        {
            return items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.Values.GetEnumerator();
        }
    }
}