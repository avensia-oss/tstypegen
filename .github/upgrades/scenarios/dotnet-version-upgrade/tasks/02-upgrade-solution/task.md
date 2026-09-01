# 02-upgrade-solution: Move all projects to .NET 10 and resolve breaking changes

Upgrade every project in the solution to `net10.0` in a single coordinated pass. Update package references where the assessment recommended newer versions, then fix the source-incompatible framework API usage in the main project so the solution compiles on the new target framework.

The main risk surface is the `TSTypeGen` project, which contains the bulk of the framework API compatibility issues. The test projects should move with the application project so references stay aligned, and package version changes should be kept consistent across the solution while preserving per-project package management during the migration.

## Research Notes

- All 4 projects are SDK-style and already on `net8.0`, so this is a modern-to-modern framework bump rather than a project-format conversion.
- The solution has 29 reported issues total; 17 of the main project issues are source-incompatible framework API changes.
- Package updates are recommended in `src/TSTypeGen/TSTypeGen.csproj` and `src/TSTypeGen.Tests/TSTypeGen.Tests.csproj` for `Microsoft.Extensions.FileSystemGlobbing`, `Newtonsoft.Json`, `System.CodeDom`, and `System.Reflection.MetadataLoadContext`.
- The API hot spots are in `src/TSTypeGen/CustomMetadataAssemblyResolver.cs` and `src/TSTypeGen/Processor.cs`, both of which use `System.Reflection.MetadataLoadContext` / `PathAssemblyResolver` APIs flagged by the assessment.
- No global package management file is present, so package references remain per-project for this upgrade.

## Affected Scope

- `src/TSTypeGen/TSTypeGen.csproj`
- `src/TSTypeGen.Tests/TSTypeGen.Tests.csproj`
- `src/TSTypeGen.Tests.Main/TSTypeGen.Tests.Main.csproj`
- `src/TSTypeGen.Tests.Shared/TSTypeGen.Tests.Shared.csproj`
- `src/TSTypeGen/CustomMetadataAssemblyResolver.cs`
- `src/TSTypeGen/Processor.cs`

**Done when**: All projects target `net10.0`, recommended package updates are applied, the framework API changes compile cleanly, and the upgraded solution is ready for validation.
