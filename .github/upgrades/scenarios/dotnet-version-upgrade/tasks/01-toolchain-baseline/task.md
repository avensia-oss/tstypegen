# 01-toolchain-baseline: Verify upgrade prerequisites

Confirm the local .NET 10 SDK/toolchain is available and that the solution configuration is ready for the framework bump. Capture any baseline details needed before editing project files so the upgrade starts from a known-good state.

This task covers the shared prerequisites for the solution, including any `global.json` compatibility check and a quick review of the current project layout. It should establish the starting point for the atomic upgrade without changing the product code yet.

## Research Notes

- .NET 10 SDK is installed and compatible with the requested target framework.
- No `global.json` file was found in the repository root or solution tree search, so there is no pinned SDK version to reconcile before the upgrade.
- The solution root is `TSTypeGen.sln` and the assessment already shows a small, all-SDK-style solution with 4 projects on `net8.0`.
- No environment blocker was identified for starting the framework upgrade.

## Affected Scope

- `src/TSTypeGen/TSTypeGen.csproj`
- `src/TSTypeGen.Tests/TSTypeGen.Tests.csproj`
- `src/TSTypeGen.Tests.Main/TSTypeGen.Tests.Main.csproj`
- `src/TSTypeGen.Tests.Shared/TSTypeGen.Tests.Shared.csproj`

**Done when**: The required .NET 10 toolchain is confirmed, any solution-wide prerequisites are understood, and the upgrade can proceed with no unresolved environment blockers.
