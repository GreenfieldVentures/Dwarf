using System.Collections;

namespace Dwarf.Interfaces
{
    /// <summary>
    /// An interface for the DwarfList extension for foreign dwarfs
    /// </summary>
    public interface IForeignDwarfList: IEnumerable
    {
        /// <summary>
        /// Parses the supplied value and returns a new IForeignDwarfList
        /// </summary>
        IForeignDwarfList Parse(string value);

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:Dwarf.IForeignDwarfList"/>.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets a string of Ids for uniqueness comparison
        /// </summary>
        string ComparisonString { get; }
    }
}