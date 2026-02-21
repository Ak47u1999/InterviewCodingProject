namespace FeatureFlagEngine.Core.Models;

public class RegionOverride
{
    public int Id { get; private set; }
    public string FeatureFlagName { get; private set; }
    public string RegionId { get; private set; }
    public bool IsEnabled { get; set; }

    private RegionOverride()
    {
        FeatureFlagName = string.Empty;
        RegionId = string.Empty;
    }

    public RegionOverride(string featureFlagName, string regionId, bool isEnabled)
    {
        FeatureFlagName = featureFlagName;
        RegionId = regionId;
        IsEnabled = isEnabled;
    }
}
