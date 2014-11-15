using System;
using System.Collections.Generic;
using System.Linq;
using Dwarf.Interfaces;

namespace Dwarf.Utilities
{
    /// <summary>
    /// Helper class logging errors
    /// </summary>
    public static class ExceptionHandler
    {
        #region Variables

        [ThreadStatic] 
        private static Dictionary<Exception, IErrorLog> exceptions;

        #endregion Variables

        #region Methods

        #region Logg

        /// <summary>
        /// Registers the supplied error to the collection
        /// </summary>
        public static void Logg(IErrorLog e)
        {
            if (exceptions == null)
                exceptions = new Dictionary<Exception, IErrorLog>();

            exceptions.Add(e.Exception, e);
        }

        #endregion Logg

        #region RetrieveErrors

        /// <summary>
        /// Returns a list of all caugth exceptions
        /// </summary>
        public static IEnumerable<IErrorLog> RetrieveErrors()
        {
            if (exceptions == null)
                return null;

            try
            {
                return exceptions.Values.ToArray();
            }
            finally
            {
                exceptions.Clear();
            }
        }

        #endregion RetrieveErrors

        #region ClearErrors

        /// <summary>
        /// Clears all current errors
        /// </summary>
        public static void ClearErrors()
        {
            if (exceptions != null)
                exceptions.Clear();
        }

        #endregion ClearErrors

        #region HasCaught

        public static bool HasCaught(Exception exception)
        {
            if (exceptions == null)
                return false;

            return exceptions.ContainsKey(exception);
        }

        #endregion HasCaught

        #region GetErrorLog

        public static IErrorLog GetErrorLog(Exception exception)
        {
            return exceptions[exception];
        }

        #endregion GetErrorLog

        #endregion Methods
    }
}