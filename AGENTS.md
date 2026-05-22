# AGENTS

## Purpose

wArrden is a scheduled job runner that searches for missing/upgrade items and cleans stuck queue entries on Sonarr/Radarr instances. This file enforces output formatting standards for all log output shown in the console.

## Log Level Threshold

Configured via `logLevel` in `config.yaml`. The value sets the minimum severity for console output:

| `logLevel` | Messages Shown |
|---|---|
| `debug` | DBG + INF + WRN + ERR (most verbose) |
| `info` (default) | INF + WRN + ERR |
| `warning` | WRN + ERR |
| `error` | ERR only (least verbose) |

Debug and Info messages write to `stdout`. Warning and Error messages write to `stderr`.

## Output Streams

- **DBG / INF** → `Console.Out` (`OutputService.Out`)
- **WRN / ERR** → `Console.Error` (`OutputService.Error`)

## Log Format Rules

### Header

Every log entry starts with a header line:

```
[HH:mm:ss LVL] [context]
```

Where:
- `HH:mm:ss` is the 24-hour timestamp of invocation
- `LVL` is the three-letter log level marker: `DBG`, `INF`, `WRN`, or `ERR`
- `context` is `instance.job` for job output, or a system context like `warden.config`, `warden.scheduler`, `cli`

### Tree Structure

All log output uses Unicode box-drawing characters to form a tree:

- ` ├─ ` — intermediate child (not the last)
- ` └─ ` — final child (last in the tree level)
- ` │  ` — vertical connector for nested content under intermediate parent
- `    ` — spacing for nested content under final parent
- ` • ` — bullet for list items

### Debug (DBG)

Single message:

```
[07:45:01 DBG] [warden.config]
 └─ Loaded config from data/config.yaml
```

With detail:

```
[07:45:01 DBG] [series.missing]
 ├─ Fetched 45 wanted episodes
 └─ Cooldown filter: 12 on cooldown, 33 eligible, 3 selected
```

### Warning (WRN)

```
[07:45:01 WRN] [series.missing]
 └─ No enabled indexers — search skipped

[07:45:01 WRN] [series.missing]
 ├─ Search trigger failed for The Boys (2019) - S01E01 - The Name of the Game
 └─ HttpRequestException: Connection refused (192.168.1.100:8989)
```

### Error (ERR)

```
[07:45:01 ERR] [series.missing]
 ├─ Missing search job failed
 └─ HttpRequestException: No route to host

[07:45:01 ERR] [warden.scheduler]
 ├─ Scheduled task error
 └─ InvalidOperationException: Unknown instance type: unknown
```

### Info (INF) — Search Jobs (missing / upgrade)

```
[07:45:01 INF] [sonarr.missing]
 ├─ Cleaning cooldown entries
 ├─ Fetching wanted episodes
 ├─ Applying cooldown filters
 └─ Stats:
    • Total Items:   4
    • On Cooldown:   4
    • Eligible:      0
    • Search Limit:  2
    • Result:        No search performed
```

When items are searched, stats come before results:

```
[07:45:01 INF] [sonarr.missing]
 ├─ Cleaning cooldown entries
 ├─ Fetching wanted episodes
 ├─ Applying cooldown filters
 ├─ Searching 3 items
 ├─ Stats:
 │  • Total Items:   10
 │  • On Cooldown:   2
 │  • Eligible:      8
 │  • Search Limit:  3
 │  • Result:        Searched 3
 └─ Results:
    • The Boys (2019) - S01E01 - The Name of the Game
    • The Boys (2019) - S01E02 - Cherry
    • Game of Thrones (2011) - S01E01 - Winter Is Coming
```

**Rules:**
- Stats always come before Results
- Full stats are shown even when no wanted items are found
- `Result` field: `"No wanted items found"` (0 total), `"No search performed"` (0 searched), or `"Searched N"`
- For Radarr, use `"Fetching wanted movies"` instead of `"Fetching wanted episodes"`

### Info (INF) — Queue Cleanup Job (queue)

No blocked items:

```
[07:45:01 INF] [sonarr.queue]
 └─ Stats:
    • Total Queue:   150
    • Result:        No blocked queue items detected
```

With matches (stats before results):

```
[07:45:01 INF] [sonarr.queue]
 ├─ Stats:
 │  • Total Queue:   150
 │  • Blocked:       5
 │  • Matched:       2
 │  • Result:        Blocklisted 2
 └─ Results:
    • The Boys (2019) - S01E01 - The Name of the Game  NOT_AN_UPGRADE
    • Game of Thrones (2011) - S02E03 - Valar Morghulis  SAMPLE
```

**Rules:**
- Stats always come before Results
- `Result` field: `"Blocklisted N"` or `"Would blocklist N"` (dry run) or `"No blocked queue items detected"`

## Item Display Format

### Episodes

```
Series Name (Year) - S##E## - Episode Title
```

Examples:
- `The Boys (2019) - S01E01 - The Name of the Game`
- `Game of Thrones (2011) - S01E01 - Winter Is Coming`

Season and episode numbers are zero-padded to 2 digits (`D2`). A hyphen and space separate the year from the season indicator.

### Movies

```
Movie Title (Year)
```

Examples:
- `Inception (2010)`
- `The Dark Knight (2008)`

Year is only appended when greater than zero.

## Implementation

Log output is handled by `OutputService` and `SearchOutputWriter` in `wArrden/Services/OutputService.cs`. Item title formatting is done in `SearchService.cs` and `QueueCleanupService.cs`. Queue cleanup rules are in `wArrden/Services/QueueCleanupRules.cs`. API models are in `wArrden/Clients/Models/`.

## Build / Quality Checks

```bash
dotnet build wArrden
dotnet test wArrden.Tests
```

## Unit Tests

- Tests live in `wArrden.Tests/` using **xUnit** and **Moq**.
- After any code change to `wArrden/`, run `dotnet test wArrden.Tests` and verify all tests pass before committing.
- EF Core tests targeting the real SQLite provider use `Microsoft.Data.Sqlite` in-memory mode (shared cache) — never the InMemory provider, which does not support `ExecuteDeleteAsync`.
- Internal members are exposed to the test project via `InternalsVisibleTo` in `wArrden.csproj`.
- New public/internal methods must have corresponding unit tests.
- Integration points (HTTP clients, scheduling) are mocked; pure logic (parsing, rule matching, title formatting, cooldown filtering) is tested directly.
