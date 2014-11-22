using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Dwarf.DataAccess;
using Dwarf.Extensions;
using Dwarf.Interfaces;

namespace Dwarf
{
    /// <summary>
    /// Class that represnts a change in an object
    /// </summary>
    public class AuditLogEventTrace
    {
        #region Properties

        #region PropertyName

        /// <summary>
        /// Gets or Sets the name of the affected property
        /// </summary>
        public string PropertyName { get; set; }

        #endregion PropertyName

        #region OriginalValue

        /// <summary>
        /// Gets or Sets the original value
        /// </summary>
        public object OriginalValue { get; set; }

        #endregion OriginalValue

        #region NewValue

        /// <summary>
        /// Gets or Sets the new value
        /// </summary>
        public object NewValue { get; set; }

        #endregion NewValue

        #endregion Properties

        #region Methods

        #region FromXml

        /// <summary>
        /// Converts an XElement to an AuditLogEventTrace
        /// </summary>
        public static AuditLogEventTrace FromXml<T>(XElement element)
        {
            return FromXml<T>(element, typeof(T));
        }

        /// <summary>
        /// Converts an XElement to an AuditLogEventTrace
        /// </summary>
        public static AuditLogEventTrace FromXml<T>(XElement element, Type type)
        {
            if (!element.Name.LocalName.Equals("Property"))
                throw new InvalidOperationException("Invalid XElement");

            var propertyName = element.Attribute("Name");
            var originalValue = element.Attribute("OriginalValue");
            var newValue = element.Attribute("NewValue");

            if (propertyName == null || string.IsNullOrEmpty(propertyName.Value))
                throw new InvalidOperationException("Invalid AuditLogEventTrace");

            var pi = PropertyHelper.GetPropertyInfo(type, propertyName.Value);

            //pi can be null if a now removed property has been logged
            if (pi == null)
            {
                return new AuditLogEventTrace
                {
                    PropertyName = propertyName.Value,
                    OriginalValue = originalValue != null ? originalValue.Value : null,
                    NewValue = newValue != null ? newValue.Value : null,
                };
            }

            return new AuditLogEventTrace 
            {
                PropertyName = propertyName != null ? propertyName.Value : null,
                OriginalValue = originalValue != null ? RecreateValue<T>(pi, originalValue.Value) : null,
                NewValue = newValue != null ? RecreateValue<T>(pi, newValue.Value) : null,
            };
        }

        #endregion FromXml

        #region RecreateValue

        private static object RecreateValue<T>(PropertyInfo pi, string value)
        {
            if (pi.PropertyType.Implements<ICollection>())
            {
                if (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericArguments()[0].Implements<IDwarf>())
                {
                    var innerType = pi.PropertyType.GetGenericArguments()[0];

                    var list = new List<IDwarf>();

                    if (string.IsNullOrEmpty(value))
                        return list;

                    var ids = value.Split(new[] {";"}, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var id in ids)
                    {
                        var guid = Guid.Parse(id);

                        var obj = innerType.FindMethodRecursively("Load", new[] { typeof(Guid) }).Invoke(null, new object[] { guid }); 

                        if (obj == null)
                        {
                            var log = DwarfContext<T>.GetConfiguration().AuditLogService.LoadAllReferencing(guid).FirstOrDefault(x => x.AuditLogType == AuditLogTypes.Deleted);

                            if (log == null)
                                continue;

                            obj = Activator.CreateInstance(innerType);
                            log.InjectTraceValues<T>(obj);
                            ((IDwarf) obj).Id = guid;
                        }

                        list.Add((IDwarf)obj);
                    }

                    return list;
                }
                else
                {
                    var sr = new StringReader(value);
                    var reader = XmlReader.Create(sr);

                    return new XmlSerializer(pi.PropertyType).Deserialize(reader);
                }
            }

            if (string.IsNullOrEmpty(value))
                return value;

            if (pi.PropertyType.Implements<IDwarf>())
            {
                var id = Guid.Parse(value);

                var obj = pi.PropertyType.FindMethodRecursively("Load", new[] { typeof(Guid) }).Invoke(null, new object[] { id });

                //Might be deleted... try to locate the deleted event and recreate the object
                if (obj == null)
                {
                    var ev = DwarfContext<T>.GetConfiguration().AuditLogService.LoadAllReferencing(id).FirstOrDefault(x => x.AuditLogType == AuditLogTypes.Deleted);

                    if (ev != null)
                    {
                        obj = Activator.CreateInstance(pi.PropertyType);
                        ev.InjectTraceValues<T>(obj);
                    }
                }

                return obj;
            }

            if (pi.PropertyType.Implements<IGem>())
                return pi.PropertyType.FindMethodRecursively("Load", new[] { typeof(object) }).Invoke(null, new object[] { value });

            if (pi.PropertyType.IsEnum())
                return Enum.Parse(pi.PropertyType.GetTrueEnumType(), value);

            var type = (pi.PropertyType.IsNullable() && pi.PropertyType != typeof(string)) ? (pi.PropertyType.IsGenericType ? pi.PropertyType.GetGenericArguments()[0] : pi.PropertyType) : pi.PropertyType;

            return Convert.ChangeType(value, type);
        }

        #endregion RecreateValue

        #region ToXml

        /// <summary>
        /// Returns the object as an XElement
        /// </summary>
        public XElement ToXml()
        {
            var element = new XElement("Property");
            element.SetAttributeValue("Name", PropertyName);
            element.SetAttributeValue("OriginalValue", GetValue(OriginalValue));
            element.SetAttributeValue("NewValue", GetValue(NewValue));

            return element;
        }

        #endregion ToXml

        #region GetValue

        private static object GetValue(object obj)
        {
            if (obj != null && obj.GetType().Implements<ICollection>())
            {
                var sb = new StringBuilder();

                if (obj.GetType().IsGenericType && obj.GetType().GetGenericArguments()[0].Implements<IDwarf>())
                {
                    var pp = ((IList) obj).Cast<IDwarf>().Flatten(x => x.Id + ";");
                    sb.Append(pp);
                }
                else
                {
                    var writer = XmlWriter.Create(sb);
                    new XmlSerializer(obj.GetType()).Serialize(writer, obj);
                }
                
                return sb.ToString();
            }

            if (obj != null && obj.GetType().Implements<IDwarf>())
                return ((IDwarf) obj).Id;

            if (obj != null && obj.GetType().Implements<IGem>())
                return ((IGem)obj).Id;

            return obj != null ? obj.ToString() : null;
        }

        #endregion GetValue

        #endregion Methods
    }
}