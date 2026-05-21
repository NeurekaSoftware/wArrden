<div align="center">

<img src="Logo.png" alt="Logo" height="128px" />

# wArrden

[![Release](https://img.shields.io/badge/dynamic/json.svg?style=flat-square&logo=git&logoColor=F43F5E&label=Release&color=F43F5E&url=https://code.neureka.dev/api/v1/repos/warrden/warrden/releases&query=$[0].tag_name)](https://code.neureka.dev/warrden/warrden/releases)
[![Actions](https://img.shields.io/badge/dynamic/json?style=flat-square&logo=gitlfs&logoColor=8B5CF6&label=Actions&color=8B5CF6&url=https://code.neureka.dev/api/v1/repos/warrden/warrden/actions/runs&query=workflow_runs[0].status)](https://code.neureka.dev/warrden/warrden/actions)
[![AI](https://img.shields.io/badge/AI-assisted-5786FE?style=flat-square&logo=deepseek&logoColor=5786FE)](https://code.neureka.dev/warrden/warrden)
[![Stars](https://img.shields.io/github/stars/NeurekaSoftware/wArrden?style=flat-square&label=Stars&color=EAB308&logo=googlegemini&logoColor=EAB308)](https://code.neureka.dev/warrden/warrden)

wArrden makes it easy to maintain your Radarr and Sonarr libraries by finding missing or upgradeable content, as well as detecting and clearing stuck imports from the queue.

</div>

## Quickstart

1. Download `compose.yaml`:
   ```
   curl -O https://code.neureka.dev/warrden/warrden/raw/branch/main/compose.yaml
   ```

2. Download the example config as `config.yaml`, then edit it with your Sonarr/Radarr URLs and API keys:
   ```
   curl -o config.yaml https://code.neureka.dev/warrden/warrden/raw/branch/main/config.example.yaml
   ```

3. Start the container:
   ```
   docker compose up -d
   ```

## Missing Search

Periodically searches for monitored content that has never been downloaded. Limits the number of searches per run and ensures the same item isn't re-searched too often.

## Upgrade Search

Periodically searches for monitored content that already exists on disk but has a better custom format score. Uses the same limits and cooldown behavior as Missing Search.

## Queue Cleanup

Detects stuck imports caused by common errors (wrong episode, not an upgrade, sample, corrupt file) and removes or blocklists them so the same release won't download again.

## Why Use wArrden?

Radarr and Sonarr primarily rely on RSS feeds to detect newly uploaded releases every 15 minutes. While this works well for new uploads, many users assume it also continuously searches and reevaluates their entire library — it does not.

This creates a few common gaps in automation:

- If your server, indexers, or download clients are offline for a period of time, releases uploaded during that window may be missed entirely.
- Adding new indexers later does not retroactively search for older missing or upgradeable content. Only newly uploaded releases seen through RSS are picked up.
- Changes to Custom Formats (CFs) or scoring rules do not trigger automatic upgrades for existing media. Improved scoring only applies to future RSS releases.

Over time, this can leave libraries with permanently missing content or media that no longer matches your preferred quality and scoring standards.

wArrden fills those gaps by periodically rechecking your library and automating the cleanup work that would otherwise require manual intervention.

## CLI Usage

| Command | Description |
|---|---|
| `docker exec warrden clear-missing [instance]` | Clears all missing search cooldowns. If `instance` is omitted, clears across all instances. |
| `docker exec warrden clear-upgrades [instance]` | Clears all upgrade search cooldowns. If `instance` is omitted, clears across all instances. |
