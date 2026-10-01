# Upgrade Options — TSTypeGen

Assessment: 4 projects on net8.0 targeting net10.0; all SDK-style; 17 source-incompatible API hits in the main project; 4 package updates recommended.

## Strategy

### Upgrade Strategy
The solution is a small modern-to-modern upgrade with a shallow dependency graph, so an all-at-once pass keeps the work atomic and avoids unnecessary phasing.

| Value | Description |
|-------|-------------|
| **All-At-Once** (selected) | Upgrade all projects simultaneously in a single operation. |
| Bottom-Up | Upgrade dependencies first, then dependants in buildable tiers. |
| Top-Down | Upgrade applications first and keep lower layers buildable through the transition. |

## Project Structure

### Package Management
The solution has multiple SDK-style projects and no centralized package management file, but the upgrade stays within modern .NET, so package version updates can be handled per project during the atomic pass.

| Value | Description |
|-------|-------------|
| **Per-Project (defer CPM to post-migration)** (selected) | Each project retains its own versions during the active migration. |
| Central Package Management (CPM) | Creates `Directory.Packages.props` and centralizes versions. |

## Compatibility

### Unsupported API Handling
The assessment found 17 source-incompatible framework API usages in the main project, so these changes need a clear handling policy during the upgrade.

| Value | Description |
|-------|-------------|
| **Fix Inline** (selected) | Resolve every API change in the same task, including complex ones. |
| Defer Complex Changes | Apply simple replacements inline and stub complex API changes for later resolution. |

## Modernization

### Nullable Reference Types
The solution is small, but it already has a noticeable breaking-change surface, so nullable can wait until the upgrade stabilizes.

| Value | Description |
|-------|-------------|
| **Leave Disabled** (selected) | Do not enable nullable reference types during this upgrade. |
| Enable Nullable Reference Types | Add `<Nullable>enable</Nullable>` to project files. |
