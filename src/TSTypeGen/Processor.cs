﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
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

        private bool LoadAllDlls()
        {
            try
            {
                var matcher = new Matcher();
                foreach (var pattern in _config.DllPatterns)
                {
                    matcher.AddInclude(pattern.Trim());
                }

                var loadedPaths = new List<string>();
                var assemblies = new List<(Assembly Assembly, string XmlCommentsFile)>();
                var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(_config.BasePath)));

                var addedDlls = new List<string>();
                var dllPaths = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll").ToList();

                foreach (var dll in result.Files)
                {
                    var fullPath = Path.Join(_config.BasePath, dll.Path);
                    var fileName = Path.GetFileName(fullPath);
                    if (!addedDlls.Contains(fileName))
                    {
                        dllPaths.Add(fullPath);
                        addedDlls.Add(fileName);
                    }
                }

                var resolver = new CustomMetadataAssemblyResolver(new PathAssemblyResolver(dllPaths));
                var mlc = new MetadataLoadContext(resolver);

                foreach (var dll in result.Files)
                {
                    if (!dll.Path.EndsWith(".dll"))
                    {
                        Console.WriteLine($"The file {dll.Path} matched the pattern but is not a dll, skipping");
                    }
                    else
                    {
                        var fullPath = Path.Join(_config.BasePath, dll.Path);
                        if (loadedPaths.Contains(fullPath))
                            continue;

                        loadedPaths.Add(fullPath);

                        try
                        {
                            var assembly = mlc.LoadFromAssemblyPath(fullPath);

                            var xmlCommentsFilePath = Path.ChangeExtension(fullPath, ".xml");
                            if (!File.Exists(xmlCommentsFilePath))
                            {
                                xmlCommentsFilePath = null;
                            }

                            if (!assemblies.Contains((assembly, xmlCommentsFilePath)))
                                assemblies.Add((assembly, xmlCommentsFilePath));
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Could not load dll file " + fullPath + ", skipping...");
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
