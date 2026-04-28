namespace XFrame.Core.Interfaces
{
    public interface IXmlProcessorService
    {
        string Transform(string xml, string xslt);
        string ProcessXmlSum(string xml, string parentTag, string childTag, string attribute);
        IEnumerable<string> GetUniqueTags(string xml, CancellationToken ct);
    }
}
