using FeatureFlagEngine.Core.Interfaces;
using FeatureFlagEngine.Core.Services;
using FeatureFlagEngine.Infrastructure.Data;
using FeatureFlagEngine.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
builder.Services.AddDbContext<FeatureFlagDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=featureflags.db"));

// --- DI ---
builder.Services.AddMemoryCache();
builder.Services.AddScoped<SqliteFeatureFlagRepository>();
builder.Services.AddScoped<IFeatureFlagRepository>(sp =>
    new CachedFeatureFlagRepository(
        sp.GetRequiredService<SqliteFeatureFlagRepository>(),
        sp.GetRequiredService<IMemoryCache>()));
builder.Services.AddScoped<FeatureFlagService>();

var app = builder.Build();

// --- Serve React frontend from wwwroot ---
app.UseDefaultFiles();
app.UseStaticFiles();

// --- Auto-migrate on startup ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FeatureFlagDbContext>();
    if (db.Database.IsSqlite())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
}

// ===================== REST API Endpoints =====================

// --- Feature Flag CRUD ---

app.MapGet("/api/flags", async (FeatureFlagService service) =>
{
    var flags = await service.GetAllFlagsAsync();
    return Results.Ok(flags.Select(f => MapFlag(f)));
});

app.MapGet("/api/flags/{name}", async (string name, FeatureFlagService service) =>
{
    try
    {
        var flag = await service.GetFlagAsync(name);
        return Results.Ok(MapFlag(flag));
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = $"Feature flag '{name}' not found." });
    }
});

app.MapPost("/api/flags", async (CreateFlagRequest request, FeatureFlagService service) =>
{
    try
    {
        var flag = await service.CreateFlagAsync(request.Name, request.IsEnabled, request.Description);
        return Results.Created($"/api/flags/{flag.Name}", MapFlag(flag));
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/flags/{name}", async (string name, UpdateFlagRequest request, FeatureFlagService service) =>
{
    try
    {
        await service.UpdateGlobalStateAsync(name, request.IsEnabled);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = $"Feature flag '{name}' not found." });
    }
});

app.MapDelete("/api/flags/{name}", async (string name, FeatureFlagService service) =>
{
    try
    {
        await service.DeleteFlagAsync(name);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = $"Feature flag '{name}' not found." });
    }
});

// --- Evaluation ---

app.MapPost("/api/flags/{name}/evaluate", async (string name, EvaluateRequest? request, FeatureFlagService service) =>
{
    try
    {
        var result = await service.EvaluateAsync(name, request?.UserId, request?.GroupIds, request?.RegionId);
        return Results.Ok(new { flagName = name, isEnabled = result });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = $"Feature flag '{name}' not found." });
    }
});

// --- User Overrides ---

app.MapPut("/api/flags/{name}/users/{userId}", async (string name, string userId, OverrideRequest request, FeatureFlagService service) =>
{
    try
    {
        await service.SetUserOverrideAsync(name, userId, request.IsEnabled);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = $"Feature flag '{name}' not found." });
    }
});

app.MapDelete("/api/flags/{name}/users/{userId}", async (string name, string userId, FeatureFlagService service) =>
{
    try
    {
        await service.RemoveUserOverrideAsync(name, userId);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

// --- Group Overrides ---

app.MapPut("/api/flags/{name}/groups/{groupId}", async (string name, string groupId, OverrideRequest request, FeatureFlagService service) =>
{
    try
    {
        await service.SetGroupOverrideAsync(name, groupId, request.IsEnabled);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = $"Feature flag '{name}' not found." });
    }
});

app.MapDelete("/api/flags/{name}/groups/{groupId}", async (string name, string groupId, FeatureFlagService service) =>
{
    try
    {
        await service.RemoveGroupOverrideAsync(name, groupId);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

// --- Region Overrides ---

app.MapPut("/api/flags/{name}/regions/{regionId}", async (string name, string regionId, OverrideRequest request, FeatureFlagService service) =>
{
    try
    {
        await service.SetRegionOverrideAsync(name, regionId, request.IsEnabled);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = $"Feature flag '{name}' not found." });
    }
});

app.MapDelete("/api/flags/{name}/regions/{regionId}", async (string name, string regionId, FeatureFlagService service) =>
{
    try
    {
        await service.RemoveRegionOverrideAsync(name, regionId);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

// --- SPA fallback: serve index.html for non-API routes ---
app.MapFallbackToFile("index.html");

app.Run();

// ===================== Request / Response DTOs =====================

static object MapFlag(FeatureFlagEngine.Core.Models.FeatureFlag f) => new
{
    f.Name,
    f.IsEnabled,
    f.Description,
    UserOverrides = f.UserOverrides.Select(u => new { u.UserId, u.IsEnabled }),
    GroupOverrides = f.GroupOverrides.Select(g => new { g.GroupId, g.IsEnabled }),
    RegionOverrides = f.RegionOverrides.Select(r => new { r.RegionId, r.IsEnabled })
};

record CreateFlagRequest(string Name, bool IsEnabled, string? Description = null);
record UpdateFlagRequest(bool IsEnabled);
record OverrideRequest(bool IsEnabled);
record EvaluateRequest(string? UserId = null, IReadOnlyList<string>? GroupIds = null, string? RegionId = null);

// Needed for WebApplicationFactory in integration tests
public partial class Program { }
