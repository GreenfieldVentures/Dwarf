using System;

namespace Evergreen.Dwarf.DataAccess
{
    /// <summary>
    /// Is thrown whenever an exception is raised during a persistance call
    /// </summary>
    public class DatabaseOperationException: ApplicationException
    {
        /// <summary>
        /// Empty Constructor
        /// </summary>
        public DatabaseOperationException() {}

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DatabaseOperationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
