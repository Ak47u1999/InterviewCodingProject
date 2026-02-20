namespace FeatureFlagEngine.Core.Models;

public class FeatureFlag
{
    public string Name { get; private set; }
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }

    public List<UserOverride> UserOverrides { get; private set; } = new();
    public List<GroupOverride> GroupOverrides { get; private set; } = new();

    // EF Core needs a parameterless constructor, but we don't want callers to use it
    private FeatureFlag() { Name = string.Empty; }

    public FeatureFlag(string name, bool isEnabled, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Feature flag name cannot be empty.", nameof(name));

        Name = name.Trim();
        IsEnabled = isEnabled;
        Description = description;
    }

    public void SetUserOverride(string userId, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        var existing = UserOverrides.FirstOrDefault(o => o.UserId == userId);
        if (existing is not null)
        {
            existing.IsEnabled = isEnabled;
        }
        else
        {
            UserOverrides.Add(new UserOverride(Name, userId, isEnabled));
        }
    }

    public bool RemoveUserOverride(string userId)
    {
        var existing = UserOverrides.FirstOrDefault(o => o.UserId == userId);
        if (existing is null) return false;

        UserOverrides.Remove(existing);
        return true;
    }

    public void SetGroupOverride(string groupId, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));

        var existing = GroupOverrides.FirstOrDefault(o => o.GroupId == groupId);
        if (existing is not null)
        {
            existing.IsEnabled = isEnabled;
        }
        else
        {
            GroupOverrides.Add(new GroupOverride(Name, groupId, isEnabled));
        }
    }

    public bool RemoveGroupOverride(string groupId)
    {
        var existing = GroupOverrides.FirstOrDefault(o => o.GroupId == groupId);
        if (existing is null) return false;

        GroupOverrides.Remove(existing);
        return true;
    }
}
