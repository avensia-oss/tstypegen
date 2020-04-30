using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace TSTypeGen
{
    public class Processor
    {
        private readonly string _projectOrSolutionPath;
        private readonly Config _config;

        private static Solution _solution;
        private HashSet<ProjectId> _projectIds;
        private ImmutableDictionary<ProjectId, TypeBuilderConfig> _typeBuilderConfigs;
        private GetSourceConfig _getSourceConfig;

        public bool IsWatching { get; private set; }

        public Processor(string projectOrSolutionPath, Config config) {
            _projectOrSolutionPath = projectOrSolutionPath;
            _config = config;
        }

        private static IEnumerable<INamedTypeSymbol> GetSelfAndNestedTypes(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol)
            {
                yield return (INamedTypeSymbol)type;
            }

            foreach (var t in type.GetTypeMembers().SelectMany(GetSelfAndNestedTypes))
            {
                yield return t;
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetTypesInAssembly(INamespaceOrTypeSymbol nmspace, IAssemblySymbol assembly)
        {
            foreach (var namespaceOrType in nmspace.GetMembers())
            {
                if (namespaceOrType is INamespaceSymbol)
                {
                    foreach (var t in GetTypesInAssembly((INamespaceSymbol)namespaceOrType, assembly))
                    {
                        yield return t;
                    }
                }
                else
                {
                    if (Equals(namespaceOrType.ContainingAssembly, assembly))
                    {
                        foreach (var t in GetSelfAndNestedTypes((ITypeSymbol)namespaceOrType))
                        {
                            yield return t;
                        }
                    }
                }
            }
        }

        private bool ShouldTypeBeGenerated(INamedTypeSymbol type)
        {
            while (type != null)
            {
                var gtsda = type.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == Program.GenerateTypeScriptDefinitionAttributeName);
                if (gtsda != null)
                {
                    return gtsda.ConstructorArguments.Length == 0 || !(gtsda.ConstructorArguments[0].Value is bool value) || value;
                }

                if (type.GetAttributes().Any(a => a.AttributeClass.Name == Program.GenerateTypeScriptDefinitionAttributeName) || type.GetAttributes().Any(a => a.AttributeClass.Name == Program.GenerateTypeScriptNamespaceAttributeName))
                    return true;
                type = type.ContainingType;
            }

            return false;
        }

        private static IEnumerable<INamedTypeSymbol> GetDeclaredTypes(Compilation compilation)
        {
            return GetTypesInAssembly(compilation.GlobalNamespace, compilation.Assembly);
        }

        public static string GetTypeFilePath(INamedTypeSymbol type)
        {
            string filePath = type.DeclaringSyntaxReferences.First().SyntaxTree.FilePath;
            string directory = Path.GetDirectoryName(filePath);
            string name = type.Name;
            while (type.ContainingType != null)
            {
                type = type.ContainingType;
                name = type.Name + "+" + name;
            }

            return Path.Combine(directory, name + ".type.ts");
        }

        private async Task<bool> OpenSolutionAsync()
        {
            try
            {
                var ws = MSBuildWorkspace.Create();
                if (_projectOrSolutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                {
                    _solution = await ws.OpenSolutionAsync(_projectOrSolutionPath);
                    _projectIds = new HashSet<ProjectId>(_solution.Projects.Select(p => p.Id));
                }
                else
                {
                    var project = await ws.OpenProjectAsync(_projectOrSolutionPath);
                    _solution = ws.CurrentSolution;
                    _projectIds = new HashSet<ProjectId> { project.Id };
                }

                bool result = true;
                foreach (var d in ws.Diagnostics.Where(d => d.Kind == WorkspaceDiagnosticKind.Failure))
                {
                    Program.WriteError($"{d.Message}");
                    result = false;
                }
                return result;
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.Error.WriteLine($"Error loading one or more types when opening the solution file {_projectOrSolutionPath}");
                foreach (var lex in ex.LoaderExceptions)
                {
                    Console.Error.Write(lex.ToString());
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to open solution file {_projectOrSolutionPath}: {ex.Message}");
                return false;
            }
        }

        private static readonly HashSet<string> DirectoriesToSkip = new HashSet<string>(StringComparer.InvariantCulture) { ".git", "node_modules", "packages" };

        private void FindGeneratedFilesRecursive(string path, List<string> result)
        {
            foreach (string d in Directory.GetDirectories(path).Where(d => !DirectoriesToSkip.Contains(Path.GetFileName(d))))
            {
                FindGeneratedFilesRecursive(d, result);
            }

            result.AddRange(Directory.GetFiles(path, "*.type.ts"));
        }

        private async Task<IEnumerable<Tuple<ProjectId, GeneratedFile>>> GetFilesToGenerateAsync(Project project)
        {
            var compilation = await project.GetCompilationAsync();

            IEnumerable<Tuple<ProjectId, GeneratedFile>> GetFiles(IEnumerable<IGrouping<string, INamedTypeSymbol>> typesByNamespace)
            {
                foreach (var item in typesByNamespace)
                {
                    if (item.Key == null)
                    {
                        foreach (var t in item)
                        {
                            yield return Tuple.Create(project.Id, (GeneratedFile)CreateGeneratedModuleFile(t));
                        }
                    }
                    else
                    {
                        yield return Tuple.Create(project.Id, (GeneratedFile)new GeneratedNamespaceFile(GetGeneratedNamespaceFilePath(project, item.Key), item.Key, ImmutableList.CreateRange(item)));
                    }
                }
            }

            return GetFiles(GetDeclaredTypes(compilation).Where(ShouldTypeBeGenerated).GroupBy(GetTypescriptNamespace));
        }

        private string GetGeneratedNamespaceFilePath(Project project, string namespaceName)
        {
            return Path.Combine(Path.GetDirectoryName(project.FilePath), namespaceName + ".d.ts");
        }

        private GeneratedModuleFile CreateGeneratedModuleFile(INamedTypeSymbol type)
        {
            return new GeneratedModuleFile(GetTypeFilePath(type), type);
        }

        private async Task<List<Tuple<ProjectId, GeneratedFile>>> GetFilesToGenerateAsync()
        {
            var result = new List<Tuple<ProjectId, GeneratedFile>>();
            foreach (var p in _solution.Projects.Where(p => _projectIds.Contains(p.Id)))
            {
                result.AddRange(await GetFilesToGenerateAsync(p));
            }
            return result;
        }

        private async Task CreateTypeBuilderConfigAsync()
        {
            var propertyTypeReference = !string.IsNullOrEmpty(_config.PropertyTypeDefinitionFile)
                ? TsTypeReference.DefaultImportedType("Property", Path.GetFullPath(Path.Combine(_config.BasePath, _config.PropertyTypeDefinitionFile)))
                : TsTypeReference.Simple(_config.PropertyTypeName);

            _typeBuilderConfigs = ImmutableDictionary<ProjectId, TypeBuilderConfig>.Empty;
            foreach (var project in _solution.Projects.Where(p => _projectIds.Contains(p.Id)))
            {
                var typeMappings = new Dictionary<string, TsTypeReference>();

                void AddTypeMappingsFromAssembly(ISymbol asm)
                {
                    foreach (var attribute in asm.GetAttributes().Where(a => a.AttributeClass.Name == Program.DefineTypeScriptTypeForExternalTypeAttributeName && a.ConstructorArguments.Length == 2 && a.ConstructorArguments[1].Value is string))
                    {
                        string key;
                        if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol nt)
                        {
                            key = nt.ToDisplayString();
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

                var compilation = await project.GetCompilationAsync();
                AddTypeMappingsFromAssembly(compilation.Assembly);
                foreach (var reference in compilation.References)
                {
                    AddTypeMappingsFromAssembly(compilation.GetAssemblyOrModuleSymbol(reference));
                }

                // override mappings from config
                foreach (var mapping in _config.TypeMappings)
                {
                    TsTypeReference reference;
                    if (mapping.Value.Contains('/') || mapping.Value.Contains('\\'))
                    {
                        var lastDot = mapping.Key.LastIndexOf('.');
                        var name = lastDot >= 0 ? mapping.Key.Substring(lastDot + 1) : mapping.Key;
                        var path = mapping.Value[0] == '.' ? Path.GetFullPath(Path.Combine(_config.BasePath, mapping.Value)) : mapping.Value;
                        reference = TsTypeReference.DefaultImportedType(name, path);
                    }
                    else
                    {
                        reference = TsTypeReference.Simple(mapping.Value);
                    }

                    typeMappings[mapping.Key] = reference;
                }

                var typeBuildConfig = new TypeBuilderConfig(
                    ImmutableDictionary.CreateRange(typeMappings),
                    propertyTypeReference,
                    _config.TypesToWrapPropertiesFor,
                    _config.StructuralSubsetOfInterfaceFullName
                );
                _typeBuilderConfigs = _typeBuilderConfigs.Add(project.Id, typeBuildConfig);
            }
        }

        private void CreateGetSourceConfig()
        {
            string rootPath = !string.IsNullOrEmpty(_config.RootPath) ? EnsureTrailingBackslash(Path.GetFullPath(Path.Combine(_config.BasePath, _config.RootPath))) : null;

            var pathAliases = new Dictionary<string, string>();
            foreach (var alias in _config.PathAliases)
            {
                pathAliases[alias.Key] = EnsureTrailingBackslash(Path.GetFullPath(Path.Combine(_config.BasePath, alias.Value)));
            }

            _getSourceConfig = new GetSourceConfig(rootPath, ImmutableDictionary.CreateRange(pathAliases), _config.UseConstEnums, _config.UseOptionalForNullables);
        }

        private static string EnsureTrailingBackslash(string s)
        {
            return s.EndsWith("\\") ? s : (s + '\\');
        }

        private async Task<bool> OpenSolutionAndUpdateOrVerifyAllTypes(bool verify)
        {
            if (!await OpenSolutionAsync())
                return false;

            await CreateTypeBuilderConfigAsync();
            CreateGetSourceConfig();

            var generatedFiles = await GetFilesToGenerateAsync();

            bool success = true;
            foreach (var fileToGenerate in generatedFiles)
            {
                if (verify)
                    success &= await fileToGenerate.Item2.VerifyAsync(_typeBuilderConfigs[fileToGenerate.Item1], _getSourceConfig, _solution);
                else
                    await fileToGenerate.Item2.ApplyAsync(_typeBuilderConfigs[fileToGenerate.Item1], _getSourceConfig, _solution);
            }

            var existingGeneratedFiles = new List<string>();
            FindGeneratedFilesRecursive(Path.GetDirectoryName(_projectOrSolutionPath), existingGeneratedFiles);
            var extraFiles = existingGeneratedFiles.Except(generatedFiles.Select(f => f.Item2.FilePath), StringComparer.OrdinalIgnoreCase);

            foreach (var extraFile in extraFiles)
            {
                if (verify)
                {
                    Program.WriteError($"Generated type in file {extraFile} does have a corresponding source. Run the frontend build and commit all changes to generated types.");
                    success = false;
                }
                else
                {
                    FileUtils.Delete(extraFile);
                    Console.WriteLine($"Deleted file {extraFile}.");
                }
            }

            return success;
        }

        private Task<bool> OpenSolutionAndUpdateAllTypes()
        {
            return OpenSolutionAndUpdateOrVerifyAllTypes(verify: false);
        }

        private Task<bool> OpenSolutionAndVerifyAllTypes()
        {
            return OpenSolutionAndUpdateOrVerifyAllTypes(verify: true);
        }

        private async Task<IEnumerable<Tuple<ProjectId, INamedTypeSymbol>>> GetTypesInDocumentAsync(DocumentId docId)
        {
            var doc = _solution.GetDocument(docId);
            var compilation = await doc.Project.GetCompilationAsync();
            var syntaxTree = await _solution.GetDocument(docId).GetSyntaxTreeAsync();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var gatherer = new TypeDeclarationGatherer(semanticModel);
            gatherer.Visit(syntaxTree.GetRoot());
            return gatherer.Result.Where(ShouldTypeBeGenerated).Select(t => Tuple.Create(doc.Project.Id, t));
        }

        private Task<bool> DoWatch(CancellationToken cancellationToken)
        {
            bool success = true;

            var cts = new TaskCompletionSource<bool>();

            var innerCancellation = new CancellationTokenSource();
            var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCancellation.Token);

            FileWatcher.WatchPath(Path.GetDirectoryName(_projectOrSolutionPath), TimeSpan.FromMilliseconds(100), linkedCancellation.Token,
                async change =>
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(change.FullPath, _projectOrSolutionPath))
                    {
                        if (!await OpenSolutionAndUpdateAllTypes())
                        {
                            success = false;
                            innerCancellation.Cancel();
                            return;
                        }
                    }
                    else if (change.FullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    {
                        var modifiedProject = _solution.Projects.FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals(p.FilePath, change.FullPath));
                        if (modifiedProject != null)
                        {
                            // It would be nice to only reload the modified project, but that is not possible: https://github.com/dotnet/roslyn/issues/7842
                            if (!await OpenSolutionAndUpdateAllTypes())
                            {
                                success = false;
                                innerCancellation.Cancel();
                                return;
                            }
                        }
                    }
                    else if (change.FullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        var docIds = _solution.GetDocumentIdsWithFilePath(change.FullPath);
                        if (docIds.Length > 0)
                        {
                            var data = FileUtils.ReadAllBytesSafe(change.FullPath);
                            var modulesToUpdate = new Dictionary<INamedTypeSymbol, ProjectId>();
                            var filesToDelete = new HashSet<string>();
                            var namespacesToUpdate = new HashSet<Tuple<ProjectId, string>>();
                            foreach (var docId in _solution.GetDocumentIdsWithFilePath(change.FullPath))
                            {
                                foreach (var projectAndType in await GetTypesInDocumentAsync(docId))
                                {
                                    if (ShouldTypeBeGenerated(projectAndType.Item2))
                                    {
                                        if (GetTypescriptNamespace(projectAndType.Item2) is string nmspace)
                                        {
                                            namespacesToUpdate.Add(Tuple.Create(projectAndType.Item1, nmspace));
                                        }
                                        else
                                        {
                                            filesToDelete.Add(GetTypeFilePath(projectAndType.Item2));
                                        }
                                    }
                                }

                                _solution = _solution.WithDocumentText(docId, SourceText.From(data, data.Length));

                                foreach (var projectAndType in await GetTypesInDocumentAsync(docId))
                                {
                                    if (ShouldTypeBeGenerated(projectAndType.Item2))
                                    {
                                        if (GetTypescriptNamespace(projectAndType.Item2) is string nmspace)
                                        {
                                            namespacesToUpdate.Add(Tuple.Create(projectAndType.Item1, nmspace));
                                        }
                                        else
                                        {
                                            modulesToUpdate[projectAndType.Item2] = projectAndType.Item1;
                                        }
                                    }
                                }
                            }

                            foreach (var typeAndProject in modulesToUpdate)
                            {
                                var generatedFile = CreateGeneratedModuleFile(typeAndProject.Key);
                                filesToDelete.Remove(generatedFile.FilePath);
                                await generatedFile.ApplyAsync(_typeBuilderConfigs[typeAndProject.Value], _getSourceConfig, _solution);
                            }

                            foreach (var nmspace in namespacesToUpdate)
                            {
                                var project = _solution.GetProject(nmspace.Item1);
                                if (project != null)
                                {
                                    var compilation = await project.GetCompilationAsync();
                                    var types = ImmutableList.CreateRange(GetDeclaredTypes(compilation).Where(t => ShouldTypeBeGenerated(t) && GetTypescriptNamespace(t) == nmspace.Item2));
                                    var path = GetGeneratedNamespaceFilePath(project, nmspace.Item2);
                                    if (types.Count > 0)
                                    {
                                        var generatedFile = new GeneratedNamespaceFile(path, nmspace.Item2, types);
                                        await generatedFile.ApplyAsync(_typeBuilderConfigs[nmspace.Item1], _getSourceConfig, _solution);
                                    }
                                    else
                                    {
                                        filesToDelete.Add(path);
                                    }
                                }
                            }

                            foreach (var f in filesToDelete)
                            {
                                FileUtils.Delete(f);
                                Console.WriteLine($"Deleted file {f}.");
                            }
                        }
                    }
                },
                exception =>
                {
                    Program.WriteError(exception.Message);
                    success = false;
                    innerCancellation.Cancel();
                },
                () =>
                {
                    IsWatching = true;
                }
            ).ContinueWith(_ =>
            {
                try
                {
                    IsWatching = false;
                    innerCancellation.Dispose();
                    linkedCancellation.Dispose();
                    cts.SetResult(success);
                }
                catch (Exception ex)
                {
                    cts.SetException(ex);
                }
            });

            return cts.Task;
        }

        public async Task<bool> UpdateTypesAsync()
        {
            return await OpenSolutionAndUpdateAllTypes();
        }

        public async Task<bool> VerifyTypesAsync()
        {
            return await OpenSolutionAndVerifyAllTypes();
        }

        public async Task<bool> WatchAsync(CancellationToken cancellationToken)
        {
            if (!await OpenSolutionAndUpdateAllTypes())
                return false;

            var result = await DoWatch(cancellationToken);

            return result;
        }

        internal static string GetTypescriptNamespace(ITypeSymbol type)
        {
            string DoGetTypescriptNamespace(ISymbol symbol)
            {
                var attr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == Program.GenerateTypeScriptNamespaceAttributeName && a.ConstructorArguments.Length == 1 && a.ConstructorArguments[0].Value is string);
                return attr != null ? (string)attr.ConstructorArguments[0].Value : null;
            }

            for (var currentType = type; currentType != null; currentType = currentType.ContainingType)
            {
                var ns = DoGetTypescriptNamespace(currentType);
                if (ns != null)
                {
                    return ns;
                }
            }

            return DoGetTypescriptNamespace(type.ContainingAssembly);
        }
    }
}
