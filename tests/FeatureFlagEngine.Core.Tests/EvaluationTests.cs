using FeatureFlagEngine.Core.Models;
using FeatureFlagEngine.Core.Services;
using FluentAssertions;

namespace FeatureFlagEngine.Core.Tests;

public class EvaluationTests
{
    [Fact]
    public void Evaluate_NoOverrides_ReturnsGlobalDefault_WhenEnabled()
    {
        var flag = new FeatureFlag("notifications", true);

        var result = FeatureFlagService.Evaluate(flag);

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_NoOverrides_ReturnsGlobalDefault_WhenDisabled()
    {
        var flag = new FeatureFlag("notifications", false);

        var result = FeatureFlagService.Evaluate(flag);

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_UserOverrideEnabled_OverridesDisabledGlobal()
    {
        var flag = new FeatureFlag("dark-mode", false);
        flag.SetUserOverride("alice", true);

        var result = FeatureFlagService.Evaluate(flag, userId: "alice");

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_UserOverrideDisabled_OverridesEnabledGlobal()
    {
        var flag = new FeatureFlag("dark-mode", true);
        flag.SetUserOverride("bob", false);

        var result = FeatureFlagService.Evaluate(flag, userId: "bob");

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_GroupOverrideEnabled_OverridesDisabledGlobal()
    {
        var flag = new FeatureFlag("new-dashboard", false);
        flag.SetGroupOverride("beta-testers", true);

        var result = FeatureFlagService.Evaluate(flag, groupIds: new[] { "beta-testers" });

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_UserOverride_TakesPrecedenceOverGroupOverride()
    {
        var flag = new FeatureFlag("experimental", false);
        flag.SetGroupOverride("beta-testers", true);
        flag.SetUserOverride("alice", false);

        // Alice has a user override (disabled), even though her group says enabled
        var result = FeatureFlagService.Evaluate(flag, userId: "alice", groupIds: new[] { "beta-testers" });

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_UserWithNoOverride_FallsBackToGroup()
    {
        var flag = new FeatureFlag("experimental", false);
        flag.SetGroupOverride("beta-testers", true);
        flag.SetUserOverride("alice", true); // alice has override, but we're checking bob

        var result = FeatureFlagService.Evaluate(flag, userId: "bob", groupIds: new[] { "beta-testers" });

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_UserNotInAnyOverride_FallsBackToGlobal()
    {
        var flag = new FeatureFlag("experimental", false);
        flag.SetGroupOverride("admins", true);
        flag.SetUserOverride("alice", true);

        // charlie has no user override and isn't in any overridden group
        var result = FeatureFlagService.Evaluate(flag, userId: "charlie", groupIds: new[] { "regular-users" });

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_MultipleGroups_FirstMatchWins()
    {
        var flag = new FeatureFlag("feature-y", false);
        flag.SetGroupOverride("alpha", true);
        flag.SetGroupOverride("beta", false);

        // user is in both alpha and beta — alpha comes first, so it should win
        var result = FeatureFlagService.Evaluate(flag, groupIds: new[] { "alpha", "beta" });

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_MultipleGroups_OnlySecondMatches()
    {
        var flag = new FeatureFlag("feature-y", true);
        flag.SetGroupOverride("beta", false);

        // user is in "alpha" (no override) and "beta" (disabled)
        var result = FeatureFlagService.Evaluate(flag, groupIds: new[] { "alpha", "beta" });

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NullUserId_SkipsUserOverride()
    {
        var flag = new FeatureFlag("feature-z", false);
        flag.SetUserOverride("alice", true);

        var result = FeatureFlagService.Evaluate(flag, userId: null);

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_EmptyGroupList_SkipsGroupOverride()
    {
        var flag = new FeatureFlag("feature-z", false);
        flag.SetGroupOverride("admins", true);

        var result = FeatureFlagService.Evaluate(flag, groupIds: Array.Empty<string>());

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NullGroupList_SkipsGroupOverride()
    {
        var flag = new FeatureFlag("feature-z", false);
        flag.SetGroupOverride("admins", true);

        var result = FeatureFlagService.Evaluate(flag, groupIds: null);

        result.Should().BeFalse();
    }

    // --- Region Override Evaluation Tests ---

    [Fact]
    public void Evaluate_RegionOverrideEnabled_OverridesDisabledGlobal()
    {
        var flag = new FeatureFlag("feature-r", false);
        flag.SetRegionOverride("eu-west", true);

        var result = FeatureFlagService.Evaluate(flag, regionId: "eu-west");

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_RegionOverrideDisabled_OverridesEnabledGlobal()
    {
        var flag = new FeatureFlag("feature-r", true);
        flag.SetRegionOverride("us-east", false);

        var result = FeatureFlagService.Evaluate(flag, regionId: "us-east");

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_GroupOverride_TakesPrecedenceOverRegion()
    {
        var flag = new FeatureFlag("feature-r", false);
        flag.SetRegionOverride("eu-west", true);
        flag.SetGroupOverride("beta", false);

        // Group override (false) should beat region override (true)
        var result = FeatureFlagService.Evaluate(flag, groupIds: new[] { "beta" }, regionId: "eu-west");

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_UserOverride_TakesPrecedenceOverRegion()
    {
        var flag = new FeatureFlag("feature-r", false);
        flag.SetRegionOverride("eu-west", true);
        flag.SetUserOverride("alice", false);

        var result = FeatureFlagService.Evaluate(flag, userId: "alice", regionId: "eu-west");

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_RegionNoMatch_FallsBackToGlobal()
    {
        var flag = new FeatureFlag("feature-r", true);
        flag.SetRegionOverride("eu-west", false);

        // User is in "us-east", no override for that region
        var result = FeatureFlagService.Evaluate(flag, regionId: "us-east");

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_FullPrecedenceChain_UserGroupRegionGlobal()
    {
        var flag = new FeatureFlag("full-chain", false);
        flag.SetRegionOverride("eu-west", true);
        flag.SetGroupOverride("beta", true);
        flag.SetUserOverride("alice", false);

        // alice: user override (false) wins over all
        FeatureFlagService.Evaluate(flag, userId: "alice", groupIds: new[] { "beta" }, regionId: "eu-west")
            .Should().BeFalse();

        // bob in beta: group override (true) wins over region
        FeatureFlagService.Evaluate(flag, userId: "bob", groupIds: new[] { "beta" }, regionId: "eu-west")
            .Should().BeTrue();

        // charlie in eu-west, no group: region override (true) wins over global
        FeatureFlagService.Evaluate(flag, userId: "charlie", regionId: "eu-west")
            .Should().BeTrue();

        // dave, no overrides at all: global (false)
        FeatureFlagService.Evaluate(flag, userId: "dave", regionId: "ap-south")
            .Should().BeFalse();
    }
}
