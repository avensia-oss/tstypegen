# 02-upgrade-solution Progress

Updated all four projects from `net8.0` to `net10.0` and aligned the package versions called out by the assessment. The main and test projects now reference the newer `Microsoft.Extensions.FileSystemGlobbing`, `Newtonsoft.Json`, `System.CodeDom`, and `System.Reflection.MetadataLoadContext` package versions.

The solution builds successfully after the upgrade. A workspace-wide test run completed with discovery only and reported no runnable tests in the selected test project, so there was no failing test surface to address during this task.
