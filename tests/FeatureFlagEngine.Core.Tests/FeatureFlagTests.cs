using FeatureFlagEngine.Core.Models;
using FluentAssertions;

namespace FeatureFlagEngine.Core.Tests;

public class FeatureFlagTests
{
    [Fact]
    public void Constructor_WithValidName_CreatesFlag()
    {
        var flag = new FeatureFlag("dark-mode", true, "Enable dark theme");

        flag.Name.Should().Be("dark-mode");
        flag.IsEnabled.Should().BeTrue();
        flag.Description.Should().Be("Enable dark theme");
    }

    [Fact]
    public void Constructor_TrimsWhitespaceFromName()
    {
        var flag = new FeatureFlag("  dark-mode  ", false);

        flag.Name.Should().Be("dark-mode");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string? name)
    {
        var act = () => new FeatureFlag(name!, false);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*cannot be empty*");
    }

    [Fact]
    public void SetUserOverride_AddsNewOverride()
    {
        var flag = new FeatureFlag("feature-x", false);

        flag.SetUserOverride("alice", true);

        flag.UserOverrides.Should().ContainSingle()
            .Which.UserId.Should().Be("alice");
    }

    [Fact]
    public void SetUserOverride_UpdatesExistingOverride()
    {
        var flag = new FeatureFlag("feature-x", false);
        flag.SetUserOverride("alice", true);

        flag.SetUserOverride("alice", false);

        flag.UserOverrides.Should().ContainSingle();
        flag.UserOverrides.First().IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SetUserOverride_WithEmptyUserId_Throws()
    {
        var flag = new FeatureFlag("feature-x", false);

        var act = () => flag.SetUserOverride("", true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveUserOverride_RemovesExisting_ReturnsTrue()
    {
        var flag = new FeatureFlag("feature-x", false);
        flag.SetUserOverride("alice", true);

        var removed = flag.RemoveUserOverride("alice");

        removed.Should().BeTrue();
        flag.UserOverrides.Should().BeEmpty();
    }

    [Fact]
    public void RemoveUserOverride_NonExistent_ReturnsFalse()
    {
        var flag = new FeatureFlag("feature-x", false);

        var removed = flag.RemoveUserOverride("ghost");

        removed.Should().BeFalse();
    }

    [Fact]
    public void SetGroupOverride_AddsNewOverride()
    {
        var flag = new FeatureFlag("feature-x", false);

        flag.SetGroupOverride("beta-testers", true);

        flag.GroupOverrides.Should().ContainSingle()
            .Which.GroupId.Should().Be("beta-testers");
    }

    [Fact]
    public void SetGroupOverride_UpdatesExistingOverride()
    {
        var flag = new FeatureFlag("feature-x", true);
        flag.SetGroupOverride("admins", false);

        flag.SetGroupOverride("admins", true);

        flag.GroupOverrides.Should().ContainSingle();
        flag.GroupOverrides.First().IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void RemoveGroupOverride_RemovesExisting_ReturnsTrue()
    {
        var flag = new FeatureFlag("feature-x", false);
        flag.SetGroupOverride("admins", true);

        var removed = flag.RemoveGroupOverride("admins");

        removed.Should().BeTrue();
        flag.GroupOverrides.Should().BeEmpty();
    }

    [Fact]
    public void RemoveGroupOverride_NonExistent_ReturnsFalse()
    {
        var flag = new FeatureFlag("feature-x", false);

        flag.RemoveGroupOverride("nope").Should().BeFalse();
    }

    // --- Region Override Tests ---

    [Fact]
    public void SetRegionOverride_AddsNewOverride()
    {
        var flag = new FeatureFlag("feature-x", false);

        flag.SetRegionOverride("eu-west", true);

        flag.RegionOverrides.Should().ContainSingle()
            .Which.RegionId.Should().Be("eu-west");
    }

    [Fact]
    public void SetRegionOverride_UpdatesExistingOverride()
    {
        var flag = new FeatureFlag("feature-x", false);
        flag.SetRegionOverride("eu-west", true);

        flag.SetRegionOverride("eu-west", false);

        flag.RegionOverrides.Should().ContainSingle();
        flag.RegionOverrides.First().IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SetRegionOverride_WithEmptyRegionId_Throws()
    {
        var flag = new FeatureFlag("feature-x", false);

        var act = () => flag.SetRegionOverride("", true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveRegionOverride_RemovesExisting_ReturnsTrue()
    {
        var flag = new FeatureFlag("feature-x", false);
        flag.SetRegionOverride("us-east", true);

        var removed = flag.RemoveRegionOverride("us-east");

        removed.Should().BeTrue();
        flag.RegionOverrides.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRegionOverride_NonExistent_ReturnsFalse()
    {
        var flag = new FeatureFlag("feature-x", false);

        flag.RemoveRegionOverride("nope").Should().BeFalse();
    }
}
