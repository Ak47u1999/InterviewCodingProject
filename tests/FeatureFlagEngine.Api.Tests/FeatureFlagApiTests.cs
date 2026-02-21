using System.Net;
using System.Net.Http.Json;
using FeatureFlagEngine.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FeatureFlagEngine.Api.Tests;

public class FeatureFlagApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FeatureFlagApiTests(WebApplicationFactory<Program> factory)
    {
        var dbName = $"TestDb-{Guid.NewGuid()}";
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all EF Core / DbContext registrations from the real app
                var efDescriptors = services
                    .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                             || d.ServiceType == typeof(DbContextOptions<FeatureFlagDbContext>)
                             || d.ServiceType == typeof(DbContextOptions)
                             || d.ServiceType == typeof(FeatureFlagDbContext))
                    .ToList();
                foreach (var d in efDescriptors) services.Remove(d);

                services.AddDbContext<FeatureFlagDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        }).CreateClient();
    }

    // ---- Create ----

    [Fact]
    public async Task CreateFlag_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/flags",
            new { Name = "dark-mode", IsEnabled = false, Description = "Dark theme" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<FlagDto>();
        body!.Name.Should().Be("dark-mode");
        body.IsEnabled.Should().BeFalse();
        body.Description.Should().Be("Dark theme");
    }

    [Fact]
    public async Task CreateFlag_Duplicate_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "dup-flag", IsEnabled = true });

        var response = await _client.PostAsJsonAsync("/api/flags", new { Name = "dup-flag", IsEnabled = false });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---- Get ----

    [Fact]
    public async Task GetFlag_Existing_ReturnsOk()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "get-test", IsEnabled = true });

        var response = await _client.GetAsync("/api/flags/get-test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FlagDto>();
        body!.Name.Should().Be("get-test");
    }

    [Fact]
    public async Task GetFlag_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/flags/ghost");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllFlags_ReturnsAllCreated()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "list-a", IsEnabled = true });
        await _client.PostAsJsonAsync("/api/flags", new { Name = "list-b", IsEnabled = false });

        var response = await _client.GetAsync("/api/flags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<FlagDto>>();
        body!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    // ---- Update global state ----

    [Fact]
    public async Task UpdateGlobalState_ReturnsNoContent()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "toggle-me", IsEnabled = false });

        var response = await _client.PutAsJsonAsync("/api/flags/toggle-me", new { IsEnabled = true });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetFromJsonAsync<FlagDto>("/api/flags/toggle-me");
        get!.IsEnabled.Should().BeTrue();
    }

    // ---- Delete ----

    [Fact]
    public async Task DeleteFlag_ReturnsNoContent()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "del-me", IsEnabled = true });

        var response = await _client.DeleteAsync("/api/flags/del-me");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteFlag_NonExistent_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/flags/nope");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- Evaluate ----

    [Fact]
    public async Task Evaluate_GlobalDefault_ReturnsCorrectState()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "eval-global", IsEnabled = true });

        var response = await _client.PostAsJsonAsync("/api/flags/eval-global/evaluate",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EvalDto>();
        body!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_UserOverride_TakesPrecedence()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "eval-user", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/eval-user/users/alice", new { IsEnabled = true });

        var response = await _client.PostAsJsonAsync("/api/flags/eval-user/evaluate",
            new { UserId = "alice" });

        var body = await response.Content.ReadFromJsonAsync<EvalDto>();
        body!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_GroupOverride_WhenNoUserOverride()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "eval-group", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/eval-group/groups/beta", new { IsEnabled = true });

        var response = await _client.PostAsJsonAsync("/api/flags/eval-group/evaluate",
            new { GroupIds = new[] { "beta" } });

        var body = await response.Content.ReadFromJsonAsync<EvalDto>();
        body!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_UserOverride_BeatsGroupOverride()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "eval-precedence", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/eval-precedence/groups/beta", new { IsEnabled = true });
        await _client.PutAsJsonAsync("/api/flags/eval-precedence/users/alice", new { IsEnabled = false });

        var response = await _client.PostAsJsonAsync("/api/flags/eval-precedence/evaluate",
            new { UserId = "alice", GroupIds = new[] { "beta" } });

        var body = await response.Content.ReadFromJsonAsync<EvalDto>();
        body!.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Evaluate_NonExistentFlag_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/flags/missing/evaluate", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- User override CRUD ----

    [Fact]
    public async Task SetUserOverride_ReturnsNoContent()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "user-ov", IsEnabled = false });

        var response = await _client.PutAsJsonAsync("/api/flags/user-ov/users/bob", new { IsEnabled = true });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveUserOverride_ReturnsNoContent()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "user-rm", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/user-rm/users/bob", new { IsEnabled = true });

        var response = await _client.DeleteAsync("/api/flags/user-rm/users/bob");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveUserOverride_NonExistent_ReturnsNotFound()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "user-rm-404", IsEnabled = false });

        var response = await _client.DeleteAsync("/api/flags/user-rm-404/users/ghost");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- Group override CRUD ----

    [Fact]
    public async Task SetGroupOverride_ReturnsNoContent()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "grp-ov", IsEnabled = false });

        var response = await _client.PutAsJsonAsync("/api/flags/grp-ov/groups/admins", new { IsEnabled = true });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveGroupOverride_ReturnsNoContent()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "grp-rm", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/grp-rm/groups/admins", new { IsEnabled = true });

        var response = await _client.DeleteAsync("/api/flags/grp-rm/groups/admins");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveGroupOverride_NonExistent_ReturnsNotFound()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "grp-rm-404", IsEnabled = false });

        var response = await _client.DeleteAsync("/api/flags/grp-rm-404/groups/nope");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- Region override CRUD ----

    [Fact]
    public async Task SetRegionOverride_ReturnsNoContent()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "reg-ov", IsEnabled = false });

        var response = await _client.PutAsJsonAsync("/api/flags/reg-ov/regions/eu-west", new { IsEnabled = true });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveRegionOverride_ReturnsNoContent()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "reg-rm", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/reg-rm/regions/eu-west", new { IsEnabled = true });

        var response = await _client.DeleteAsync("/api/flags/reg-rm/regions/eu-west");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveRegionOverride_NonExistent_ReturnsNotFound()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "reg-rm-404", IsEnabled = false });

        var response = await _client.DeleteAsync("/api/flags/reg-rm-404/regions/nope");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Evaluate_RegionOverride_WhenNoUserOrGroupOverride()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "eval-region", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/eval-region/regions/eu-west", new { IsEnabled = true });

        var response = await _client.PostAsJsonAsync("/api/flags/eval-region/evaluate",
            new { RegionId = "eu-west" });

        var body = await response.Content.ReadFromJsonAsync<EvalDto>();
        body!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_GroupOverride_BeatsRegionOverride()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "eval-grp-reg", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/eval-grp-reg/regions/eu-west", new { IsEnabled = true });
        await _client.PutAsJsonAsync("/api/flags/eval-grp-reg/groups/beta", new { IsEnabled = false });

        var response = await _client.PostAsJsonAsync("/api/flags/eval-grp-reg/evaluate",
            new { GroupIds = new[] { "beta" }, RegionId = "eu-west" });

        var body = await response.Content.ReadFromJsonAsync<EvalDto>();
        body!.IsEnabled.Should().BeFalse();
    }

    // ---- Update existing overrides (covers the "update" branch in model) ----

    [Fact]
    public async Task UpdateUserOverride_ChangesValue()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "upd-user", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/upd-user/users/alice", new { IsEnabled = true });

        await _client.PutAsJsonAsync("/api/flags/upd-user/users/alice", new { IsEnabled = false });

        var response = await _client.PostAsJsonAsync("/api/flags/upd-user/evaluate",
            new { UserId = "alice" });
        var evalBody = await response.Content.ReadFromJsonAsync<EvalDto>();
        evalBody!.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateGroupOverride_ChangesValue()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "upd-grp", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/upd-grp/groups/beta", new { IsEnabled = true });

        await _client.PutAsJsonAsync("/api/flags/upd-grp/groups/beta", new { IsEnabled = false });

        var response = await _client.PostAsJsonAsync("/api/flags/upd-grp/evaluate",
            new { GroupIds = new[] { "beta" } });
        var evalBody = await response.Content.ReadFromJsonAsync<EvalDto>();
        evalBody!.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateRegionOverride_ChangesValue()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "upd-reg", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/upd-reg/regions/eu-west", new { IsEnabled = true });

        await _client.PutAsJsonAsync("/api/flags/upd-reg/regions/eu-west", new { IsEnabled = false });

        var response = await _client.PostAsJsonAsync("/api/flags/upd-reg/evaluate",
            new { RegionId = "eu-west" });
        var evalBody = await response.Content.ReadFromJsonAsync<EvalDto>();
        evalBody!.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Evaluate_FullPrecedence_UserGroupRegionGlobal()
    {
        await _client.PostAsJsonAsync("/api/flags", new { Name = "full-prec", IsEnabled = false });
        await _client.PutAsJsonAsync("/api/flags/full-prec/regions/eu-west", new { IsEnabled = true });
        await _client.PutAsJsonAsync("/api/flags/full-prec/groups/beta", new { IsEnabled = true });
        await _client.PutAsJsonAsync("/api/flags/full-prec/users/alice", new { IsEnabled = false });

        var r1 = await _client.PostAsJsonAsync("/api/flags/full-prec/evaluate",
            new { UserId = "alice", GroupIds = new[] { "beta" }, RegionId = "eu-west" });
        (await r1.Content.ReadFromJsonAsync<EvalDto>())!.IsEnabled.Should().BeFalse();

        var r2 = await _client.PostAsJsonAsync("/api/flags/full-prec/evaluate",
            new { UserId = "bob", GroupIds = new[] { "beta" }, RegionId = "eu-west" });
        (await r2.Content.ReadFromJsonAsync<EvalDto>())!.IsEnabled.Should().BeTrue();

        var r3 = await _client.PostAsJsonAsync("/api/flags/full-prec/evaluate",
            new { UserId = "charlie", RegionId = "eu-west" });
        (await r3.Content.ReadFromJsonAsync<EvalDto>())!.IsEnabled.Should().BeTrue();

        var r4 = await _client.PostAsJsonAsync("/api/flags/full-prec/evaluate",
            new { UserId = "dave", RegionId = "ap-south" });
        (await r4.Content.ReadFromJsonAsync<EvalDto>())!.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateGlobalState_NonExistent_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/flags/nope-flag", new { IsEnabled = true });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetUserOverride_NonExistentFlag_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/flags/nope-flag/users/alice", new { IsEnabled = true });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetGroupOverride_NonExistentFlag_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/flags/nope-flag/groups/beta", new { IsEnabled = true });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetRegionOverride_NonExistentFlag_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/flags/nope-flag/regions/eu", new { IsEnabled = true });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- DTOs for deserialization ----

    private record FlagDto(string Name, bool IsEnabled, string? Description);
    private record EvalDto(string FlagName, bool IsEnabled);
}
