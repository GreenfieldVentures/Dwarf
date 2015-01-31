using System;
using Evergreen.Dwarf.Interfaces;

namespace Evergreen.Dwarf.Interfaces
{
    /// <summary>
    /// Interface for defining an error log entry
    /// </summary>
    public interface IErrorLog : IDwarf
    {
        /// <summary>
        /// Gets or sets ex.Message
        /// </summary>
        string Message { get; set; }
        
        /// <summary>
        /// Gets or sets ex.StackTrace
        /// </summary>
        string StackTrace { get; set; }

        /// <summary>
        /// Gets or sets ex.GetType()
        /// </summary>
        string Type { get; set; }
        
        /// <summary>
        /// Gets or sets ex.Exception
        /// </summary>
        Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets ex.StackTrace
        /// </summary>
//        IErrorLog InnerException { get; set; }
//
//        /// <summary>
//        /// Gets or sets ex.InnerMessage
//        /// </summary>
//        string InnerMessage { get; set; }
//
//        /// <summary>
//        /// Gets or sets ex.InnerException.Tostring()
//        /// </summary>
//        string InnerException { get; set; }
//
//        /// <summary>
//        /// Gets or sets ex.InnerException.StackTrace
//        /// </summary>
//        string InnerStackTrace { get; set; }

        /// <summary>
        /// Gets or sets the name of the currently logged in user
        /// </summary>
        string UserName { get; set; }

        /// <summary>
        /// Gets or sets the time of the incident
        /// </summary>
        DateTime TimeStamp { get; set; }
    }
}
