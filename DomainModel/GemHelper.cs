using System;
using System.Collections.Generic;

namespace Dwarf
{
    /// <summary>
    /// Helper class for gems
    /// </summary>
    public static class GemList
    {
        #region Methods

        #region Load

        /// <summary>
        /// Utilize the compiled Load expresseion (use instead of reflection wherever possible
        /// </summary>
        public static object Load(Type type, object id)
        {
            return Cfg.LoadGem[type](id);
        }

        #endregion Load

        #endregion Methods
    }
}