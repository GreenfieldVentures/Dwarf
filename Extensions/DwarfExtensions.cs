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
        
        #region CloneX

        /// <summary>
        /// See IDwarf.Clone()
        /// </summary>
        public static T CloneX<T>(this T obj) where T : IDwarf
        {
            return (T)obj.Clone();
        }

        #endregion CloneX
    }
}
