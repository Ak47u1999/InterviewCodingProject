namespace FeatureFlagEngine.Core.Models;

public class GroupOverride
{
    public int Id { get; private set; }
    public string FeatureFlagName { get; private set; }
    public string GroupId { get; private set; }
    public bool IsEnabled { get; set; }

    private GroupOverride()
    {
        FeatureFlagName = string.Empty;
        GroupId = string.Empty;
    }

    public GroupOverride(string featureFlagName, string groupId, bool isEnabled)
    {
        FeatureFlagName = featureFlagName;
        GroupId = groupId;
        IsEnabled = isEnabled;
    }
}
