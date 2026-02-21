using FeatureFlagEngine.Core.Interfaces;
using FeatureFlagEngine.Core.Models;
using FeatureFlagEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FeatureFlagEngine.Infrastructure.Repositories;

public class SqliteFeatureFlagRepository : IFeatureFlagRepository
{
    private readonly FeatureFlagDbContext _context;

    public SqliteFeatureFlagRepository(FeatureFlagDbContext context)
    {
        _context = context;
    }

    public async Task<FeatureFlag?> GetByNameAsync(string name)
    {
        return await _context.FeatureFlags
            .Include(f => f.UserOverrides)
            .Include(f => f.GroupOverrides)
            .Include(f => f.RegionOverrides)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Name == name);
    }

    public async Task<IReadOnlyList<FeatureFlag>> GetAllAsync()
    {
        return await _context.FeatureFlags
            .Include(f => f.UserOverrides)
            .Include(f => f.GroupOverrides)
            .Include(f => f.RegionOverrides)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task AddAsync(FeatureFlag flag)
    {
        _context.FeatureFlags.Add(flag);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(FeatureFlag flag)
    {
        _context.FeatureFlags.Update(flag);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string name)
    {
        var flag = await _context.FeatureFlags.FindAsync(name);
        if (flag is not null)
        {
            _context.FeatureFlags.Remove(flag);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await _context.FeatureFlags.AnyAsync(f => f.Name == name);
    }
}
