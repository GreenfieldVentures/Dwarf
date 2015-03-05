using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Evergreen.Dwarf.Extensions;
using Evergreen.Dwarf.Interfaces;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf
{
    /// <summary>
    /// A DwarfList extension for gems
    /// </summary>
    public class GemList<T> : DwarfList<T>, IGemList, IXmlSerializable where T : Gem<T>, new()
    {
        #region Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        public GemList()
        {
        }

        /// <summary>
        /// Constructor that initializes the collection with a predefined List of Ts
        /// </summary>
        public GemList(Expression<Func<T, object>> alternatePrimaryKey): base(alternatePrimaryKey)
        {
        }

        /// <summary>
        /// Constructor that initializes the collection with a predefined List of Ts
        /// </summary>
        public GemList(IEnumerable<T> list): base(list)
        {
        }

        /// <summary>
        /// Constructor that initializes the collection with a predefined List of Ts and uniqueColumns
        /// </summary>
        public GemList(IEnumerable<T> list, Expression<Func<T, object>> alternatePrimaryKey) : base(list, alternatePrimaryKey)
        {
            
        }        

        #endregion Constructors

        #region Parse

        /// <summary>
        /// See base
        /// </summary>
        public IGemList Parse(string value)
        {
            return ParseValue(value);
        }

        #endregion Parse

        #region ParseValue

        /// <summary>
        /// See Parse
        /// </summary>
        public static GemList<T> ParseValue(string value)
        {
            return new GemList<T>(new Regex(@"(?<=\¶)[^¶]+(?=\¶)", RegexOptions.IgnoreCase).Matches(value).Cast<Match>().Select(x => Gem<T>.Load(x.Groups[0].Value)), x => x.Id);
        }

        #endregion ParseValue

        #region ToString

        /// <summary>
        /// See base
        /// </summary>
        public override string ToString()
        {
            var value = this.OrderBy(x => x.ToString()).Select(x => x.ToString() + ", ").ToList();

            return value.Any() ? value.Flatten().TruncateEnd(2) : string.Empty;
        }

        #endregion ToString

        #region ComparisonString

        /// <summary>
        /// See base
        /// </summary>
        public string ComparisonString
        {
            get
            {
                var value = this.OrderBy(x => x.Id).Select(x => x.Id + ", ").ToList();

                return value.Any() ? value.Flatten().TruncateEnd(2) : string.Empty;   
            }
        }

        #endregion ComparisonString

        #region GetSchema

        /// <summary>
        /// See base
        /// </summary>
        public XmlSchema GetSchema()
        {
            return null;
        }

        #endregion GetSchema

        #region ReadXml

        /// <summary>
        /// See base
        /// </summary>
        public void ReadXml(XmlReader reader)
        {
            var wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            var items = new List<T>();

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.None)
                    break;

                reader.ReadStartElement("List");

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    reader.ReadStartElement("Item");
                    
                    reader.MoveToContent();
                    items.Add((T)GemHelper.Load(typeof(T), reader.ReadContentAsString()));
                    reader.ReadToNextSibling("Item");
                }

                reader.ReadEndElement();
            }

            InitializeList(items);
        }

        #endregion ReadXml

        #region WriteXml

        /// <summary>
        /// See base
        /// </summary>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("List");

            foreach (var obj in this)
            {
                writer.WriteStartElement("Item");
                writer.WriteValue(obj.Id);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        #endregion WriteXml
    }
}
