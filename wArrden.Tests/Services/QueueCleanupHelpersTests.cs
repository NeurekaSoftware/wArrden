using wArrden.Clients.Models;
using wArrden.Services;

namespace wArrden.Tests;

public class QueueCleanupHelpersTests
{
    [Fact]
    public void CollectMessages_ErrorMessageOnly_ReturnsRawMessage()
    {
        var item = new QueueResource
        {
            ErrorMessage = "Download failed",
            StatusMessages = null
        };

        var result = QueueCleanupService.CollectMessages(item);
        Assert.Equal("Download failed", result);
    }

    [Fact]
    public void CollectMessages_StatusMessages_ConcatenatesAll()
    {
        var item = new QueueResource
        {
            ErrorMessage = "Error",
            StatusMessages = new List<QueueStatusMessage>
            {
                new() { Title = "msg1", Messages = new List<string> { "Message A", "Message B" } },
                new() { Title = "msg2", Messages = new List<string> { "Message C" } }
            }
        };

        var result = QueueCleanupService.CollectMessages(item);
        Assert.Equal("Error Message A Message B Message C", result);
    }

    [Fact]
    public void CollectMessages_NullMessages_ReturnsEmptyString()
    {
        var item = new QueueResource
        {
            ErrorMessage = null,
            StatusMessages = null
        };

        var result = QueueCleanupService.CollectMessages(item);
        Assert.Equal("", result);
    }

    [Fact]
    public void CollectMessages_NullMessagesList_ReturnsOnlyError()
    {
        var item = new QueueResource
        {
            ErrorMessage = "Err",
            StatusMessages = new List<QueueStatusMessage>
            {
                new() { Title = "msg", Messages = null }
            }
        };

        var result = QueueCleanupService.CollectMessages(item);
        Assert.Equal("Err", result);
    }

    [Fact]
    public void MatchRule_CaseInsensitive_FindsMatch()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("Not an upgrade for existing episode", false)
        };

        var result = QueueCleanupService.MatchRule("This is NOT AN UPGRADE for existing episode here", rules);
        Assert.NotNull(result);
        Assert.Equal("Not an upgrade for existing episode", result.Value.Label);
        Assert.False(result.Value.Blocklist);
    }

    [Fact]
    public void MatchRule_BlocklistRule_FindsMatch()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("Sample", true)
        };

        var result = QueueCleanupService.MatchRule("This is a Sample file", rules);
        Assert.NotNull(result);
        Assert.Equal("Sample", result.Value.Label);
        Assert.True(result.Value.Blocklist);
    }

    [Fact]
    public void MatchRule_NoMatch_ReturnsNull()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("Not an upgrade for existing episode", false)
        };

        var result = QueueCleanupService.MatchRule("Download completed successfully", rules);
        Assert.Null(result);
    }

    [Fact]
    public void MatchRule_MultipleRules_FirstMatchWins()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("common text", false),
            new("common", true)
        };

        var result = QueueCleanupService.MatchRule("this contains common text", rules);
        Assert.NotNull(result);
        Assert.Equal("common text", result.Value.Label);
    }

    [Fact]
    public void MatchRule_EmptyMessage_ReturnsNull()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("Sample", true)
        };

        var result = QueueCleanupService.MatchRule("", rules);
        Assert.Null(result);
    }

    [Fact]
    public void MatchRule_EmptyRules_ReturnsNull()
    {
        var result = QueueCleanupService.MatchRule("Some message", new List<QueueCleanupRule>());
        Assert.Null(result);
    }

    [Fact]
    public void GetTitle_Episode_FormatsCorrectly()
    {
        var item = new QueueResource
        {
            Episode = new QueueEpisode
            {
                Id = 42,
                Title = "The Episode",
                SeasonNumber = 1,
                EpisodeNumber = 3,
                Series = new QueueEpisodeSeriesResource { Title = "Test Show", Year = 2020 }
            }
        };

        var result = QueueCleanupService.GetTitle(item);
        Assert.Equal("Test Show (2020) - S01E03 - The Episode", result);
    }

    [Fact]
    public void GetTitle_Movie_FormatsCorrectly()
    {
        var item = new QueueResource
        {
            Movie = new QueueMovie { Id = 10, Title = "Inception", Year = 2010 }
        };

        var result = QueueCleanupService.GetTitle(item);
        Assert.Equal("Inception (2010)", result);
    }

    [Fact]
    public void GetTitle_MovieZeroYear_OmitsYear()
    {
        var item = new QueueResource
        {
            Movie = new QueueMovie { Id = 10, Title = "Unknown Movie", Year = 0 }
        };

        var result = QueueCleanupService.GetTitle(item);
        Assert.Equal("Unknown Movie", result);
    }

    [Fact]
    public void GetTitle_NoEpisodeOrMovie_FallsBackToItemTitle()
    {
        var item = new QueueResource { Id = 99, Title = "Fallback Title" };

        var result = QueueCleanupService.GetTitle(item);
        Assert.Equal("Fallback Title", result);
    }

    [Fact]
    public void GetTitle_NoItemTitle_FallsBackToId()
    {
        var item = new QueueResource { Id = 99 };

        var result = QueueCleanupService.GetTitle(item);
        Assert.Equal("ID 99", result);
    }

    [Fact]
    public void GetTitle_EpisodeNullTitle_FallsBackToEpisodeId()
    {
        var item = new QueueResource
        {
            Episode = new QueueEpisode
            {
                Id = 42,
                Title = null,
                SeasonNumber = 2,
                EpisodeNumber = 5,
                Series = new QueueEpisodeSeriesResource { Title = "Show", Year = 2021 }
            }
        };

        var result = QueueCleanupService.GetTitle(item);
        Assert.Equal("Show (2021) - S02E05 - Episode 42", result);
    }
}
