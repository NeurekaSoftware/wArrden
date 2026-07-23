using wArrden.Services;

namespace wArrden.Tests;

public class InstanceHealthTrackerTests
{
    [Fact]
    public void NewInstance_IsEnabled()
    {
        var tracker = new InstanceHealthTracker();
        Assert.True(tracker.IsEnabled("music"));
    }

    [Fact]
    public void Disable_MakesInstanceDisabled()
    {
        var tracker = new InstanceHealthTracker();
        tracker.Disable("music", "authentication failed");
        Assert.False(tracker.IsEnabled("music"));
    }

    [Fact]
    public void Disable_ReturnsTrueOnFirstCall_FalseThereafter()
    {
        var tracker = new InstanceHealthTracker();
        Assert.True(tracker.Disable("music", "first"));
        Assert.False(tracker.Disable("music", "second"));
    }

    [Fact]
    public void Disable_IsCaseInsensitive()
    {
        var tracker = new InstanceHealthTracker();
        tracker.Disable("Music", "auth");
        Assert.False(tracker.IsEnabled("music"));
    }

    [Fact]
    public void Disable_OneInstance_DoesNotAffectOthers()
    {
        var tracker = new InstanceHealthTracker();
        tracker.Disable("music", "auth");
        Assert.False(tracker.IsEnabled("music"));
        Assert.True(tracker.IsEnabled("series"));
        Assert.True(tracker.IsEnabled("movies"));
    }

    [Fact]
    public void GetDisableReason_ReturnsReason_WhenDisabled()
    {
        var tracker = new InstanceHealthTracker();
        tracker.Disable("music", "API key rejected (401 Unauthorized)");
        Assert.Equal("API key rejected (401 Unauthorized)", tracker.GetDisableReason("music"));
    }

    [Fact]
    public void GetDisableReason_IsCaseInsensitive()
    {
        var tracker = new InstanceHealthTracker();
        tracker.Disable("Music", "auth");
        Assert.Equal("auth", tracker.GetDisableReason("music"));
    }

    [Fact]
    public void GetDisableReason_ReturnsNull_WhenEnabled()
    {
        var tracker = new InstanceHealthTracker();
        Assert.Null(tracker.GetDisableReason("music"));
    }
}
