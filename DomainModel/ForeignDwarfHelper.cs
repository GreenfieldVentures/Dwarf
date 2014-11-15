using System;
using System.Collections.Generic;

namespace Dwarf
{
    /// <summary>
    /// Helper class for foreign dwarfs
    /// </summary>
    public static class ForeignDwarfHelper
    {
        #region Methods

        #region Load

        /// <summary>
        /// Utilize the compiled Load expresseion (use instead of reflection wherever possible
        /// </summary>
        public static object Load(Type type, object id)
        {
            return Cfg.LoadForeignDwarf[type](id);
        }

        #endregion Load

        #endregion Methods
    }
}