using wArrden.Configuration;

namespace wArrden.Tests;

public class DurationParserTests
{
    [Theory]
    [InlineData("30d", 30, 0, 0, 0)]
    [InlineData("12h", 0, 12, 0, 0)]
    [InlineData("90m", 0, 0, 90, 0)]
    [InlineData("45s", 0, 0, 0, 45)]
    [InlineData("1h30m", 0, 1, 30, 0)]
    [InlineData("2d12h30m45s", 2, 12, 30, 45)]
    [InlineData("7d", 7, 0, 0, 0)]
    [InlineData("100s", 0, 0, 0, 100)]
    public void Parse_ValidDurations(string input, int days, int hours, int minutes, int seconds)
    {
        var result = DurationParser.Parse(input);
        Assert.Equal(new TimeSpan(days, hours, minutes, seconds), result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => DurationParser.Parse(input));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("30x")]
    [InlineData("12")]
    [InlineData("1y")]
    public void Parse_InvalidFormat_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => DurationParser.Parse(input));
    }

    [Fact]
    public void Parse_ZeroResult_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => DurationParser.Parse("0d"));
    }

    [Fact]
    public void Parse_Null_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => DurationParser.Parse(null!));
    }

    [Theory]
    [InlineData("30D")]
    [InlineData("12H")]
    [InlineData("90M")]
    [InlineData("45S")]
    public void Parse_CaseInsensitiveUnits(string input)
    {
        var result = DurationParser.Parse(input);
        Assert.True(result > TimeSpan.Zero);
    }

    [Theory]
    [InlineData("30 d", 30, 0, 0, 0)]
    [InlineData("1 h 30 m", 0, 1, 30, 0)]
    public void Parse_WhitespaceBetweenNumberAndUnit(string input, int d, int h, int m, int s)
    {
        var result = DurationParser.Parse(input);
        Assert.Equal(new TimeSpan(d, h, m, s), result);
    }

    [Fact]
    public void Parse_MultipleSameUnit_SumsThem()
    {
        var result = DurationParser.Parse("1d2d");
        Assert.Equal(TimeSpan.FromDays(3), result);
    }
}
