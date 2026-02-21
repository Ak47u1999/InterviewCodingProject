using FeatureFlagEngine.Core.Interfaces;
using FeatureFlagEngine.Core.Models;

namespace FeatureFlagEngine.Core.Tests.Fakes;

/// <summary>
/// Simple in-memory repository for testing. No real DB, just a dictionary.
/// </summary>
public class InMemoryFeatureFlagRepository : IFeatureFlagRepository
{
    private readonly Dictionary<string, FeatureFlag> _flags = new();

    public Task<FeatureFlag?> GetByNameAsync(string name)
    {
        _flags.TryGetValue(name, out var flag);
        return Task.FromResult(flag);
    }

    public Task<IReadOnlyList<FeatureFlag>> GetAllAsync()
    {
        IReadOnlyList<FeatureFlag> result = _flags.Values.ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(FeatureFlag flag)
    {
        _flags[flag.Name] = flag;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FeatureFlag flag)
    {
        _flags[flag.Name] = flag;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string name)
    {
        _flags.Remove(name);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string name)
    {
        return Task.FromResult(_flags.ContainsKey(name));
    }
}
