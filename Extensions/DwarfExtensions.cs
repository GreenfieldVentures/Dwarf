using System.Collections.Generic;
using Dwarf.Interfaces;

namespace Dwarf.Extensions
{
    /// <summary>
    /// Extension methods for various IDwarf types
    /// </summary>
    public static class DwarfExtensions
    {
        #region SaveX

        /// <summary>
        /// See IDwarf.Save()
        /// </summary>
        public static T SaveX<T>(this T obj) where T : IDwarf
        {
            obj.Save();
            return obj;
        }

        #endregion SaveX
    }
}
