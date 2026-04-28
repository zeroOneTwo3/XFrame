namespace XFrame.Configuration
{
    /// <summary>
    /// File Picker file types for the application.
    /// </summary>
    public static class FileTypes
    {
        /// <summary>
        /// Xml file type definition for file picking operations on different platforms.
        /// </summary>
        public static readonly FilePickerFileType Xml = new FilePickerFileType(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
            { DevicePlatform.iOS, new[] { "public.xml" } },
            { DevicePlatform.Android, new[] { "application/xml", "text/xml" } },
            { DevicePlatform.WinUI, new[] { ".xml" } },
            { DevicePlatform.MacCatalyst, new[] { "public.xml" } },
            });

        /// <summary>
        /// Xslt file type definition for file picking operations on different platforms.
        /// </summary>
        public static readonly FilePickerFileType Xslt = new FilePickerFileType(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
            { DevicePlatform.iOS, new[] { "public.xml", "com.netscape.javascript-source" } },
            { DevicePlatform.Android, new[] { "application/xml", "text/xml", "text/plain" } },
            { DevicePlatform.WinUI, new[] { ".xslt", ".xsl" } },
            { DevicePlatform.MacCatalyst, new[] { "public.xml" } },
            });
    }
}
