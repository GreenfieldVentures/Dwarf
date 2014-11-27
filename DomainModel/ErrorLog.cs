using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dwarf;
using Dwarf.Attributes;
using Dwarf.DataAccess;
using Dwarf.Interfaces;
using Dwarf.Utilities;

namespace Dwarf
{
    /// <summary>
    /// Represents an error (stored exception)
    /// </summary>
    public class ErrorLog : Dwarf<ErrorLog>, IErrorLog, IAuditLogless, ITransactionless
    {
        #region Properties

        #region Message

        /// <summary>
        /// Exception Message
        /// </summary>
        [DwarfProperty(UseMaxLength = true)]
        public string Message { get; set; }

        #endregion Message

        #region StackTrace

        /// <summary>
        /// Exception Stack Trace
        /// </summary>
        [DwarfProperty(UseMaxLength = true)]
        public string StackTrace { get; set; }

        #endregion StackTrace

        #region InnerException

        [DwarfProperty(IsNullable = true, DisableDeleteCascade = true)]
        public virtual ErrorLog InnerException { get; set; }

        #endregion InnerException

        #region Type

        /// <summary>
        /// Exception (GetType())
        /// </summary>
        [DwarfProperty]
        public string Type { get; set; }

        #endregion Type

        #region Exception

        public Exception Exception { get; set; }

        #endregion Exception

        #region UserName

        /// <summary>
        /// Afflicted User
        /// </summary>
        [DwarfProperty]
        public string UserName { get; set; }

        #endregion UserName

        #region TimeStamp

        /// <summary>
        /// Date of incident
        /// </summary>
        [DwarfProperty(IsDefaultSort = true)]
        public DateTime TimeStamp { get; set; }

        #endregion TimeStamp

        #endregion Properties

        #region Methods

        #region Logg

        /// <summary>
        /// Extract the information from the supplied exception 
        /// and stores the information as a new ErrorLog
        /// </summary>
        public static ErrorLog Logg<T>(Exception e, bool suppressMessage = false)
        {
            if (ExceptionHandler.HasCaught(e))
                return ExceptionHandler.GetErrorLog(e) as ErrorLog;

            var error = new ErrorLog
            {
                UserName = DwarfContext<T>.GetConfiguration().UserService.CurrentUser != null ? DwarfContext<T>.GetConfiguration().UserService.CurrentUser.UserName : "",
                TimeStamp = DateTime.Now,
                Message = e.Message,
                Type = e.GetType().ToString(),
                StackTrace = e.StackTrace,
                Exception = e,
            };

            if (e.InnerException != null)
                error.InnerException = Logg<T>(e.InnerException, suppressMessage);

            DwarfContext<T>.GetDatabase().Insert<T, ErrorLog>(error);

            if (!suppressMessage)
                ExceptionHandler.Logg(error);

            return error;
        }

        #endregion Logg

        #region Load

        /// <summary>
        /// See base
        /// </summary>
        public static ErrorLog Load<T>(Guid id)
        {
            return DwarfContext<T>.GetDatabase().Select<T, ErrorLog>(id);
        }

        /// <summary>
        /// See base
        /// </summary>
        public new static ErrorLog Load(Guid id)
        {
            throw new InvalidOperationException("Use Load<T> for Dwarf's ErrorLogs instead");
        }

        #endregion Load

        #region LoadAll

        /// <summary>
        /// See base
        /// </summary>
        public static List<ErrorLog> LoadAll<T>()
        {
            return DwarfContext<T>.GetDatabase().SelectReferencing<T, ErrorLog>();
        }

        /// <summary>
        /// See base
        /// </summary>
        public new static List<ErrorLog> LoadAll()
        {
            throw new InvalidOperationException("Use LoadAll<T> for Dwarf's ErrorLogs instead");
        }

        #endregion LoadAll

        #endregion Methods
    }

    #region ErrorLogService

    /// <summary>
    /// Dwarf specific implemention of the ErrorLog Service
    /// </summary>
    public class ErrorLogService<T> : IErrorLogService
    {
        /// <summary>
        /// See base
        /// </summary>
        public IErrorLog Logg(Exception exception, bool suppressMessage = false)
        {
            return ErrorLog.Logg<T>(exception, suppressMessage);
        }

        /// <summary>
        /// See base
        /// </summary>
        public IErrorLog Load(Guid id)
        {
            return ErrorLog.Load<T>(id);
        }        

        /// <summary>
        /// See base
        /// </summary>
        public List<IErrorLog> LoadAll()
        {
            return ErrorLog.LoadAll<T>().Cast<IErrorLog>().ToList();
        }

        /// <summary>
        /// See base
        /// </summary>
        public IErrorLog CreateInstance()
        {
            return new ErrorLog();
        }
    }

    #endregion ErrorLogService
}
