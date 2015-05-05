using System;
using System.Reflection;

namespace Evergreen.Dwarf.Extensions
{
    /// <summary>
    /// Static class for holding extension methods for Types
    /// </summary>
    public static class TypeExtensions
    {
        #region Implements

        /// <summary>
        /// Returns true if the type implements or is a descendant of the specified generic type
        /// </summary>
        public static bool Implements<T> (this Type type)
        {
            return typeof(T).IsAssignableFrom(type);
        }

        #endregion Implements        
        
        #region HasAttribute

        /// <summary>
        /// Returns true if the type defines or is a descendant of a type that defines the specified attribute
        /// </summary>
        public static bool HasAttribute<T>(this Type type) where T: Attribute
        {
            return Attribute.GetCustomAttribute(type, typeof (T), true) != null;
        }

        #endregion HasAttribute

        #region GetAttribute

        /// <summary>
        /// Returns the attribute if the type defines or is a descendant of a type that defines the specified attribute
        /// </summary>
        public static T GetAttribute<T>(this Type type) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(type, typeof (T), true);
        }

        #endregion GetAttribute

        #region FindMethodRecursively

        /// <summary>
        /// Extends GetMethod by looking beyond the first level of supplied type for the specified method
        /// </summary>
        public static MethodInfo FindMethodRecursively (this Type type, string name, Type[] types = null)
        {
            var currentType = type;

            while (currentType != null)
            {
                var result = types != null ? currentType.GetMethod(name, types) : currentType.GetMethod(name);

                if (result != null)
                    return result;

                currentType = currentType.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Extends GetMethod by looking beyond the first level of supplied type for the specified method
        /// </summary>
        public static MethodInfo FindMethodRecursively(this Type type, string name, BindingFlags bindingFlags, Type[] types = null)
        {
            var currentType = type;

            while (currentType != null)
            {
                var result = currentType.GetMethod(name, bindingFlags, null, types, new ParameterModifier[]{});

                if (result != null)
                    return result;

                currentType = currentType.BaseType;
            }

            return null;
        }

        #endregion FindMethodRecursively

        #region IsNumeric

        /// <summary>
        /// Checks if type is numeric.
        /// </summary>
        /// <param name="valueType">Type of object to check</param>
        public static bool IsNumeric(this Type valueType)
        {
            var typeCode = Type.GetTypeCode(valueType);

            return typeCode == TypeCode.Int16 || typeCode == TypeCode.Int32 || typeCode == TypeCode.Int64 ||
                   typeCode == TypeCode.UInt16 || typeCode == TypeCode.UInt32 || typeCode == TypeCode.UInt64 ||
                   typeCode == TypeCode.Double || typeCode == TypeCode.Decimal;
        }

        #endregion IsNumeric  
  
        #region IsNullable

        /// <summary>
        /// Checks if type is nullable
        /// </summary>
        public static bool IsNullable(this Type type)
        {
            if (type.IsClass)
                return true;

            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
        }

        #endregion IsNullable

        #region IsDateTime

        /// <summary>
        /// Checks if type is a DateTime.
        /// </summary>
        /// <param name="valueType">Type of object to check</param>
        public static bool IsDateTime(this Type valueType)
        {
            return Type.GetTypeCode(valueType) == TypeCode.DateTime;
        }

        #endregion IsDateTime 

        #region IsEnum

        /// <summary>
        /// Checks if the type (or its underlying generic type) is an enum.
        /// </summary>
        public static bool IsEnum(this Type type)
        {
            return GetTrueEnumType(type) != null;
        }

        #endregion IsEnum

        #region GetTrueEnumType

        /// <summary>
        /// Returns the enum of the supplied type (given that it is an enum). 
        /// Needed since nullable enumerators are not seen as enumerators, but their first generic argument is.
        /// </summary>
        public static Type GetTrueEnumType(this Type type)
        {
            if (type.Implements<Enum>())
                return type;

            if (type.IsGenericType && type.GetGenericArguments()[0].Implements<Enum>())
                return type.GetGenericArguments()[0];

            return null;  
        }

        #endregion GetTrueEnumType
    }
}
