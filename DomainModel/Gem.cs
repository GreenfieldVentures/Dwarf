using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Dwarf.Interfaces;

namespace Dwarf
{
    /// <summary>
    /// An object, referenced by a persisted Dwarf, that itself is persisted elsewhere.
    /// </summary>
    public abstract class Gem<T> : IGem where T : Gem<T>, new()
    {
        #region Variables

        internal static ConcurrentDictionary<Type, Gem<T>> loadObjects = new ConcurrentDictionary<Type, Gem<T>>();

        #endregion Variables

        #region Properties

        #region Id

        /// <summary>
        /// Returns the Id of the instance
        /// </summary>
        public virtual object Id { get; set; }

        #endregion Id

        #endregion Properties

        #region Methods

        #region Load

        /// <summary>
        /// Returns an object with the specified id
        /// </summary>
        public abstract T LoadImplementation(object id);

        /// <summary>
        /// Returns an object with the specified id
        /// </summary>
        public static T Load(object id)
        {
            var type = typeof (T);

            if (!loadObjects.ContainsKey(type))
                loadObjects[type] = new T();

            return loadObjects[type].LoadImplementation(id);
        }
        
        #endregion Load

        #region LoadAll

        /// <summary>
        /// Returns an object with the specified id
        /// </summary>
        public abstract List<T> LoadAllImplementation();

        /// <summary>
        /// Returns an object with the specified id
        /// </summary>
        public static List<T> LoadAll()
        {
            var type = typeof (T);

            if (!loadObjects.ContainsKey(type))
                loadObjects[type] = new T();

            return loadObjects[type].LoadAllImplementation();
        }
        
        #endregion Load

        #region Equals

        /// <summary>
        /// Dwarf comparison is done via the Id property
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Gem<T>)
            {
                if (GetType() != obj.GetType())
                    return false;

                return Id.Equals(((Gem<T>)obj).Id);
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
        /// See base
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
        public static bool operator ==(Gem<T> a, object b)
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
        public static bool operator !=(Gem<T> a, object b)
        {
            return !(a == b);
        }

        #endregion !=

        #endregion Methods
    }
}