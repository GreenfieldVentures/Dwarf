using System.Collections;

namespace Evergreen.Dwarf.Interfaces
{
    /// <summary>
    /// An interface for the DwarfList extension for gems
    /// </summary>
    public interface IGemList: IEnumerable
    {
        /// <summary>
        /// Parses the supplied value and returns a new IGemList
        /// </summary>
        IGemList Parse(string value);

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:Dwarf.IGemList"/>.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets a string of Ids for uniqueness comparison
        /// </summary>
        string ComparisonString { get; }
    }
}