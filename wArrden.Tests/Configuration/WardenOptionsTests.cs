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

    [Fact]
    public void HttpRetryCountValue_WhenUnset_ReturnsDefault()
    {
        var opts = new WardenOptions();
        Assert.Equal(WardenOptions.DefaultHttpRetryCount, opts.HttpRetryCountValue);
    }

    [Fact]
    public void HttpRetryCountValue_WhenSet_IsParsed()
    {
        var opts = new WardenOptions { HttpRetryCount = "5" };
        Assert.Equal(5, opts.HttpRetryCountValue);
    }

    [Fact]
    public void HttpRetryCountValue_AllowsZero()
    {
        var opts = new WardenOptions { HttpRetryCount = "0" };
        Assert.Equal(0, opts.HttpRetryCountValue);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("")]
    public void HttpRetryCountValue_WhenInvalid_ReturnsDefault(string raw)
    {
        var opts = new WardenOptions { HttpRetryCount = raw };
        Assert.Equal(WardenOptions.DefaultHttpRetryCount, opts.HttpRetryCountValue);
    }

    [Fact]
    public void HttpTimeoutSecondsValue_WhenUnset_ReturnsDefault()
    {
        var opts = new WardenOptions();
        Assert.Equal(WardenOptions.DefaultHttpTimeoutSeconds, opts.HttpTimeoutSecondsValue);
    }

    [Fact]
    public void HttpTimeoutSecondsValue_WhenSet_IsParsed()
    {
        var opts = new WardenOptions { HttpTimeoutSeconds = "45" };
        Assert.Equal(45, opts.HttpTimeoutSecondsValue);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("nope")]
    public void HttpTimeoutSecondsValue_WhenInvalidOrNonPositive_ReturnsDefault(string raw)
    {
        var opts = new WardenOptions { HttpTimeoutSeconds = raw };
        Assert.Equal(WardenOptions.DefaultHttpTimeoutSeconds, opts.HttpTimeoutSecondsValue);
    }
}
