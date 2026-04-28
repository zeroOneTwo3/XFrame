using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using XFrame.Core.Interfaces;

namespace XFrame.Core.Services
{
    public class XmlProcessorService : IXmlProcessorService
    {
        private static readonly XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false
        };
        public string ProcessXmlSum(string xml, string parentTag, string childTag, string attribute)
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

        public IEnumerable<string> GetUniqueTags(string xml)
        {
            var allTags = new HashSet<string>();
            using var reader = XmlReader.Create(new StringReader(xml));
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                    allTags.Add(reader.Name);
            }

            return allTags;
        }

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

        private double ParseAmount(string? val)
        {
            var input = val?.Trim();
            if (string.IsNullOrWhiteSpace(input) || !char.IsDigit(input.First()))
                return 0;

            input = input.Replace(',', '.');
            // Try parsing with invariant culture first
            if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;

            // If that fails, try the current culture (handles comma/dot issues)
            if (double.TryParse(input, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return result;

            // If both fail, return 0 or throw an error
            return 0;
        }
    }
}
