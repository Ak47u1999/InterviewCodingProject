using FeatureFlagEngine.Core.Interfaces;
using FeatureFlagEngine.Core.Models;

namespace FeatureFlagEngine.Core.Services;

public class FeatureFlagService
{
    private readonly IFeatureFlagRepository _repository;

    public FeatureFlagService(IFeatureFlagRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Evaluates whether a feature is enabled for the given context.
    /// Precedence: user override > group override > global default.
    /// </summary>
    public async Task<bool> EvaluateAsync(string flagName, string? userId = null, IReadOnlyList<string>? groupIds = null)
    {
        var flag = await _repository.GetByNameAsync(flagName);
        if (flag is null)
            throw new KeyNotFoundException($"Feature flag '{flagName}' does not exist.");

        return Evaluate(flag, userId, groupIds);
    }

    /// <summary>
    /// Pure evaluation logic — no I/O, easy to test.
    /// </summary>
    public static bool Evaluate(FeatureFlag flag, string? userId = null, IReadOnlyList<string>? groupIds = null)
    {
        // 1. Check user-level override first
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var userOverride = flag.UserOverrides.FirstOrDefault(o => o.UserId == userId);
            if (userOverride is not null)
                return userOverride.IsEnabled;
        }

        // 2. Check group-level overrides
        if (groupIds is not null && groupIds.Count > 0)
        {
            foreach (var groupId in groupIds)
            {
                var groupOverride = flag.GroupOverrides.FirstOrDefault(o => o.GroupId == groupId);
                if (groupOverride is not null)
                    return groupOverride.IsEnabled;
            }
        }

        // 3. Fall back to the global default
        return flag.IsEnabled;
    }

    public async Task<FeatureFlag> CreateFlagAsync(string name, bool isEnabled, string? description = null)
    {
        if (await _repository.ExistsAsync(name))
            throw new InvalidOperationException($"A feature flag with name '{name}' already exists.");

        var flag = new FeatureFlag(name, isEnabled, description);
        await _repository.AddAsync(flag);
        return flag;
    }

    public async Task<FeatureFlag> GetFlagAsync(string name)
    {
        var flag = await _repository.GetByNameAsync(name);
        if (flag is null)
            throw new KeyNotFoundException($"Feature flag '{name}' does not exist.");

        return flag;
    }

    public async Task<IReadOnlyList<FeatureFlag>> GetAllFlagsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task UpdateGlobalStateAsync(string name, bool isEnabled)
    {
        var flag = await GetFlagAsync(name);
        flag.IsEnabled = isEnabled;
        await _repository.UpdateAsync(flag);
    }

    public async Task SetUserOverrideAsync(string flagName, string userId, bool isEnabled)
    {
        var flag = await GetFlagAsync(flagName);
        flag.SetUserOverride(userId, isEnabled);
        await _repository.UpdateAsync(flag);
    }

    public async Task RemoveUserOverrideAsync(string flagName, string userId)
    {
        var flag = await GetFlagAsync(flagName);
        if (!flag.RemoveUserOverride(userId))
            throw new KeyNotFoundException($"No user override found for user '{userId}' on flag '{flagName}'.");

        await _repository.UpdateAsync(flag);
    }

    public async Task SetGroupOverrideAsync(string flagName, string groupId, bool isEnabled)
    {
        var flag = await GetFlagAsync(flagName);
        flag.SetGroupOverride(groupId, isEnabled);
        await _repository.UpdateAsync(flag);
    }

    public async Task RemoveGroupOverrideAsync(string flagName, string groupId)
    {
        var flag = await GetFlagAsync(flagName);
        if (!flag.RemoveGroupOverride(groupId))
            throw new KeyNotFoundException($"No group override found for group '{groupId}' on flag '{flagName}'.");

        await _repository.UpdateAsync(flag);
    }

    public async Task DeleteFlagAsync(string name)
    {
        if (!await _repository.ExistsAsync(name))
            throw new KeyNotFoundException($"Feature flag '{name}' does not exist.");

        await _repository.DeleteAsync(name);
    }
}
