# ArrWarden

Automates Radarr and Sonarr download queue management — removes stuck/warning queue items and triggers periodic searches for missing or upgradable content.

## Queue Cleaner

Checks the download queue for items stuck in "warning" status (import blocked). Each warning is matched against known import error messages from Radarr and Sonarr and the item is automatically removed. Some items are sent to the blocklist (bad releases that should not be re-downloaded), while others are removed without blocklisting (items that simply aren't needed).

### Sonarr Rules

| Import Error Message | Action |
|---|---|
| Found matching series via grab history, but release was matched to series by ID. Automatic import is not possible. | Remove & Blocklist |
| Episode {0} was not found in the grabbed release: {1} | Remove & Blocklist |
| Episode {0} was unexpected considering the {1} folder name | Remove & Blocklist |
| Not an upgrade for existing episode file(s). Existing quality: {0}. New Quality {1}. | Remove only |
| Not a Custom Format upgrade for existing episode file(s). New: [{0}] ({1}) do not improve on Existing: [{2}] ({3}) | Remove only |
| No files found are eligible for import in {0} | Remove & Blocklist |
| Episode file already imported at {0} | Remove only |
| No audio tracks detected | Remove & Blocklist |
| Invalid season or episode | Remove & Blocklist |
| Single episode file contains all episodes in seasons. Review file name or manually import | Remove & Blocklist |
| Sample / Unable to determine if file is a sample | Remove & Blocklist |
| Found archive file, might need to be extracted | Remove & Blocklist |

### Radarr Rules

| Import Error Message | Action |
|---|---|
| Found matching movie via grab history, but release was matched to movie by ID. Manual Import required. | Remove & Blocklist |
| Not an upgrade for existing movie file. Existing quality: {0}. New Quality {1}. | Remove only |
| Not a Custom Format upgrade for existing movie file(s). New: [{0}] ({1}) do not improve on Existing: [{2}] ({3}) | Remove only |
| No files found are eligible for import in {0} | Remove & Blocklist |
| Movie file already imported at {0} | Remove only |
| No audio tracks detected | Remove & Blocklist |
| Sample / Unable to determine if file is a sample | Remove & Blocklist |
| Found archive file, might need to be extracted | Remove & Blocklist |

## Missing Search

Periodically searches for monitored episodes or movies that are missing from disk. Each search request is limited to a configurable number of items and respects a cooldown period to avoid re-searching the same item too frequently.

**Configuration:** `SONARR_MISSING_SEARCH_CRON`, `SONARR_MISSING_MAX_RESULTS`, `SONARR_MISSING_COOLDOWN`, `RADARR_MISSING_SEARCH_CRON`, `RADARR_MISSING_MAX_RESULTS`, `RADARR_MISSING_COOLDOWN`

## Upgrade Search

Periodically searches for monitored episodes or movies that have an existing file on disk but a better quality or custom format score is wanted. Same limiting and cooldown behavior as Missing Search.

**Configuration:** `SONARR_UPGRADE_SEARCH_CRON`, `SONARR_UPGRADE_MAX_RESULTS`, `SONARR_UPGRADE_COOLDOWN`, `RADARR_UPGRADE_SEARCH_CRON`, `RADARR_UPGRADE_MAX_RESULTS`, `RADARR_UPGRADE_COOLDOWN`
