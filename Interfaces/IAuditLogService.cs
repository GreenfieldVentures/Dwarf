using System;
using System.Collections.Generic;
using Evergreen.Dwarf.DataAccess;

namespace Evergreen.Dwarf.Interfaces
{
    /// <summary>
    /// Interface for implementing the PersistanceFoundation's Audit log service
    /// </summary>
    public interface IAuditLogService
    {
        #region Logg

        /// <summary>
        /// Stores an AuditLog object for the ongoing transaction
        /// </summary>
        IAuditLog Logg(IDwarf obj, AuditLogTypes auditLogType);

        /// <summary>
        /// Stores an AuditLog object for the ongoing transaction with details about the change
        /// </summary>
        IAuditLog Logg(IDwarf obj, AuditLogTypes auditLogType, params AuditLogEventTrace[] auditLogEventTraces);

        #endregion Logg

        #region CreateInstance

        /// <summary>
        /// Returns a new instances of databacked IErrorLog
        /// </summary>
        IAuditLog CreateInstance();

        #endregion CreateInstance

        #region Load

        /// <summary>
        /// Helper for the AuditLog-implementation's Load Method
        /// </summary>
        IAuditLog Load(Guid id);

        #endregion Load

        #region LoadAll

        /// <summary>
        /// Helper for the AuditLog-implementation's LoadAll Method
        /// </summary>
        List<IAuditLog> LoadAll();

        #endregion LoadAll

        #region LoadAllReferencing

        /// <summary>
        /// Helper for the AuditLog-implementation's LoadAll Method
        /// </summary>
        List<IAuditLog> LoadAllReferencing(IDwarf obj);

        /// <summary>
        /// Helper for the AuditLog-implementation's LoadAll Method
        /// </summary>
        List<IAuditLog> LoadAllReferencing(Guid id);

        #endregion LoadAllReferencing
    }
}
