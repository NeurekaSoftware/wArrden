using wArrden.Configuration;

namespace wArrden.Tests;

public class WardenOptionsTests
{
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
    public void DatabasePath_HasDefaultValue()
    {
        var opts = new WardenOptions();
        Assert.Equal("data/warden.db", opts.DatabasePath);
    }

    [Fact]
    public void Timezone_DefaultIsNull()
    {
        var opts = new WardenOptions();
        Assert.Null(opts.Timezone);
    }
}
