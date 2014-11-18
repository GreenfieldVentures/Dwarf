using System;
using System.Collections.Generic;
using Dwarf.Attributes;
using Dwarf.Extensions;
using Dwarf.DataAccess;
using Dwarf.Interfaces;
using Dwarf.Utilities;

namespace Dwarf
{
    /// <summary>
    /// Dwarf specialization for tree structures
    /// </summary>
    public abstract class UnaryDwarf<T> : Dwarf<T> where T : UnaryDwarf<T>, new()
    {
        #region Properties

        #region Parent

        /// <summary>
        /// The Parent
        /// </summary>
        [DwarfProperty(IsNullable = true, DisableDeleteCascade = true)]
        public virtual T Parent { get; set; }

        #endregion Parent

        #region Children

        /// <summary>
        /// A collection of children
        /// </summary>
        [OneToMany]
        public DwarfList<T> Children
        {
            get { return OneToMany(x => x.Children, null, x => x.Parent ); }
        }

        #endregion Children

        #endregion Properties

        #region Methods

        #region OnBeforeDelete

        /// <summary>
        /// See base
        /// </summary>
        protected internal override void OnBeforeDelete()
        {
            base.OnBeforeDelete();

            //Manual deletion is necessary since sql server doesn't support unary delete cascades
            Children.DeleteAll();
        }

        #endregion OnBeforeDelete

        #region LoadAncestors

        /// <summary>
        /// Returns a collection of ancestors to the current object
        /// </summary>
        public List<T> LoadAncestors()
        {
            var items = new List<T>();

            var currentObj = Parent;

            while (currentObj != null)
            {
                items.Add(currentObj);

                currentObj = (currentObj).Parent;
            }

            items.Reverse();

            return items;
        }

        #endregion LoadAncestors

        #region LoadDescendants

        /// <summary>
        /// Returns a collection of descendants to the current object
        /// </summary>
        public List<T> LoadDescendants()
        {
            var descendants = new List<T>();

            DescendantHelper(descendants, this as T);

            return descendants;
        }

        private static void DescendantHelper(ICollection<T> descendants, T currentObject)
        {
            foreach (T child in currentObject.Children)
            {
                descendants.Add(child);
                DescendantHelper(descendants, child);
            }
        }

        #endregion LoadDescendants

        #region LoadRoots

        /// <summary>
        /// Returns a list of all root objects of the current type
        /// </summary>
        public static List<T> LoadRoots()
        {
            return LoadReferencing(new WhereCondition<T> { Column = x => x.Parent, Value = null });
        }

        #endregion LoadRoots

        #endregion Methods
    }
}
