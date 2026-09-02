# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade the solution from .NET 8 to .NET 10.
**Scope**: 4 SDK-style projects, ~3.7k LOC, shallow dependency graph, with package updates and a concentrated API-fix surface in the main project.

## Tasks

### 01-toolchain-baseline: Verify upgrade prerequisites

Confirm the local .NET 10 SDK/toolchain is available and that the solution configuration is ready for the framework bump. Capture any baseline details needed before editing project files so the upgrade starts from a known-good state.

This task covers the shared prerequisites for the solution, including any `global.json` compatibility check and a quick review of the current project layout. It should establish the starting point for the atomic upgrade without changing the product code yet.

**Done when**: The required .NET 10 toolchain is confirmed, any solution-wide prerequisites are understood, and the upgrade can proceed with no unresolved environment blockers.

---

### 02-upgrade-solution: Move all projects to .NET 10 and resolve breaking changes

Upgrade every project in the solution to `net10.0` in a single coordinated pass. Update package references where the assessment recommended newer versions, then fix the source-incompatible framework API usage in the main project so the solution compiles on the new target framework.

The main risk surface is the `TSTypeGen` project, which contains the bulk of the framework API compatibility issues. The test projects should move with the application project so references stay aligned, and package version changes should be kept consistent across the solution while preserving per-project package management during the migration.

**Done when**: All projects target `net10.0`, recommended package updates are applied, the framework API changes compile cleanly, and the upgraded solution is ready for validation.

---

### 03-validate-solution: Build and test the upgraded solution

Run the full solution validation after the upgrade to confirm the framework bump is complete and stable. Verify the solution builds successfully, the tests pass, and no follow-up compile issues remain in the upgraded projects.

This task is the final quality gate for the atomic upgrade. It should also confirm that the repository is still in a good state for the deferred package-management follow-up, which will remain per-project until a later cleanup effort.

**Done when**: The solution builds without errors, tests pass, and the upgrade can be considered complete from a .NET version perspective.
