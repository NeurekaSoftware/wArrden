namespace wArrden.Services;

public static class QueueCleanupRules
{
    public static readonly Dictionary<string, (string Match, bool Blocklist)> Sonarr = new()
    {
        ["GRAB_HISTORY_SERIES"] = ("Found matching series via grab history", true),
        ["EPISODE_NOT_IN_RELEASE"] = ("not found in the grabbed release", true),
        ["UNEXPECTED_EPISODE"] = ("unexpected considering the", true),
        ["NOT_AN_UPGRADE"] = ("Not an upgrade for existing episode", false),
        ["NOT_A_CUSTOM_FORMAT_UPGRADE"] = ("Not a Custom Format upgrade", false),
        ["NO_FILES_ELIGIBLE"] = ("No files found are eligible", true),
        ["EPISODE_ALREADY_IMPORTED"] = ("Episode file already imported", false),
        ["NO_AUDIO_TRACKS"] = ("No audio tracks detected", true),
        ["INVALID_SEASON_OR_EPISODE"] = ("Invalid season or episode", true),
        ["FULL_SEASON"] = ("all episodes in seasons", true),
        ["SAMPLE"] = ("Sample", true),
        ["ARCHIVE_FILE"] = ("Found archive file", true),
    };

    public static readonly Dictionary<string, (string Match, bool Blocklist)> Radarr = new()
    {
        ["GRAB_HISTORY_MOVIE"] = ("Found matching movie via grab history", true),
        ["NOT_AN_UPGRADE"] = ("Not an upgrade for existing movie", false),
        ["NOT_A_CUSTOM_FORMAT_UPGRADE"] = ("Not a Custom Format upgrade", false),
        ["NO_FILES_ELIGIBLE"] = ("No files found are eligible", true),
        ["MOVIE_ALREADY_IMPORTED"] = ("Movie file already imported", false),
        ["NO_AUDIO_TRACKS"] = ("No audio tracks detected", true),
        ["SAMPLE"] = ("Sample", true),
        ["ARCHIVE_FILE"] = ("Found archive file", true),
    };
}
