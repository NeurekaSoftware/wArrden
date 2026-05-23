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
            new("NOT_QUALITY_UPGRADE", false)
        };

        var result = QueueCleanupService.MatchRule("This is Not an upgrade for existing episode file(s). Existing quality: HDTV-720p. New Quality HDTV-1080p.", rules, "sonarr");
        Assert.NotNull(result);
        Assert.Equal("NOT_QUALITY_UPGRADE", result.Value.Label);
        Assert.False(result.Value.Blocklist);
    }

    [Fact]
    public void MatchRule_BlocklistRule_FindsMatch()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("SAMPLE", true)
        };

        var result = QueueCleanupService.MatchRule("This is a Sample file", rules, "sonarr");
        Assert.NotNull(result);
        Assert.Equal("SAMPLE", result.Value.Label);
        Assert.True(result.Value.Blocklist);
    }

    [Fact]
    public void MatchRule_NoMatch_ReturnsNull()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("NOT_QUALITY_UPGRADE", false)
        };

        var result = QueueCleanupService.MatchRule("Download completed successfully", rules, "sonarr");
        Assert.Null(result);
    }

    [Fact]
    public void MatchRule_MultipleRules_FirstMatchWins()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("NOT_QUALITY_UPGRADE", false),
            new("NO_FILES_ELIGIBLE", true)
        };

        var result = QueueCleanupService.MatchRule("Not an upgrade for existing episode file(s)", rules, "sonarr");
        Assert.NotNull(result);
        Assert.Equal("NOT_QUALITY_UPGRADE", result.Value.Label);
    }

    [Fact]
    public void MatchRule_EmptyMessage_ReturnsNull()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("SAMPLE", true)
        };

        var result = QueueCleanupService.MatchRule("", rules, "sonarr");
        Assert.Null(result);
    }

    [Fact]
    public void MatchRule_EmptyRules_ReturnsNull()
    {
        var result = QueueCleanupService.MatchRule("Some message", new List<QueueCleanupRule>(), "sonarr");
        Assert.Null(result);
    }

    [Fact]
    public void MatchRule_UnknownKey_FallsBackToRawSubstring()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("some raw text", false)
        };

        var result = QueueCleanupService.MatchRule("this contains some raw text", rules, "sonarr");
        Assert.NotNull(result);
        Assert.Equal("some raw text", result.Value.Label);
    }

    [Fact]
    public void MatchRule_KeyNotApplicableToArr_ReturnsNull()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("ALBUM_ALREADY_IMPORTED", true)
        };

        var result = QueueCleanupService.MatchRule("Album already imported", rules, "sonarr");
        Assert.Null(result);
    }

    [Fact]
    public void MatchRule_RevisionUpgrade_FindsMatch()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("NOT_REVISION_UPGRADE", false)
        };

        var result = QueueCleanupService.MatchRule(
            "Not a quality revision upgrade for existing movie file(s)", rules, "radarr");
        Assert.NotNull(result);
        Assert.Equal("NOT_REVISION_UPGRADE", result.Value.Label);
    }

    [Fact]
    public void MatchRule_CrossArrKey_UsesCorrectPatterns()
    {
        var rules = new List<QueueCleanupRule>
        {
            new("NOT_QUALITY_UPGRADE", false)
        };

        var radarrMatch = QueueCleanupService.MatchRule(
            "Not an upgrade for existing movie file(s)", rules, "radarr");
        Assert.NotNull(radarrMatch);

        var sonarrMatch = QueueCleanupService.MatchRule(
            "Not an upgrade for existing episode file(s)", rules, "sonarr");
        Assert.NotNull(sonarrMatch);

        var lidarrMatch = QueueCleanupService.MatchRule(
            "Not an upgrade for existing album file(s)", rules, "lidarr");
        Assert.NotNull(lidarrMatch);
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

    [Fact]
    public void GetTitle_ArtistWithAlbum_FormatsCorrectly()
    {
        var item = new QueueResource
        {
            Artist = new QueueArtist { Id = 1, ArtistName = "The Beatles" },
            Album = new QueueAlbum { Id = 10, Title = "Abbey Road" }
        };

        var result = QueueCleanupService.GetTitle(item);
        Assert.Equal("The Beatles - Abbey Road", result);
    }

    [Fact]
    public void GetTitle_ArtistWithoutAlbum_UsesArtistNameOnly()
    {
        var item = new QueueResource
        {
            Artist = new QueueArtist { Id = 1, ArtistName = "Pink Floyd" }
        };

        var result = QueueCleanupService.GetTitle(item);
        Assert.Equal("Pink Floyd", result);
    }

    [Fact]
    public void GetTitle_ArtistNullName_FallsBackToArtistId()
    {
        var item = new QueueResource
        {
            Artist = new QueueArtist { Id = 7, ArtistName = null }
        };

        var result = QueueCleanupService.GetTitle(item);
        Assert.Equal("Artist 7", result);
    }

    [Fact]
    public void GetTitle_ArtistNullNameWithAlbum_FallsBackToArtistId()
    {
        var item = new QueueResource
        {
            Artist = new QueueArtist { Id = 7, ArtistName = null },
            Album = new QueueAlbum { Id = 10, Title = "Dark Side of the Moon" }
        };

        var result = QueueCleanupService.GetTitle(item);
        Assert.Equal("Artist 7 - Dark Side of the Moon", result);
    }

    [Fact]
    public void GetTitle_WhisparrEpisode_FormatsSameAsSonarr()
    {
        var item = new QueueResource
        {
            Episode = new QueueEpisode
            {
                Id = 99,
                Title = "Scene 1",
                SeasonNumber = 1,
                EpisodeNumber = 1,
                Series = new QueueEpisodeSeriesResource { Title = "Test Studio", Year = 2023 }
            }
        };

        var result = QueueCleanupService.GetTitle(item);
        Assert.Equal("Test Studio (2023) - S01E01 - Scene 1", result);
    }
}
