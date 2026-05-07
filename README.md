<p align="center">

<img src="Logo.png" alt="Logo" height="128px" />

# ArrWarden

Makes maintaining your Radarr and Sonarr libraries easy by automatically removing stuck downloads and searching for missing or upgradable content.

</p>

## Queue Cleanup

Automatically removes stuck downloads from your queue that are blocking imports. Recognizes common import errors (wrong episodes, not an upgrade, samples, corrupt files, etc.) and either removes the item or blocklists it to prevent re-downloading the same bad release.

## Missing Search

Periodically searches for monitored content that has never been downloaded. Limits the number of searches per run and ensures the same item isn't re-searched too often.

## Upgrade Search

Periodically searches for monitored content that already exists on disk but has a better custom format score. Uses the same limits and cooldown behavior as Missing Search.
