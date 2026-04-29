namespace XFrame.Core.Interfaces
{
    /// <summary>
    /// Provides XML processing capabilities, including XSLT transformations and specialized node value summation.
    /// </summary>
    public interface IXmlProcessorService
    {
        /// <summary>
        /// Transforms an XML string using the provided XSLT stylesheet.
        /// </summary>
        /// <param name="xml">The raw XML input string.</param>
        /// <param name="xslt">The XSLT stylesheet content.</param>
        /// <returns>A string containing the transformed XML result.</returns>
        /// <exception cref="XmlException">Thrown if input XML or XSLT is malformed.</exception>
        string Transform(string xml, string xslt);

        /// <summary>
        /// Locates a specific parent/child structure and sums a numeric attribute's value.
        /// </summary>
        /// <param name="xml">The source XML to process.</param>
        /// <param name="parentTag">The name of the parent element.</param>
        /// <param name="childTag">The name of the child element containing the data.</param>
        /// <param name="attribute">The attribute holding the numeric value to sum.</param>
        /// <returns>The modified XML string with the sum injected, or null if the structure is not found.</returns>
        string? ProcessXmlSum(string xml, string parentTag, string childTag, string attribute);

        /// <summary>
        /// Extracts unique xml tags from the specified xml
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IEnumerable<string> GetUniqueTags(string xml, CancellationToken ct);
    }
}
