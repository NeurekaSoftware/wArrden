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
    public void JobConfig_Defaults()
    {
        var job = new JobConfig();
        Assert.False(job.Enabled);
        Assert.Equal("", job.Cron);
        Assert.Equal(0, job.MaxResults);
        Assert.Equal("30d", job.Cooldown);
    }
}
