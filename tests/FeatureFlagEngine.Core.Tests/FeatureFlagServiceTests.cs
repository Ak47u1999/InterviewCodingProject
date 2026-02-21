using FeatureFlagEngine.Core.Services;
using FeatureFlagEngine.Core.Tests.Fakes;
using FluentAssertions;

namespace FeatureFlagEngine.Core.Tests;

public class FeatureFlagServiceTests
{
    private readonly InMemoryFeatureFlagRepository _repository;
    private readonly FeatureFlagService _service;

    public FeatureFlagServiceTests()
    {
        _repository = new InMemoryFeatureFlagRepository();
        _service = new FeatureFlagService(_repository);
    }

    [Fact]
    public async Task CreateFlag_SetsUpNewFlag()
    {
        var flag = await _service.CreateFlagAsync("dark-mode", false, "Dark theme toggle");

        flag.Name.Should().Be("dark-mode");
        flag.IsEnabled.Should().BeFalse();
        flag.Description.Should().Be("Dark theme toggle");
    }

    [Fact]
    public async Task CreateFlag_DuplicateName_Throws()
    {
        await _service.CreateFlagAsync("dark-mode", false);

        var act = () => _service.CreateFlagAsync("dark-mode", true);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task GetFlag_ExistingFlag_ReturnsIt()
    {
        await _service.CreateFlagAsync("dark-mode", true);

        var flag = await _service.GetFlagAsync("dark-mode");

        flag.Name.Should().Be("dark-mode");
    }

    [Fact]
    public async Task GetFlag_NonExistent_Throws()
    {
        var act = () => _service.GetFlagAsync("ghost-flag");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetAllFlags_ReturnsAllCreatedFlags()
    {
        await _service.CreateFlagAsync("flag-a", true);
        await _service.CreateFlagAsync("flag-b", false);

        var all = await _service.GetAllFlagsAsync();

        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateGlobalState_ChangesState()
    {
        await _service.CreateFlagAsync("dark-mode", false);

        await _service.UpdateGlobalStateAsync("dark-mode", true);

        var flag = await _service.GetFlagAsync("dark-mode");
        flag.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task SetUserOverride_AddsOverride()
    {
        await _service.CreateFlagAsync("dark-mode", false);

        await _service.SetUserOverrideAsync("dark-mode", "alice", true);

        var result = await _service.EvaluateAsync("dark-mode", userId: "alice");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserOverride_RemovesIt()
    {
        await _service.CreateFlagAsync("dark-mode", false);
        await _service.SetUserOverrideAsync("dark-mode", "alice", true);

        await _service.RemoveUserOverrideAsync("dark-mode", "alice");

        var result = await _service.EvaluateAsync("dark-mode", userId: "alice");
        result.Should().BeFalse(); // falls back to global default
    }

    [Fact]
    public async Task RemoveUserOverride_NonExistent_Throws()
    {
        await _service.CreateFlagAsync("dark-mode", false);

        var act = () => _service.RemoveUserOverrideAsync("dark-mode", "ghost");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SetGroupOverride_AddsOverride()
    {
        await _service.CreateFlagAsync("dark-mode", false);

        await _service.SetGroupOverrideAsync("dark-mode", "beta-testers", true);

        var result = await _service.EvaluateAsync("dark-mode", groupIds: new[] { "beta-testers" });
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveGroupOverride_RemovesIt()
    {
        await _service.CreateFlagAsync("dark-mode", false);
        await _service.SetGroupOverrideAsync("dark-mode", "beta-testers", true);

        await _service.RemoveGroupOverrideAsync("dark-mode", "beta-testers");

        var result = await _service.EvaluateAsync("dark-mode", groupIds: new[] { "beta-testers" });
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveGroupOverride_NonExistent_Throws()
    {
        await _service.CreateFlagAsync("dark-mode", false);

        var act = () => _service.RemoveGroupOverrideAsync("dark-mode", "nope");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteFlag_RemovesIt()
    {
        await _service.CreateFlagAsync("temp-flag", true);

        await _service.DeleteFlagAsync("temp-flag");

        var act = () => _service.GetFlagAsync("temp-flag");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteFlag_NonExistent_Throws()
    {
        var act = () => _service.DeleteFlagAsync("nope");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task EvaluateAsync_NonExistentFlag_Throws()
    {
        var act = () => _service.EvaluateAsync("missing-flag");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // --- Region Override Service Tests ---

    [Fact]
    public async Task SetRegionOverride_AddsOverride()
    {
        await _service.CreateFlagAsync("dark-mode", false);

        await _service.SetRegionOverrideAsync("dark-mode", "eu-west", true);

        var result = await _service.EvaluateAsync("dark-mode", regionId: "eu-west");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveRegionOverride_RemovesIt()
    {
        await _service.CreateFlagAsync("dark-mode", false);
        await _service.SetRegionOverrideAsync("dark-mode", "eu-west", true);

        await _service.RemoveRegionOverrideAsync("dark-mode", "eu-west");

        var result = await _service.EvaluateAsync("dark-mode", regionId: "eu-west");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveRegionOverride_NonExistent_Throws()
    {
        await _service.CreateFlagAsync("dark-mode", false);

        var act = () => _service.RemoveRegionOverrideAsync("dark-mode", "nope");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Evaluate_FullPrecedenceChain()
    {
        // Set up a flag with overrides at every level
        await _service.CreateFlagAsync("checkout-v2", false);
        await _service.SetGroupOverrideAsync("checkout-v2", "beta", true);
        await _service.SetUserOverrideAsync("checkout-v2", "alice", false);

        // alice: user override (false) beats group override (true)
        var aliceResult = await _service.EvaluateAsync("checkout-v2", "alice", new[] { "beta" });
        aliceResult.Should().BeFalse();

        // bob in beta: group override (true) beats global (false)
        var bobResult = await _service.EvaluateAsync("checkout-v2", "bob", new[] { "beta" });
        bobResult.Should().BeTrue();

        // charlie: no overrides, gets global default (false)
        var charlieResult = await _service.EvaluateAsync("checkout-v2", "charlie");
        charlieResult.Should().BeFalse();
    }
}
