namespace XFrame.Services
{
    public interface ISettingsService
    {
        string TargetParentTag { get; set; }
        string TargetChildTag { get; set; }
        string TargetAttribute { get; set; }
    }
}
