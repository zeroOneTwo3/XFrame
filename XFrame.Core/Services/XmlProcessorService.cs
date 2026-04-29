using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using XFrame.Core.Interfaces;

namespace XFrame.Core.Services
{
    /// <inheritdoc />
    public class XmlProcessorService : IXmlProcessorService
    {
        private static readonly XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false
        };

        /// <inheritdoc />
        public string? ProcessXmlSum(string xml, string parentTag, string childTag, string attribute)
        {
            var doc = XDocument.Parse(xml);
            var targetNode = doc.Descendants(parentTag).FirstOrDefault();
            if (targetNode == null)
            {
                return null;
            }

            var targetNodes = targetNode.Parent == null
                ? [targetNode]
                : targetNode.Parent.Elements();

            foreach (var parent in targetNodes)
            {
                //  Only add the 'total' attribute if we actually found children with the specified attribute
                if (parent.Elements(childTag).Any(e => e.Attribute(attribute) != null))
                {
                    double total = parent.Elements(childTag)
                        .Select(child => ParseAmount((string?)child.Attribute(attribute)))
                        .Sum();

                    parent.SetAttributeValue("total", total.ToString("F2", CultureInfo.InvariantCulture));
                }
            }

            using var stringWriter = new StringWriter();
            using (var writer = XmlWriter.Create(stringWriter, xmlWriterSettings))
            {
                doc.Save(writer);
            }

            return stringWriter.ToString();
        }

        /// <inheritdoc />
        public IEnumerable<string> GetUniqueTags(string xml, CancellationToken ct)
        {
            var allTags = new HashSet<string>();
            using var reader = XmlReader.Create(new StringReader(xml));
            while (reader.Read())
            {
                ct.ThrowIfCancellationRequested();
                if (reader.NodeType == XmlNodeType.Element)
                    allTags.Add(reader.Name);
            }

            return allTags;
        }

        /// <inheritdoc />
        public string Transform(string xml, string xslt)
        {
            using var xmlReader = XmlReader.Create(new StringReader(xml));
            using var xsltReader = XmlReader.Create(new StringReader(xslt));

            var transformer = new XslCompiledTransform();
            transformer.Load(xsltReader);

            using var resultsWriter = new StringWriter();
            transformer.Transform(xmlReader, null, resultsWriter);

            return resultsWriter.ToString();
        }

        /// <summary>
        /// Parses a string into a double, handling various cultural formats 
        /// and common XML data inconsistencies.
        /// </summary>
        /// <param name="val">The string value to parse.</param>
        /// <returns>The parsed double value, or 0 if parsing fails.</returns>
        private double ParseAmount(string? val)
        {
            if (string.IsNullOrWhiteSpace(val))
                return 0;

            var input = val.Trim();

            // Try Invariant Culture (usually the standard for XML/Data). Invariant expects '.' as decimal
            if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;

            // Try Current Culture (user's local Windows/Phone settings)
            if (double.TryParse(input, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return result;

            // Last Resort: Handle cases where data might have "wrong" separators (try manual cleaning).
            var cleanedInput = input.Replace(',', '.');
            if (double.TryParse(cleanedInput, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;

            System.Diagnostics.Debug.WriteLine($"[Parser]: Failed to parse '{val}'");
            return 0;
        }
    }
}
