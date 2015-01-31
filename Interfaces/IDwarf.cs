using System;

namespace Evergreen.Dwarf.Interfaces
{
    /// <summary>
    /// Base interface for all persistance objects.
    /// </summary>
    public interface IDwarf : ICloneable, IComparable
    {
        /// <summary>
        /// Id - Auto Persistent
        /// </summary>
        Guid? Id { get; set; }

        /// <summary>
        /// Returns true if the item is stored in the database (has a generated Id)
        /// </summary>
        bool IsSaved { get; }

        /// <summary>
        /// Returns true if the item is stored in the database and has one or many dirty (non-stored) properties
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Saves or updates the current object
        /// </summary>
        void Save();

        /// <summary>
        /// Deletes the current object
        /// </summary>
        void Delete();

        /// <summary>
        /// Refreshes the object with values from the underlying database
        /// </summary>
        void Refresh();

        /// <summary>
        /// Resets the object's proporties to the their original values
        /// </summary>
        void Reset();
    }
}