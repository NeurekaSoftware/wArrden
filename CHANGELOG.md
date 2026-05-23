# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.1.2] - 2026-05-22

### Added
- Add configurable log level (debug, info, warning, error) with tree-formatted console output

### Changed
- Rename queue cleanup Warnings label and distinguish remove vs blocklist actions in output

## [2.1.1] - 2026-05-22

### Fixed
- Fix Lidarr missing/upgrade search silently returning no results despite valid configuration
- Fix crash when optional configuration fields are left empty
- Fix indexer availability check using wrong search endpoint across all instance types

## [2.1.0] - 2026-05-21

### Added
- Add Lidarr and Whisparr support with queue cleanup rules
- Add PUID and PGID support for non-root container execution
- Add clear-missing and clear-upgrades CLI commands for cooldown management

## [2.0.1] - 2026-05-20

### Added
- Add ARM64 multi-arch support for Docker deployments

## [2.0.0] - 2026-05-20

### Added
- Add searchType configuration for episode and season searches on Sonarr instances
- Add per-instance indexer name filter for searches and upgrades
- Make queue cleanup warning matchers configurable through YAML configuration

### Changed
- Reject unknown YAML keys and missing required keys with startup validation errors

### Removed
- Remove legacy environment variable configuration (SONARR_*, RADARR_*), deprecated since 1.1.0

### Fixed
- Fix searches creating cooldown entries when no enabled indexers are available

## [1.1.0] - 2026-05-17

### Added
- YAML-based configuration supporting multiple named instances per arr type
- `CONFIG_PATH` environment variable for specifying a custom config file path
- Config file example with comprehensive instance configuration options

### Changed
- Configuration model from single-instance environment variables to multi-instance YAML

### Deprecated
- Legacy environment variable configuration (`SONARR_*`, `RADARR_*`); emits a warning on startup

## [1.0.0] - 2026-05-16

### Added
- Scheduled missing item search for Sonarr episodes and Radarr movies
- Scheduled upgrade search for finding better-quality versions of existing media
- Automatic queue cleanup to remove stuck or blocked imports
- Cooldown system to avoid re-searching the same items too frequently
- Support for both Radarr and Sonarr instances with independent configuration per job type
- Structured console output with item counts, cooldown status, and result summaries

[Unreleased]: https://code.neureka.dev/warrden/warrden/compare/2.1.2...HEAD
[2.1.2]: https://code.neureka.dev/warrden/warrden/releases/tag/2.1.2
[2.1.1]: https://code.neureka.dev/warrden/warrden/releases/tag/2.1.1
[2.1.0]: https://code.neureka.dev/warrden/warrden/releases/tag/2.1.0
[2.0.1]: https://code.neureka.dev/warrden/warrden/releases/tag/2.0.1
[2.0.0]: https://code.neureka.dev/warrden/warrden/releases/tag/2.0.0
[1.1.0]: https://code.neureka.dev/warrden/warrden/releases/tag/1.1.0
[1.0.0]: https://code.neureka.dev/warrden/warrden/releases/tag/1.0.0
