using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace Evergreen.Dwarf.Extensions
{
    /// <summary>
    /// Holds extension methods for strings
    /// </summary>
    public static class StringExtensions
    {
        #region PurgeHtml

        /// <summary>
        /// Returns the value purged from all html elements
        /// </summary>
        public static string PurgeHtml(this string value)
        {
            return value == null ? null : Regex.Replace(value, @"<(.|\n)*?>", string.Empty);
        }

        #endregion PurgeHtml

        #region TruncateStart

        /// <summary>
        /// Removes the specified number of characters from the start of the string
        /// </summary>
        public static string TruncateStart(this string s, int length)
        {
            return s.Substring(length, s.Length - length);
        }

        #endregion TruncateEnd

        #region TruncateEnd

        /// <summary>
        /// Removes the specified number of characters from the end of the string
        /// </summary>
        public static string TruncateEnd(this string s, int length)
        {
            return s.Substring(0, s.Length - length);
        }

        #endregion TruncateEnd

        #region RemoveAll

        /// <summary>
        /// Removes all occurances of the supplied value
        /// </summary>
        public static string RemoveAll(this string s, string value)
        {
            return s.Replace(value, string.Empty);
        }

        /// <summary>
        /// Removes all occurances of the supplied values
        /// </summary>
        public static string RemoveAll(this string s, params string[] values)
        {
            foreach (var value in values)
                s = s.RemoveAll(value);

            return s;
        }

        #endregion RemoveAll

        #region ToUpperAtIndex

        /// <summary>
        /// Make the character at the specified index upper case
        /// </summary>
        public static string ToUpperAtIndex(this string s, int index)
        {
            var c = s[index].ToString().ToUpper();
            s = s.Remove(index, 1);
            s = s.Insert(index, c);

            return s;
        }

        #endregion ToUpperAtIndex

        #region ToLowerAtIndex

        /// <summary>
        /// Make the character at the specified index lower case
        /// </summary>
        public static string ToLowerAtIndex(this string s, int index)
        {
            var c = s[index].ToString().ToLower();
            s = s.Remove(index, 1);
            s = s.Insert(index, c);

            return s;
        }

        #endregion ToLowerAtIndex

        #region SerializeForUrlQuery

        /// <summary>
        /// Serializes and returns the string as a url query compliant value
        /// </summary>
        public static string SerializeForUrlQuery(this string s)
        {
            var formatter = new LosFormatter();
            var writer = new StringWriter();

            formatter.Serialize(writer, s);

            return HttpUtility.UrlEncode(writer.GetStringBuilder().ToString());
        }

        #endregion SerializeForUrlQuery

        #region DeserializeFromUrlQuery

        /// <summary>
        /// Deserializes and returns the string from a url query compliant value
        /// </summary>
        public static string DeserializeFromUrlQuery(this string s)
        {
            var formatter = new LosFormatter();
            var result = formatter.Deserialize(HttpUtility.UrlDecode(s));
            return (string)result;
        }

        #endregion DeserializeFromUrlQuery

        #region Flatten

        /// <summary>
        /// Flattens the collection of strings into one 
        /// </summary>
        public static string Flatten(this IEnumerable<string> collection)
        {
            return string.Concat(collection);
        }

        /// <summary>
        /// Flattens the collection of strings into one 
        /// </summary>
        public static string Flatten<T>(this IEnumerable<T> collection, Func<T, string> func)
        {
            return collection.Select(func).Flatten();
        }

        #endregion Flatten
    }
}
