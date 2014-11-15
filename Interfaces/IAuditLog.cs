using System;
using Dwarf.DataAccess;
using Dwarf.Interfaces;

namespace Dwarf.Interfaces
{
    /// <summary>
    /// Interface for defining an audit log entry
    /// </summary>
    public interface IAuditLog : IDwarf
    {
        /// <summary>
        /// Gets or Sets the name of the type
        /// </summary>
        string ClassType { get; set; }

        /// <summary>
        /// Gets or Sets the current value of the object (f.g. the object's ToString() value)
        /// </summary>
        string ObjectValue { get; set; }

        /// <summary>
        /// Gets or Sets the Id of the object
        /// </summary>
        string ObjectId { get; set; }

        /// <summary>
        /// Gets or Sets the auditLogType
        /// </summary>
        AuditLogTypes AuditLogType { get; set; }

        /// <summary>
        /// Gets or Sets the the name of the user responsible for the change
        /// </summary>
        string UserName { get; set; }

        /// <summary>
        /// Gets or Sets the time of the event
        /// </summary>
        DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or Sets detailed information about the event (affected properties, before/after values, etc)
        /// </summary>
        string AuditDetails { get; set; }
    }
}
