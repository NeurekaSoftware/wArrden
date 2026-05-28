namespace wArrden.Services;

public static class QueueCleanupRuleMatchers
{
    private static readonly Dictionary<string, Dictionary<string, string[]>> Registry = new()
    {
        // ── Shared (all arr types) ──

        ["SAMPLE"] = Shared("Sample", "Unable to determine if file is a sample"),
        ["NO_FILES_ELIGIBLE"] = Shared("No files found are eligible"),
        ["NOT_CUSTOM_FORMAT_UPGRADE"] = Shared("Not a Custom Format upgrade"),
        ["NO_AUDIO_TRACKS"] = Shared("No audio tracks detected"),
        ["ARCHIVE_FILE"] = Shared("Found archive file"),
        ["INSUFFICIENT_FREE_SPACE"] = Shared("Not enough free space"),
        ["FILE_UNPACKING"] = Shared("File is still being unpacked"),
        ["UNABLE_TO_PARSE"] = Shared("Unable to parse file"),
        ["UNEXPECTED_ERROR"] = Shared("Unexpected error processing file"),
        ["LOCKED_FILE"] = Shared("Locked file, try again later"),
        ["UNSUPPORTED_EXTENSION"] = Shared("unsupported extension"),

        ["NOT_QUALITY_UPGRADE"] = new()
        {
            ["radarr"]   = new[] { "Not an upgrade for existing movie" },
            ["sonarr"]   = new[] { "Not an upgrade for existing episode" },
            ["lidarr"]   = new[] { "Not an upgrade for existing track file", "Not an upgrade for existing album file" },
            ["whisparr"] = new[] { "Not an upgrade for existing episode", "Not an upgrade for existing movie" },
        },

        ["NOT_REVISION_UPGRADE"] = new()
        {
            ["radarr"]   = new[] { "Not a quality revision upgrade" },
            ["sonarr"]   = new[] { "Not a quality revision upgrade" },
            ["whisparr"] = new[] { "Not a quality revision upgrade" },
        },

        ["DANGEROUS_FILE"] = new()
        {
            ["radarr"]   = new[] { "potentially dangerous file", "Found executable file" },
            ["sonarr"]   = new[] { "potentially dangerous file", "Found executable file" },
            ["lidarr"]   = new[] { "Found executable file" },
            ["whisparr"] = new[] { "potentially dangerous file", "Found executable file" },
        },

        ["DOWNLOAD_CLIENT_ERROR"] = Shared("is reporting an error"),

        ["IMPORT_PATH_INACCESSIBLE"] = Shared("Import failed, path does not exist or is not accessible by"),

        ["MATCHED_VIA_GRAB_HISTORY"] = new()
        {
            ["radarr"]   = new[] { "Found matching movie via grab history" },
            ["sonarr"]   = new[] { "Found matching series via grab history" },
            ["whisparr"] = new[] { "Found matching series via grab history", "Found matching movie via grab history" },
        },

        ["NOT_IN_GRABBED_RELEASE"] = new()
        {
            ["radarr"]   = new[] { "was not found in the grabbed release" },
            ["sonarr"]   = new[] { "not found in the grabbed release" },
            ["whisparr"] = new[] { "not found in the grabbed release" },
        },

        // ── Radarr / Whisparr Eros ──

        ["MOVIE_ALREADY_IMPORTED"] = RadarrWhisparr("Movie file already imported"),

        // ── Sonarr / Whisparr ──

        ["INVALID_SEASON_OR_EPISODE"] = SonarrWhisparr("Invalid season or episode"),

        ["EPISODE_UNEXPECTED_FOLDER"] = new()
        {
            ["sonarr"]   = new[] { "was unexpected considering the", "were unexpected considering the" },
            ["whisparr"] = new[] { "was unexpected considering the", "were unexpected considering the" },
        },

        ["FULL_SEASON_PACK"] = SonarrWhisparr("all episodes in seasons"),
        ["EPISODE_ALREADY_IMPORTED"] = SonarrWhisparr("Episode file already imported"),
        ["TITLE_MISSING"] = SonarrWhisparr("does not have a title"),
        ["TITLE_TBA"] = SonarrWhisparr("has a TBA title"),
        ["EXISTING_FILE_MORE_EPISODES"] = SonarrWhisparr("contains more episodes than this file"),
        ["SPLIT_EPISODE"] = SonarrWhisparr("split into multiple files"),
        ["MISSING_ABSOLUTE_NUMBER"] = SonarrWhisparr("does not have an absolute episode number"),
        ["UNVERIFIED_SCENE_MAPPING"] = SonarrWhisparr("mapping for this episode has not been confirmed"),

        // ── Lidarr Only ──

        ["ALBUM_ALREADY_IMPORTED"] = LidarrOnly("Album already imported"),
        ["FEWER_TRACKS"] = LidarrOnly("Has fewer tracks than existing release"),
        ["UNMATCHED_TRACKS"] = LidarrOnly("Has unmatched tracks"),
        ["MISSING_TRACKS"] = LidarrOnly("Has missing tracks"),
        ["ALBUM_NOT_REQUESTED"] = LidarrOnly("Album release not requested"),
        ["EXISTING_FILE_MORE_TRACKS"] = LidarrOnly("contains more tracks than this file"),
        ["DEST_FOLDER_NOT_ROOT"] = LidarrOnly("Destination folder"),
        ["ALBUM_MATCH_NOT_CLOSE"] = LidarrOnly("Album match is not close enough"),
        ["NO_TRACKS_MATCHED"] = LidarrOnly("No tracks matched"),
        ["TRACK_MATCH_NOT_CLOSE"] = LidarrOnly("Track match is not close enough"),
    };

    private static Dictionary<string, string[]> Shared(params string[] patterns) => new()
    {
        ["radarr"]   = patterns,
        ["sonarr"]   = patterns,
        ["lidarr"]   = patterns,
        ["whisparr"] = patterns,
    };

    private static Dictionary<string, string[]> RadarrOnly(params string[] patterns) => new()
    {
        ["radarr"] = patterns,
    };

    private static Dictionary<string, string[]> RadarrWhisparr(params string[] patterns) => new()
    {
        ["radarr"] = patterns,
        ["whisparr"] = patterns,
    };

    private static Dictionary<string, string[]> SonarrWhisparr(params string[] patterns) => new()
    {
        ["sonarr"]   = patterns,
        ["whisparr"] = patterns,
    };

    private static Dictionary<string, string[]> LidarrOnly(params string[] patterns) => new()
    {
        ["lidarr"] = patterns,
    };

    public static string[]? GetPatterns(string key, string arrType)
    {
        if (Registry.TryGetValue(key, out var arrDict)
            && arrDict.TryGetValue(arrType, out var patterns))
        {
            return patterns;
        }

        return null;
    }

    public static bool IsValidKey(string key) => Registry.ContainsKey(key);
}
