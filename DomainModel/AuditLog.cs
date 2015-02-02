using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Evergreen.Dwarf.Attributes;
using Evergreen.Dwarf.Extensions;
using Evergreen.Dwarf.DataAccess;
using Evergreen.Dwarf.Interfaces;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf
{
    /// <summary>
    /// Represents an error (stored exception)
    /// </summary>
    public class AuditLog : Dwarf<AuditLog>, IAuditLog, IAuditLogless, ICacheless
    {
        #region Properties

        /// <summary>
        /// See IAuditLog
        /// </summary>
        [DwarfProperty]
        public string ClassType { get; set; }

        /// <summary>
        /// See IAuditLog
        /// </summary>
        [DwarfProperty(UseMaxLength = true)]
        public string ObjectValue { get; set; }
        
        /// <summary>
        /// See IAuditLog
        /// </summary>
        [DwarfProperty]
        public string ObjectId { get; set; }

        /// <summary>
        /// See IAuditLog
        /// </summary>
        [DwarfProperty]
        public AuditLogTypes AuditLogType { get; set; }

        /// <summary>
        /// See IAuditLog
        /// </summary>
        [DwarfProperty]
        public string UserName { get; set; }

        /// <summary>
        /// See IAuditLog
        /// </summary>
        [DwarfProperty(IsDefaultSort = true)]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// See IAuditLog
        /// </summary>
        [DwarfProperty(UseMaxLength = true)]
        public string AuditDetails { get; set; }

        #endregion Properties

        #region Logg

        /// <summary>
        /// Stores an AuditLog object for the ongoing transaction
        /// </summary>
        public static IAuditLog Logg<T>(IDwarf obj, AuditLogTypes auditLogType)
        {
            return Logg<T>(obj, auditLogType, new AuditLogEventTrace[] { });
        }

        /// <summary>
        /// Stores an AuditLog object for the ongoing transaction
        /// </summary>
        public static IAuditLog Logg<T>(IDwarf obj, AuditLogTypes auditLogType, params AuditLogEventTrace[] auditUpdateEvents)
        {
            var type = DwarfHelper.DeProxyfy(obj);

            if (type.Implements<IAuditLogless>() || type.Implements<IAuditLog>())
                return null;

            var al = new AuditLog
            {
                ClassType = type.Name,
                AuditLogType = auditLogType,
                UserName = DwarfContext<T>.GetConfiguration().UserService.CurrentUser != null ? DwarfContext<T>.GetConfiguration().UserService.CurrentUser.UserName : string.Empty,
                TimeStamp = DateTime.Now,
                ObjectValue = obj.ToString(),
            };

            if (!type.Implements<ICompositeId>())
                al.ObjectId = obj.Id.ToString();
            else
            {
                foreach (var ep in DwarfHelper.GetPKProperties<T>(type))
                    al.ObjectId += string.Format("[{0}: {1}]", ep.Name, ep.GetValue(obj));
            }

            if (auditLogType != AuditLogTypes.Created)
            {
                al.AuditDetails = "<?xml version=\"1.0\"?><Properties>";

                foreach (var auditUpdateEvent in auditUpdateEvents)
                    al.AuditDetails += auditUpdateEvent.ToXml().ToString();

                al.AuditDetails += "</Properties>";

            }

            DwarfContext<T>.GetDatabase().Insert<T, AuditLog>(al);

            return al;
        }

        #endregion Logg

        #region GetCreatedEvent

        /// <summary>
        /// Returns the "Created" AuditLog event for the supplied object
        /// </summary>
        public static AuditLog GetCreatedEvent<T>(T obj) where T : Dwarf<T>, new()
        {
            return DwarfContext<T>.GetDatabase().SelectReferencing<T, AuditLog>
            (
                new WhereCondition<AuditLog> { Column = x => x.ClassType, Value = obj.GetType().Name },
                new WhereCondition<AuditLog> { Column = x => x.AuditLogType, Value = AuditLogTypes.Created },
                new WhereCondition<AuditLog> { Column = x => x.ObjectId, Value = obj.Id }
            ).FirstOrDefault();
        }

        #endregion GetCreatedEvent    

        #region GetAllEvents

        /// <summary>
        /// Returns all AuditLog events for the supplied object
        /// </summary>
        public static List<AuditLog> GetAllEvents<T>(IDwarf obj)
        {
            return DwarfContext<T>.GetDatabase().SelectReferencing<T, AuditLog>
            (
                new WhereCondition<AuditLog> { Column = x => x.ClassType, Value = obj.GetType().Name },
                new WhereCondition<AuditLog> { Column = x => x.ObjectId, Value = obj.Id }
            );
        }

        /// <summary>
        /// Returns all AuditLog events for the supplied object
        /// </summary>
        public static List<AuditLog> GetAllEvents<T>(Guid id)
        {
            return DwarfContext<T>.GetDatabase().SelectReferencing<T, AuditLog>
            (
                new WhereCondition<AuditLog> { Column = x => x.ClassType, Value = typeof(T).Name },
                new WhereCondition<AuditLog> { Column = x => x.ObjectId, Value = id }
            );
        }

        #endregion GetAllEvents        
        
        #region Load

        /// <summary>
        /// See base
        /// </summary>
        public static AuditLog Load<T>(Guid id)
        {
            return DwarfContext<T>.GetDatabase().Select<T, AuditLog>(id);
        }

        /// <summary>
        /// See base
        /// </summary>
        public new static AuditLog Load(Guid id)
        {
            throw new InvalidOperationException("Use Load<T> for Dwarf's AuditLogs instead");
        }

        #endregion Load

        #region LoadAll

        /// <summary>
        /// See base
        /// </summary>
        public static List<AuditLog> LoadAll<T>()
        {
            return DwarfContext<T>.GetDatabase().SelectReferencing<T, AuditLog>();
        }

        /// <summary>
        /// See base
        /// </summary>
        public new static List<AuditLog> LoadAll()
        {
            throw new InvalidOperationException("Use LoadAll<T> for Dwarf's AuditLog instead");
        }

        #endregion LoadAll
    }

    #region AuditLogService

    /// <summary>
    /// Dwarf specific implementation of the AuditLog Service
    /// </summary>
    public class AuditLogService<T> : IAuditLogService
    {
        /// <summary>
        /// See base
        /// </summary>
        public IAuditLog Logg(IDwarf obj, AuditLogTypes auditLogType)
        {
            return AuditLog.Logg<T>(obj, auditLogType);
        }

        /// <summary>
        /// See base
        /// </summary>
        public IAuditLog Logg(IDwarf obj, AuditLogTypes auditLogType, params AuditLogEventTrace[] auditLogEventTraces)
        {
            return AuditLog.Logg<T>(obj, auditLogType, auditLogEventTraces);
        }

        /// <summary>
        /// See base
        /// </summary>
        public IAuditLog CreateInstance()
        {
            return new AuditLog();
        }

        /// <summary>
        /// See base
        /// </summary>
        public IAuditLog Load(Guid id)
        {
            return AuditLog.Load<T>(id);
        }

        /// <summary>
        /// See base
        /// </summary>
        public List<IAuditLog> LoadAll()
        {
            return AuditLog.LoadAll<T>().Cast<IAuditLog>().ToList();
        }

        /// <summary>
        /// See base
        /// </summary>
        public List<IAuditLog> LoadAllReferencing(IDwarf obj)
        {
            return AuditLog.GetAllEvents<T>(obj).Cast<IAuditLog>().ToList();
        }        
        
        /// <summary>
        /// See base
        /// </summary>
        public List<IAuditLog> LoadAllReferencing(Guid id)
        {
            return AuditLog.GetAllEvents<T>(id).Cast<IAuditLog>().ToList();
        }   
    }

    #endregion AuditLogService
}
