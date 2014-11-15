using System;
using System.Collections.Generic;

namespace Dwarf.Interfaces
{
    /// <summary>
    /// Interface for implementing the PersistanceFoundation's error log service
    /// </summary>
    public interface IErrorLogService
    {
        #region Logg

        /// <summary>
        /// Extract the info from the supplied exception 
        /// and stores the exception as a new ErrorLog
        /// </summary>
        IErrorLog Logg(Exception exception, bool suppressMessage = false);

        #endregion Logg

        #region CreateInstance

        /// <summary>
        /// Returns a new instances of databacked IErrorLog
        /// </summary>
        IErrorLog CreateInstance();

        #endregion CreateInstance

        #region Load

        /// <summary>
        /// Helper for the ErrorLog-implementation's Load Method
        /// </summary>
        IErrorLog Load(Guid id);

        #endregion Load

        #region LoadAll

        /// <summary>
        /// Helper for the ErrorLog-implementation's LoadAll Method
        /// </summary>
        List<IErrorLog> LoadAll();

        #endregion LoadAll
    }
}
