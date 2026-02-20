using FeatureFlagEngine.Core.Models;

namespace FeatureFlagEngine.Core.Interfaces;

public interface IFeatureFlagRepository
{
    Task<FeatureFlag?> GetByNameAsync(string name);
    Task<IReadOnlyList<FeatureFlag>> GetAllAsync();
    Task AddAsync(FeatureFlag flag);
    Task UpdateAsync(FeatureFlag flag);
    Task DeleteAsync(string name);
    Task<bool> ExistsAsync(string name);
}
