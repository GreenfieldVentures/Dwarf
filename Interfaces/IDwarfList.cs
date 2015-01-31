using System;
using System.Collections;
using System.Collections.Generic;

namespace Evergreen.Dwarf.Interfaces
{
    /// <summary>
    /// Empty, non-generic, interface for DwarfLists
    /// </summary>
    public interface IDwarfList : IList
    {
        List<IDwarf> GetAddedItems();
        List<IDwarf> GetDeletedItems();
    }
}