using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace TSTypeGen
{
    public class Processor
    {
        private readonly Config _config;

        private TypeBuilderConfig _typeBuilderConfig;
        private GeneratorContext _generatorContext;

        public Processor(Config config)
        {
            _config = config;
        }

        private bool ShouldTypeBeGenerated(Type type)
        {
            while (type != null)
            {
                var customAttributes = TypeUtils.GetCustomAttributesData(type);
                var gtsda = customAttributes.FirstOrDefault(a => a.AttributeType.Name == Constants.GenerateTypeScriptDefinitionAttributeName);
                if (gtsda != null)
                {
                    return gtsda.ConstructorArguments.Count == 0 || !(gtsda.ConstructorArguments[0].Value is bool value) || value;
                }

                if (customAttributes.Any(a => a.AttributeType.Name == Constants.GenerateTypeScriptDefinitionAttributeName) || customAttributes.Any(a => a.AttributeType.Name == Constants.GenerateTypeScriptNamespaceAttributeName))
                    return true;

                type = type.DeclaringType;
            }

            return false;
        }

        private List<string> GetRuntimeAssembliesForFrameworkVersion(string version)
        {
            var targetFrameworkMatch = Regex.Match(version, "^v([0-9]+)\\.([0-9]+)$");
            if (!targetFrameworkMatch.Success)
            {
                Console.Error.WriteLine($"Invalid target framework. Expected v<x>.<y>, was {version}");
                return null;
            }
            // This is fishy, but I can't find another way to locate all required framework assemblies.
            var dotnetDirectory = Path.GetDirectoryName(RuntimeEnvironment.GetRuntimeDirectory().TrimEnd('\\', '/'));
            var aspnetDirectory = Path.Combine(Path.GetDirectoryName(dotnetDirectory), "Microsoft.AspNetCore.App");
            var dotnetVersions = Directory.GetDirectories(dotnetDirectory).Select(Path.GetFileName);
            var aspnetVersions = Directory.GetDirectories(aspnetDirectory).Select(Path.GetFileName);
            var versionToUse = dotnetVersions.Intersect(aspnetVersions)
                .Select(v => Regex.Match(v, "^([0-9]+)\\.([0-9]+)\\.([0-9]+)$"))
                .Where(m => m.Success && m.Groups[1].Value == targetFrameworkMatch.Groups[1].Value && m.Groups[2].Value == targetFrameworkMatch.Groups[2].Value)
                .MaxBy(m => int.Parse(m.Groups[3].Value))
                ?.Value;

            if (versionToUse == null)
            {
                Console.Error.WriteLine($"Unable to locate an Asp.net runtime compatible with framework {version}");
                return null;
            }

            return Directory.GetFiles(Path.Combine(dotnetDirectory, versionToUse), "*.dll").Concat(Directory.GetFiles(Path.Combine(aspnetDirectory, versionToUse), "*.dll")).ToList();
        }

        private ICollection<string> GetFilesMatchingPatterns(string basePath, ICollection<string> patterns)
        {
            var filesByName = new Dictionary<string, string>();
            void TryAddFile(string path)
            {
                var name = Path.GetFileName(path);
                if (filesByName.TryGetValue(name, out var existing))
                {
                    if (existing != path)
                    {
                        Console.WriteLine($"The file {name} was included from both paths {path} and {existing}, choosing {existing}");
                        return;
                    }
                }
                filesByName.Add(name, path);
            }

            foreach (var pattern in patterns)
            {
                if (pattern.Contains('*'))
                {
                    string matchBasePath, matchPattern;
                    if (Path.IsPathRooted(pattern))
                    {
                        var parts = pattern.Split('\\', '/');
                        var fixedParts = parts.TakeWhile(p => !p.Contains('*')).ToList();
                        matchPattern = string.Join(Path.DirectorySeparatorChar, parts.Skip(fixedParts.Count));
                        matchBasePath = string.Join(Path.DirectorySeparatorChar, fixedParts);
                    }
                    else
                    {
                        matchBasePath = basePath;
                        matchPattern = pattern;
                    }
                    var matcher = new Matcher();
                    matcher.AddInclude(matchPattern);
                    foreach (var file in matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(matchBasePath))).Files)
                    {
                        var path = Path.Combine(matchBasePath, file.Path);
                        TryAddFile(path);
                    }
                }
                else
                {
                    var path = Path.IsPathRooted(pattern) ? pattern : Path.Combine(basePath, pattern);
                    TryAddFile(path);
                }
            }
            return filesByName.Values;
        }

        private bool LoadAllDlls()
        {
            List<string> dllPaths;
            if (!string.IsNullOrEmpty(_config.TargetFrameworkVersion))
            {
                dllPaths = GetRuntimeAssembliesForFrameworkVersion(_config.TargetFrameworkVersion);
                if (dllPaths == null)
                {
                    return false;
                }
            }
            else
            {
                dllPaths = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll").ToList();
            }

            try
            {
                var patternPaths = GetFilesMatchingPatterns(_config.BasePath, _config.DllPatterns);
                if (patternPaths == null)
                {
                    return false;
                }

                var loadedPaths = new List<string>();
                var assemblies = new List<(Assembly Assembly, string XmlCommentsFile)>();

                var addedDlls = new List<string>();

                foreach (var dll in patternPaths)
                {
                    var fileName = Path.GetFileName(dll);
                    if (!addedDlls.Contains(fileName))
                    {
                        dllPaths.Add(dll);
                        addedDlls.Add(fileName);
                    }
                }

                // Locate all dlls that exist in a directory in which we have locaed a file to process. This allows generating types for a single file in the output directory
                foreach (var directory in patternPaths.Select(Path.GetDirectoryName).Distinct())
                {
                    foreach (var dll in Directory.GetFiles(directory, "*.dll"))
                    {
                        var fileName = Path.GetFileName(dll);
                        if (!addedDlls.Contains(fileName))
                        {
                            dllPaths.Add(dll);
                            addedDlls.Add(fileName);
                        }
                    }
                }

                var resolver = new CustomMetadataAssemblyResolver(new PathAssemblyResolver(dllPaths), _config.PackagesDirectories);
                var mlc = new MetadataLoadContext(resolver);

                foreach (var dll in patternPaths)
                {
                    if (!dll.EndsWith(".dll"))
                    {
                        Console.WriteLine($"The file {dll} matched the pattern but is not a dll, skipping");
                    }
                    else
                    {
                        if (loadedPaths.Contains(dll))
                            continue;

                        loadedPaths.Add(dll);

                        try
                        {
                            var assembly = mlc.LoadFromAssemblyPath(dll);

                            var xmlCommentsFilePath = Path.ChangeExtension(dll, ".xml");
                            if (!File.Exists(xmlCommentsFilePath))
                            {
                                xmlCommentsFilePath = null;
                            }

                            if (!assemblies.Contains((assembly, xmlCommentsFilePath)))
                                assemblies.Add((assembly, xmlCommentsFilePath));
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Could not load dll file " + dll + ", skipping...");
                        }
                    }
                }

                if (!assemblies.Any())
                {
                    Console.Error.WriteLine($"No dlls were found matching the pattern {string.Join(", ", _config.DllPatterns)}");
                    return false;
                }

                _generatorContext = new GeneratorContext(assemblies);

                return true;
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.Error.WriteLine($"Error loading one or more types when opening the dll files in pattern {string.Join(", ", _config.DllPatterns)}");
                foreach (var lex in ex.LoaderExceptions)
                {
                    Console.Error.Write(lex.ToString());
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to open the dll files in pattern {string.Join(", ", _config.DllPatterns)}: {ex.Message}");
                return false;
            }
        }

        private Dictionary<string, List<Type>> GetFilesToGenerate(Assembly assembly)
        {
            var typesPerNamespace = new Dictionary<string, List<Type>>();
            foreach (var type in TypeUtils.GetAssemblyTypes(assembly))
            {
                if (ShouldTypeBeGenerated(type))
                {
                    var namespaceName = TypeBuilder.GetTypescriptNamespace(type);
                    if (string.IsNullOrEmpty(namespaceName))
                    {
                        Console.Error.WriteLine($"The type {type.FullName} is marked to generate TypeScript definition but no TypeScript namespace could be found for it");
                        continue;
                    }

                    if (!typesPerNamespace.ContainsKey(namespaceName))
                        typesPerNamespace.Add(namespaceName, new List<Type>());

                    typesPerNamespace[namespaceName].Add(type);
                }
            }

            return typesPerNamespace;
        }

        private string GetGeneratedNamespaceFilePath(string namespaceName)
        {
            return Path.Combine(_config.OutputPath, namespaceName + ".d.ts");
        }

        private List<GeneratedNamespaceFile> GetFilesToGenerate()
        {
            var result = new List<GeneratedNamespaceFile>();
            var typesPerNamespace = new Dictionary<string, List<Type>>();
            foreach (var asm in _generatorContext.Assemblies)
            {
                var namespaceTypes = GetFilesToGenerate(asm);
                foreach (var (ns, types) in namespaceTypes)
                {
                    if (!typesPerNamespace.ContainsKey(ns))
                        typesPerNamespace.Add(ns, new List<Type>());

                    typesPerNamespace[ns].AddRange(types);
                }
            }

            foreach (var (ns, types) in typesPerNamespace)
            {
                result.Add(new GeneratedNamespaceFile(GetGeneratedNamespaceFilePath(ns), ns, ImmutableList.CreateRange(types)));
            }

            return result;
        }

        private void CreateTypeBuilderConfig()
        {
            var typeMappings = new Dictionary<string, TsTypeReference>();

            void AddTypeMappingsFromAssembly(Assembly asm)
            {
                foreach (var attribute in TypeUtils.GetAssemblyCustomAttributesData(asm).Where(a =>
                    a.AttributeType.Name == Constants.DefineTypeScriptTypeForExternalTypeAttributeName && a.ConstructorArguments.Count == 2 && a.ConstructorArguments[1].Value is string))
                {
                    string key;
                    if (attribute.ConstructorArguments[0].Value is Type type)
                    {
                        key = TypeUtils.GetFullNameWithGenericArguments(type);
                    }
                    else if (attribute.ConstructorArguments[0].Value is string s)
                    {
                        key = s;
                    }
                    else
                    {
                        continue;
                    }

                    typeMappings[key] = TsTypeReference.Simple((string)attribute.ConstructorArguments[1].Value);
                }
            }

            foreach (var assembly in _generatorContext.Assemblies)
            {
                AddTypeMappingsFromAssembly(assembly);
            }

            // override mappings from config
            foreach (var mapping in _config.TypeMappings)
            {
                var reference = TsTypeReference.Simple(mapping.Value);
                typeMappings[mapping.Key] = reference;
            }

            var mappings = ImmutableDictionary.CreateRange(typeMappings);
            _typeBuilderConfig = new TypeBuilderConfig(mappings, _config.CustomTypeScriptIgnoreAttributeFullName, _config.WrapConstEnumsInTemplateStrings, _config.GenerateInterfaceProperties);
        }

        private async Task<bool> LoadAllDllsAndUpdateOrVerifyAllTypes(bool verify)
        {
            if (!LoadAllDlls())
                return false;

            CreateTypeBuilderConfig();

            var generatedFiles = GetFilesToGenerate();

            bool success = true;
            foreach (var fileToGenerate in generatedFiles)
            {
                if (verify)
                    success &= await fileToGenerate.VerifyAsync(_typeBuilderConfig, _config, _generatorContext);
                else
                    await fileToGenerate.ApplyAsync(_typeBuilderConfig, _config, _generatorContext);
            }

            if (_config.UseEmbeddedDeclarations)
            {
                foreach (var asm in _generatorContext.Assemblies)
                {
                    var embeddedDeclarations = GetEmbeddedDeclarations(asm);
                    if (embeddedDeclarations != null)
                    {
                        foreach (var name in embeddedDeclarations.Keys)
                        {
                            var path = GetGeneratedNamespaceFilePath(name.Replace(".d.ts", ""));
                            File.WriteAllText(path, embeddedDeclarations[name]);
                        }
                    }
                }
            }

            return success;
        }

        private Dictionary<string, string> GetEmbeddedDeclarations(Assembly assembly)
        {
            var resourceNames = assembly
                .GetManifestResourceNames()
                .Where(r => r.EndsWith(".d.ts"))
                .ToArray();

            if (resourceNames.Length == 0)
                return null;

            var declarations = new Dictionary<string, string>();

            foreach (var resourceName in resourceNames)
            {
                var info = assembly.GetManifestResourceInfo(resourceName);
                using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
                {
                    var declaration = reader.ReadToEnd();
                    declarations.Add(resourceName, declaration);
                }
            }

            return declarations;
        }

        private Task<bool> LoadAllDllsAndUpdateAllTypes()
        {
            return LoadAllDllsAndUpdateOrVerifyAllTypes(verify: false);
        }

        private Task<bool> LoadAllDllsAndVerifyAllTypes()
        {
            return LoadAllDllsAndUpdateOrVerifyAllTypes(verify: true);
        }

        public async Task<bool> UpdateTypesAsync()
        {
            return await LoadAllDllsAndUpdateAllTypes();
        }

        public async Task<bool> VerifyTypesAsync()
        {
            return await LoadAllDllsAndVerifyAllTypes();
        }
    }
}
