using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Linq;
using Dwarf.Interfaces;
using Dwarf.Utilities;

namespace Dwarf.Extensions
{
    /// <summary>
    /// Extension methods for AuditLogs
    /// </summary>
    public static class AuditLogExtensions
    {
        #region GetLogData

        /// <summary>
        /// Finds the event trace of the supplied property in the audit log
        /// and returns, if possible, a typed array of the values; OriginalValue | NewValue
        /// </summary>
        public static TY[] GetLogData<T, TY>(this IAuditLog auditLog, Expression<Func<T, TY>> expression)
        {
            if (string.IsNullOrEmpty(auditLog.AuditDetails))
                throw new InvalidOperationException("No events found.");

            var doc = XDocument.Parse(auditLog.AuditDetails);

            foreach (var element in doc.Root.Elements("Property"))
            {
                var trace = AuditLogEventTrace.FromXml<T>(element);
                
                if (trace.PropertyName.Equals(ReflectionHelper.GetPropertyName(expression)))
                {
                    var result = new TY[2];
                    result[0] = (TY)Convert.ChangeType(trace.OriginalValue, typeof(TY));
                    result[1] = (TY)Convert.ChangeType(trace.NewValue, typeof(TY));
                    return result;
                }
            }

            throw new InvalidOperationException("No events found for " + ReflectionHelper.GetPropertyName(expression));
        }

        #endregion GetLogData

        #region ContainsLogData

        /// <summary>
        /// Returns true if an event trace of the supplied property in the audit log is found
        /// </summary>
        public static bool ContainsLogData<T>(this IAuditLog auditLog, Expression<Func<T, object>> expression)
        {
            if (string.IsNullOrEmpty(auditLog.AuditDetails))
                return false;

            var doc = XDocument.Parse(auditLog.AuditDetails);

            foreach (var element in doc.Root.Elements("Property"))
            {
                var trace = AuditLogEventTrace.FromXml<T>(element);

                if (trace.PropertyName.Equals(ReflectionHelper.GetPropertyName(expression)))
                    return true;
            }

            return false;
        }

        #endregion ContainsLogData

        #region InjectTraceValues

        /// <summary>
        /// Injects the values from the current event trace into the supplied object
        /// </summary>
        public static object InjectTraceValues<T>(this IAuditLog auditLog, object obj)
        {
            var doc = XDocument.Parse(auditLog.AuditDetails);

            foreach (var element in doc.Root.Elements("Property"))
            {
                var trace = AuditLogEventTrace.FromXml<T>(element, obj.GetType());

                var pi = PropertyHelper.GetPropertyInfo(obj.GetType(), trace.PropertyName);

                if (pi == null || pi.GetSetMethod() == null)
                    continue;

                var originalType = trace.OriginalValue.GetType();

                if (originalType.Implements<IDwarf>())
                    originalType = originalType.BaseType;
                
                var val = originalType == pi.PropertyType ? trace.OriginalValue : Convert.ChangeType(trace.OriginalValue, pi.PropertyType.IsNullable() ? Nullable.GetUnderlyingType(pi.PropertyType) : pi.PropertyType);
                pi.SetValue(obj, val, null);
            }

            return obj;
        }

        #endregion InjectTraceValues

        #region AuditLogEventTraces

        /// <summary>
        /// Returns all AuditLogEventTraces for the current AuditLog
        /// </summary>
        public static IEnumerable<AuditLogEventTrace> AuditLogEventTraces<T> (this IAuditLog auditLog)
        {
            return AuditLogEventTraces<T>(auditLog, typeof(T));
        }


        /// <summary>
        /// Returns all AuditLogEventTraces for the current AuditLog
        /// </summary>
        public static IEnumerable<AuditLogEventTrace> AuditLogEventTraces<T>(this IAuditLog auditLog, Type type)
        {
            return string.IsNullOrEmpty(auditLog.AuditDetails)
                       ? new List<AuditLogEventTrace>()
                       : XDocument.Parse(auditLog.AuditDetails).Root.Elements("Property").Select(x => AuditLogEventTrace.FromXml<T>(x, type));
        }

        #endregion AuditLogEventTraces
    }
}
