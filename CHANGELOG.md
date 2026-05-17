# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-05-16

### Added
- Scheduled missing item search for Sonarr episodes and Radarr movies
- Scheduled upgrade search for finding better-quality versions of existing media
- Automatic queue cleanup to remove stuck or blocked imports
- Cooldown system to avoid re-searching the same items too frequently
- Support for both Radarr and Sonarr instances with independent configuration per job type
- Structured console output with item counts, cooldown status, and result summaries

[Unreleased]: https://code.neureka.dev/warrden/warrden/compare/1.0.0...HEAD
[1.0.0]: https://code.neureka.dev/warrden/warrden/releases/tag/1.0.0
