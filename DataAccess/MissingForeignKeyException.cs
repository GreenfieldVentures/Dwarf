using System;

namespace Dwarf.DataAccess
{
    /// <summary>
    /// Is thrown whenever an foreign key is still missing upon transcation commit
    /// </summary>
    public class MissingForeignKeyException: ApplicationException
    {
        /// <summary>
        /// Empty Constructor
        /// </summary>
        public MissingForeignKeyException() {}

        /// <summary>
        /// Default Constructor
        /// </summary>
        public MissingForeignKeyException(string message, Exception innerException) : base(message, innerException) { }
    }
}
