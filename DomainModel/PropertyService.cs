using System;
using System.Collections.Generic;
using System.Reflection;
using Evergreen.Dwarf.Interfaces;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf
{
    public static class PropertyHelper
    {
        #region GetValue

        /// <summary>
        /// Gets a property's value
        /// </summary>
        public static object GetValue(object obj, string propertyName)
        {
            if (obj is IDwarf)
            {
                var type = DwarfHelper.DeProxyfy(obj);

                if (!Cfg.PropertyExpressions[type].ContainsKey(propertyName))
                    throw new ApplicationException(type.Name + " doesn't have a property: " + propertyName);

                return Cfg.PropertyExpressions[type][propertyName].GetValue(obj);
            }
            else
            {
                var type = obj.GetType();
                var pi = type.GetProperty(propertyName);

                if (pi == null)
                    throw new ApplicationException(type.Name + " doesn't have a property: " + propertyName);

                return pi.GetValue(obj, null);
            }
        }

        #endregion GetValue

        #region SetValue

        /// <summary>
        /// Sets a property's value
        /// </summary>
        public static void SetValue(object obj, string propertyName, object value)
        {
            if (obj is IDwarf)
            {
                var type = DwarfHelper.DeProxyfy(obj);

                if (!Cfg.PropertyExpressions[type].ContainsKey(propertyName))
                    throw new ApplicationException(type.Name + " doesn't have a property: " + propertyName);

                var ep = Cfg.PropertyExpressions[type][propertyName];

                if (ep.CanWrite)
                    ep.SetValue(obj, value);
            }
            else
            {
                var type = obj.GetType();
                var pi = type.GetProperty(propertyName);

                if (pi == null)
                    throw new ApplicationException(type.Name + " doesn't have a property: " + propertyName);

                if (pi.CanWrite)
                    pi.SetValue(obj, value, null);
            }
        }

        #endregion SetValue

        #region HasProperty

        /// <summary>
        /// Returns true if the supplied object contains a property by the specified name
        /// </summary>
        public static bool HasProperty<T>(string propertyName)
        {
            return HasProperty(typeof(T), propertyName);
        }

        /// <summary>
        /// Returns true if the supplied object contains a property by the specified name
        /// </summary>
        public static bool HasProperty(object obj, string propertyName)
        {
            return HasProperty(obj.GetType(), propertyName);
        }

        /// <summary>
        /// Returns true if the supplied object contains a property by the specified name
        /// </summary>
        public static bool HasProperty(Type type, string propertyName)
        {
            return Cfg.PropertyExpressions[DwarfHelper.DeProxyfy(type)].ContainsKey(propertyName);
        }

        #endregion HasProperty

        #region GetPropertyInfo

        /// <summary>
        /// See base
        /// </summary>
        public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            var ep = GetProperty(type, propertyName);

            return ep != null ? ep.ContainedProperty : null;
        }

        /// <summary>
        /// See base
        /// </summary>
        public static PropertyInfo GetPropertyInfo(object obj, string propertyName)
        {
            return GetPropertyInfo(obj.GetType(), propertyName);
        }

        #endregion GetPropertyInfo

        #region GetPropertyInfo

        public static ExpressionProperty GetProperty(Type type, string propertyName)
        {
            DwarfHelper.DeProxyfy(ref type);

            return Cfg.PropertyExpressions[type].ContainsKey(propertyName)
                       ? Cfg.PropertyExpressions[type][propertyName]
                       : null;
        }

        public static ExpressionProperty GetProperty<T>(string propertyName)
        {
            return GetProperty(typeof (T), propertyName);
        }

        #endregion GetPropertyInfo

        #region GetProperties

        internal static IEnumerable<ExpressionProperty> GetProperties(Type type)
        {
            DwarfHelper.DeProxyfy(ref type);

            return Cfg.PropertyExpressions[type].Values;
        }

        #endregion GetProperties
    }
}
