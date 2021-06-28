using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nuclear.Assemblies;
using Nuclear.Assemblies.Runtimes;
using Nuclear.Extensions;

namespace TSTypeGen
{
    public class CustomMetadataAssemblyResolver : MetadataAssemblyResolver
    {
        private readonly PathAssemblyResolver _pathAssemblyResolver;

        private static readonly string _nugetDirName = ".nuget";
        private static readonly string _packagesDirName = "packages";

        private readonly List<DirectoryInfo> _nugetCaches = null;

        public CustomMetadataAssemblyResolver(PathAssemblyResolver pathAssemblyResolver)
        {
            _pathAssemblyResolver = pathAssemblyResolver;
            _nugetCaches = GetCaches();
        }

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            var asm = _pathAssemblyResolver.Resolve(context, assemblyName);

            if (asm == null)
            {
                if (RuntimesHelper.TryGetCurrentRuntime(out RuntimeInfo current))
                {
                    var candidates = GetAssemblyCandidates(assemblyName, _nugetCaches, current);
                    if (candidates.Any())
                    {
                        asm = context.LoadFromAssemblyPath(candidates.First().FullName);
                    }
                }
            }

            return asm;
        }

        internal static List<DirectoryInfo> GetCaches()
        {
            var caches = new List<DirectoryInfo>();

            var userProfileDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var usersDir = userProfileDir.Parent;

            var userNugetDir = Path.Combine(userProfileDir.FullName, _nugetDirName, _packagesDirName);

            if (Directory.Exists(userNugetDir))
            {
                caches.Add(new DirectoryInfo(userNugetDir));
            }

            foreach (var userDir in usersDir.EnumerateDirectories())
            {
                try
                {
                    var secondaryNugetDir = Path.Combine(userDir.FullName, _nugetDirName, _packagesDirName);

                    if (Directory.Exists(secondaryNugetDir) && secondaryNugetDir != userNugetDir)
                    {
                        caches.Add(new DirectoryInfo(secondaryNugetDir));
                    }

                }
                catch {}

            }

            return caches;
        }

        internal static List<FileInfo> GetAssemblyCandidates(AssemblyName assemblyName, List<DirectoryInfo> nugetCaches, RuntimeInfo current)
        {
            var candidates = new List<FileInfo>();

            if (RuntimesHelper.TryGetLoadableRuntimes(current, out IEnumerable<RuntimeInfo> validRuntimes))
            {
                foreach (var cache in nugetCaches)
                {
                    candidates.AddRange(GetAssemblyCandidatesFromCache(assemblyName, cache, validRuntimes, true));
                }

                if (!candidates.Any())
                {
                    foreach (var cache in nugetCaches)
                    {
                        candidates.AddRange(GetAssemblyCandidatesFromCache(assemblyName, cache, validRuntimes, false));
                    }
                }
            }

            return candidates;
        }

        internal static IEnumerable<FileInfo> GetAssemblyCandidatesFromCache(AssemblyName assemblyName, DirectoryInfo nugetCache, IEnumerable<RuntimeInfo> validRuntimes, bool matchExact)
        {
            var comparer = new RuntimeInfoFeatureComparer();

            if (TryGetPackage(assemblyName.Name, nugetCache, out DirectoryInfo package))
            {
                var packageVersions =
                    GetPackageVersions(package)
                        .Where(pv => validRuntimes.Contains(pv.Key.runtime))
                        .OrderByDescending(pv => pv.Key.version)
                        .ThenByDescending(pv => pv.Key.runtime, comparer);

                foreach (var packageVersion in packageVersions)
                {
                    var candidates = packageVersion.Value.EnumerateFiles($"{assemblyName.Name}.dll", SearchOption.AllDirectories).ToList();
                    var candidateAssemblyNames = candidates.Select(fileInfo =>
                    {
                        AssemblyHelper.TryGetAssemblyName(fileInfo, out AssemblyName asmName);
                        return (asmName, fileInfo);
                    }).Where(t => t.asmName != null && AssemblyHelper.ValidateArchitecture(t.asmName)).ToList();

                    foreach (var candidate in candidateAssemblyNames)
                    {
                        if (!matchExact || AssemblyHelper.ValidateByName(assemblyName, candidate.asmName))
                        {
                            yield return candidate.fileInfo;
                        }
                    }
                }
            }
        }

        internal static bool TryGetPackage(String name, DirectoryInfo cache, out DirectoryInfo package)
        {
            package = null;

            try
            {
                package = cache.EnumerateDirectories(name, SearchOption.TopDirectoryOnly).FirstOrDefault();
            }
            catch {}

            return package != null && package.Exists;
        }

        internal static IDictionary<(Version version, RuntimeInfo runtime, ProcessorArchitecture arch), DirectoryInfo> GetPackageVersions(DirectoryInfo package)
        {
            var packageVersions = new Dictionary<(Version, RuntimeInfo, ProcessorArchitecture), DirectoryInfo>();

            if (package != null && package.Exists)
            {
                foreach (var semVer in package.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    if (Version.TryParse(semVer.Name, out Version version))
                    {
                        GetPackageVersionRuntimes(new DirectoryInfo(Path.Combine(semVer.FullName, "lib")))
                            .Foreach(kvp => packageVersions.Add((version, kvp.Key, ProcessorArchitecture.MSIL), kvp.Value));

                        GetPackageVersionRuntimes(new DirectoryInfo(Path.Combine(semVer.FullName, "lib", "x86")))
                            .Foreach(kvp => packageVersions.Add((version, kvp.Key, ProcessorArchitecture.X86), kvp.Value));

                        GetPackageVersionRuntimes(new DirectoryInfo(Path.Combine(semVer.FullName, "lib", "x64")))
                            .Foreach(kvp => packageVersions.Add((version, kvp.Key, ProcessorArchitecture.Amd64), kvp.Value));
                    }
                }
            }

            return packageVersions;
        }

        internal static IDictionary<RuntimeInfo, DirectoryInfo> GetPackageVersionRuntimes(DirectoryInfo lib)
        {
            var packageVersions = new Dictionary<RuntimeInfo, DirectoryInfo>();

            if (lib != null && lib.Exists)
            {
                foreach (var targetFramework in lib.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    if (RuntimesHelper.TryParseTFM(targetFramework.Name, out RuntimeInfo runtime))
                    {
                        packageVersions.Add(runtime, targetFramework);
                    }
                }
            }

            return packageVersions;
        }
    }
}