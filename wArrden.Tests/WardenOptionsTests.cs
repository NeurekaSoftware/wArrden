using wArrden.Configuration;

namespace wArrden.Tests;

public class WardenOptionsTests
{
    [Fact]
    public void HasSonarr_WhenUrlAndApiKeySet_ReturnsTrue()
    {
        var opts = new WardenOptions
        {
            SonarrUrl = "http://localhost:8989",
            SonarrApiKey = "abc123"
        };
        Assert.True(opts.HasSonarr);
    }

    [Fact]
    public void HasSonarr_WhenUrlMissing_ReturnsFalse()
    {
        var opts = new WardenOptions
        {
            SonarrApiKey = "abc123"
        };
        Assert.False(opts.HasSonarr);
    }

    [Fact]
    public void HasSonarr_WhenApiKeyMissing_ReturnsFalse()
    {
        var opts = new WardenOptions
        {
            SonarrUrl = "http://localhost:8989"
        };
        Assert.False(opts.HasSonarr);
    }

    [Fact]
    public void HasRadarr_WhenUrlAndApiKeySet_ReturnsTrue()
    {
        var opts = new WardenOptions
        {
            RadarrUrl = "http://localhost:7878",
            RadarrApiKey = "abc123"
        };
        Assert.True(opts.HasRadarr);
    }

    [Fact]
    public void HasRadarr_WhenUrlMissing_ReturnsFalse()
    {
        var opts = new WardenOptions
        {
            RadarrApiKey = "abc123"
        };
        Assert.False(opts.HasRadarr);
    }

    [Fact]
    public void IsDryRun_WhenSetToTrue_ReturnsTrue()
    {
        var opts = new WardenOptions { DryRun = "true" };
        Assert.True(opts.IsDryRun);
    }

    [Fact]
    public void IsDryRun_WhenSetToAnythingElse_ReturnsFalse()
    {
        var opts = new WardenOptions { DryRun = "false" };
        Assert.False(opts.IsDryRun);
    }

    [Fact]
    public void IsDryRun_WhenNull_ReturnsFalse()
    {
        var opts = new WardenOptions { DryRun = null };
        Assert.False(opts.IsDryRun);
    }

    [Fact]
    public void IsDryRun_CaseInsensitive_ReturnsTrue()
    {
        var opts = new WardenOptions { DryRun = "TRUE" };
        Assert.True(opts.IsDryRun);
    }

    [Fact]
    public void Cooldown_DefaultValuesAre30Days()
    {
        var opts = new WardenOptions();
        Assert.Equal(TimeSpan.FromDays(30), opts.SonarrMissingCooldown);
        Assert.Equal(TimeSpan.FromDays(30), opts.SonarrUpgradeCooldown);
        Assert.Equal(TimeSpan.FromDays(30), opts.RadarrMissingCooldown);
        Assert.Equal(TimeSpan.FromDays(30), opts.RadarrUpgradeCooldown);
    }

    [Fact]
    public void Cooldown_CustomRawValuesAreParsed()
    {
        var opts = new WardenOptions
        {
            SonarrMissingCooldownRaw = "7d",
            SonarrUpgradeCooldownRaw = "12h",
            RadarrMissingCooldownRaw = "90m",
            RadarrUpgradeCooldownRaw = "300s"
        };
        Assert.Equal(TimeSpan.FromDays(7), opts.SonarrMissingCooldown);
        Assert.Equal(TimeSpan.FromHours(12), opts.SonarrUpgradeCooldown);
        Assert.Equal(TimeSpan.FromMinutes(90), opts.RadarrMissingCooldown);
        Assert.Equal(TimeSpan.FromSeconds(300), opts.RadarrUpgradeCooldown);
    }

    [Fact]
    public void MaxResults_DefaultValues()
    {
        var opts = new WardenOptions();
        Assert.Equal(100, opts.SonarrMissingMaxResults);
        Assert.Equal(50, opts.SonarrUpgradeMaxResults);
        Assert.Equal(100, opts.RadarrMissingMaxResults);
        Assert.Equal(50, opts.RadarrUpgradeMaxResults);
    }
}
