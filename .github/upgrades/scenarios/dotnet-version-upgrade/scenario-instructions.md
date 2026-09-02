# .NET Version Upgrade Scenario

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0
- **Commit Strategy**: After Each Task

## Source Control
- **Source Branch**: update-to-esmodules
- **Working Branch**: upgrade-dotnet-10
- **Commit Strategy**: After Each Task
- **Branch Sync**: Auto (Merge)

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-At-Once

### Project Structure
- Package Management: Per-Project (defer CPM to post-migration)

### Compatibility
- Unsupported API Handling: Fix Inline

### Modernization
- Nullable Reference Types: Leave Disabled

## Strategy
**Selected**: All-At-Once
**Rationale**: 4 SDK-style projects are moving from net8.0 to net10.0 on a shallow dependency graph. The main project has source-incompatible framework API usage, but there is no .NET Framework migration or side-by-side coexistence required.

### Execution Constraints
- Upgrade all projects together in a single operation.
- Update TFMs and package references before compiling.
- Fix source-incompatible API changes inline within the same pass.
- Run full solution build validation after the upgrade and then test the solution.
- Keep package management per-project during the migration and defer CPM until after the upgrade stabilizes.
