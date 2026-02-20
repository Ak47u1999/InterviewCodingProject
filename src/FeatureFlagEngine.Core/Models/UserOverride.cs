namespace FeatureFlagEngine.Core.Models;

public class UserOverride
{
    public int Id { get; private set; }
    public string FeatureFlagName { get; private set; }
    public string UserId { get; private set; }
    public bool IsEnabled { get; set; }

    private UserOverride()
    {
        FeatureFlagName = string.Empty;
        UserId = string.Empty;
    }

    public UserOverride(string featureFlagName, string userId, bool isEnabled)
    {
        FeatureFlagName = featureFlagName;
        UserId = userId;
        IsEnabled = isEnabled;
    }
}
