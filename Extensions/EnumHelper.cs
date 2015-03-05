using System;
using System.Collections.Generic;
using System.Linq;

namespace Evergreen.Dwarf.Extensions
{
    /// <summary>
    /// Holds extension methods for Enumerables
    /// </summary>
    public static class EnumHelper
    {
        #region Parse

        /// <summary>
        /// Parse an enum without the nasty syntax
        /// </summary>
        public static T Parse<T>(string value, bool ignoreCase = false)
        {
            if (string.IsNullOrEmpty(value))
                return default(T);

            return (T)Enum.Parse(typeof(T).GetTrueEnumType(), value, ignoreCase);
        }

        /// <summary>
        /// Parse an enum without the nasty syntax
        /// </summary>
        public static object Parse(Type type, string value, bool ignoreCase = false)
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception();

            return Enum.Parse(type.GetTrueEnumType(), value, ignoreCase);
        }

        #endregion Parse

        #region GetValues

        /// <summary>
        /// Retrieves an array of the values of the constants in a specified enumeration.
        /// </summary>
        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        #endregion GetValues
    }
}
