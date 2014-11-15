using System;

namespace Dwarf.Interfaces
{
    /// <summary>
    /// Base interface for all foreign dwarfs.
    /// </summary>
    public interface IForeignDwarf : IComparable
    {
        /// <summary>
        /// Returns the Id of the instance
        /// </summary>
        object Id { get; }
    }
}
