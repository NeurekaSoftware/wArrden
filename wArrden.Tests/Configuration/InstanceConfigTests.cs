using wArrden.Configuration;

namespace wArrden.Tests;

public class InstanceConfigTests
{
    [Fact]
    public void InstanceKey_LowercasesName()
    {
        var inst = new InstanceConfig { Name = "My Sonarr" };
        Assert.Equal("my sonarr", inst.InstanceKey);
    }

    [Fact]
    public void IsSonarr_CaseInsensitive()
    {
        var inst = new InstanceConfig { Type = "SONARR" };
        Assert.True(inst.IsSonarr);
        Assert.False(inst.IsRadarr);
    }

    [Fact]
    public void IsRadarr_ReturnsTrueForCorrectType()
    {
        var inst = new InstanceConfig { Type = "radarr" };
        Assert.True(inst.IsRadarr);
        Assert.False(inst.IsSonarr);
    }

    [Fact]
    public void IsSonarr_ReturnsFalseForRadarr()
    {
        var inst = new InstanceConfig { Type = "radarr" };
        Assert.False(inst.IsSonarr);
    }

    [Fact]
    public void IsSonarr_NullType_ReturnsFalse()
    {
        var inst = new InstanceConfig { Type = null! };
        Assert.False(inst.IsSonarr);
    }

    [Fact]
    public void IsRadarr_NullType_ReturnsFalse()
    {
        var inst = new InstanceConfig { Type = null! };
        Assert.False(inst.IsRadarr);
    }

    [Fact]
    public void IsLidarr_CaseInsensitive()
    {
        var inst = new InstanceConfig { Type = "LIDARR" };
        Assert.True(inst.IsLidarr);
        Assert.False(inst.IsSonarr);
        Assert.False(inst.IsRadarr);
        Assert.False(inst.IsWhisparr);
    }

    [Fact]
    public void IsLidarr_ReturnsTrueForCorrectType()
    {
        var inst = new InstanceConfig { Type = "lidarr" };
        Assert.True(inst.IsLidarr);
    }

    [Fact]
    public void IsLidarr_NullType_ReturnsFalse()
    {
        var inst = new InstanceConfig { Type = null! };
        Assert.False(inst.IsLidarr);
    }

    [Fact]
    public void IsWhisparr_CaseInsensitive()
    {
        var inst = new InstanceConfig { Type = "WHISPARR" };
        Assert.True(inst.IsWhisparr);
        Assert.False(inst.IsSonarr);
        Assert.False(inst.IsRadarr);
        Assert.False(inst.IsLidarr);
    }

    [Fact]
    public void IsWhisparr_ReturnsTrueForCorrectType()
    {
        var inst = new InstanceConfig { Type = "whisparr" };
        Assert.True(inst.IsWhisparr);
    }

    [Fact]
    public void IsWhisparr_NullType_ReturnsFalse()
    {
        var inst = new InstanceConfig { Type = null! };
        Assert.False(inst.IsWhisparr);
    }

    [Fact]
    public void JobConfig_Defaults()
    {
        var job = new JobConfig();
        Assert.Null(job.Enabled);
        Assert.Null(job.Cron);
        Assert.Null(job.MaxResults);
        Assert.Null(job.Cooldown);
    }
}
