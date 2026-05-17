<div align="center">

<img src="Logo.png" alt="Logo" height="128px" />

# wArrden

[![Release](https://img.shields.io/badge/dynamic/json.svg?style=flat-square&logo=git&logoColor=F43F5E&label=Release&color=F43F5E&url=https://code.neureka.dev/api/v1/repos/warrden/warrden/releases&query=$[0].tag_name)](https://code.neureka.dev/warrden/warrden/releases)
[![Actions](https://img.shields.io/badge/dynamic/json?style=flat-square&logo=gitlfs&logoColor=8B5CF6&label=Actions&color=8B5CF6&url=https://code.neureka.dev/api/v1/repos/warrden/warrden/actions/runs&query=workflow_runs[0].status)](https://code.neureka.dev/warrden/warrden/actions)
[![AI](https://img.shields.io/badge/AI-assisted-5786FE?style=flat-square&logo=deepseek&logoColor=5786FE)](https://code.neureka.dev/warrden/warrden)

Makes maintaining your Radarr and Sonarr libraries easy by automatically removing stuck downloads and searching for missing or upgradable content.

</div>

## Queue Cleanup

Automatically removes stuck downloads from your queue that are blocking imports. Recognizes common import errors (wrong episodes, not an upgrade, samples, corrupt files, etc.) and either removes the item or blocklists it to prevent re-downloading the same bad release.

## Missing Search

Periodically searches for monitored content that has never been downloaded. Limits the number of searches per run and ensures the same item isn't re-searched too often.

## Upgrade Search

Periodically searches for monitored content that already exists on disk but has a better custom format score. Uses the same limits and cooldown behavior as Missing Search.
