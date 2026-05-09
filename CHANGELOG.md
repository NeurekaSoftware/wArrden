# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Tree-style console log output with Unicode box-drawing characters and stats-before-results ordering
- Database-backed cooldown system to prevent repeated searches for the same items
- Search results sorted alphabetically

### Changed
- Radarr jobs run before Sonarr jobs
- Blocklist rules are now always active (environment variables no longer required)
- Project renamed from ArrWarden to wArrden

### Fixed
- Cooldown counts in search stats report accurately
- Sonarr queue cleanup using the wrong service due to a dependency injection conflict
- SQLite permission error when running in Docker
- Missing newline after queue cleanup operation output
- Banner box alignment and full episode titles in search results
