namespace XFrame.Services
{
    public class SettingsService : ISettingsService
    {
        public string TargetParentTag
        {
            get => Preferences.Default.Get("parent_tag", "Employee");
            set => Preferences.Default.Set("parent_tag", value);
        }

        public string TargetChildTag
        {
            get => Preferences.Default.Get("child_tag", "salary");
            set => Preferences.Default.Set("child_tag", value);
        }

        public string TargetAttribute
        {
            get => Preferences.Default.Get("attribute", "amount");
            set => Preferences.Default.Set("attribute", value);
        }
    }
}
