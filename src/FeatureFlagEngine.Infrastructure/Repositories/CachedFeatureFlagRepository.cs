using FeatureFlagEngine.Core.Interfaces;
using FeatureFlagEngine.Core.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FeatureFlagEngine.Infrastructure.Repositories;

/// <summary>
/// Decorator around any IFeatureFlagRepository that adds in-memory caching.
/// Reads are served from cache; writes invalidate the cache.
/// </summary>
public class CachedFeatureFlagRepository : IFeatureFlagRepository
{
    private readonly IFeatureFlagRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string AllFlagsCacheKey = "__all_flags__";

    public CachedFeatureFlagRepository(IFeatureFlagRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<FeatureFlag?> GetByNameAsync(string name)
    {
        var cacheKey = $"flag_{name}";
        if (_cache.TryGetValue(cacheKey, out FeatureFlag? cached))
            return cached;

        var flag = await _inner.GetByNameAsync(name);
        if (flag is not null)
            _cache.Set(cacheKey, flag, CacheDuration);

        return flag;
    }

    public async Task<IReadOnlyList<FeatureFlag>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllFlagsCacheKey, out IReadOnlyList<FeatureFlag>? cached))
            return cached!;

        var flags = await _inner.GetAllAsync();
        _cache.Set(AllFlagsCacheKey, flags, CacheDuration);
        return flags;
    }

    public async Task AddAsync(FeatureFlag flag)
    {
        await _inner.AddAsync(flag);
        InvalidateCache(flag.Name);
    }

    public async Task UpdateAsync(FeatureFlag flag)
    {
        await _inner.UpdateAsync(flag);
        InvalidateCache(flag.Name);
    }

    public async Task DeleteAsync(string name)
    {
        await _inner.DeleteAsync(name);
        InvalidateCache(name);
    }

    public Task<bool> ExistsAsync(string name)
    {
        // Existence checks are cheap; delegate directly
        return _inner.ExistsAsync(name);
    }

    private void InvalidateCache(string flagName)
    {
        _cache.Remove($"flag_{flagName}");
        _cache.Remove(AllFlagsCacheKey);
    }
}
