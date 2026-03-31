using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace TSTypeGen
{
    public class GeneratedNamespaceFile
    {
        public string FilePath { get; }
        private readonly string _namespaceName;
        private readonly ImmutableList<Type> _types;

        public GeneratedNamespaceFile(string filePath, string namespaceName, ImmutableList<Type> types)
        {
            FilePath = filePath;
            _namespaceName = namespaceName;
            _types = types;
        }

        protected async Task<string> GetContentAsync(TypeBuilderConfig typeBuilderConfig, Config config, GeneratorContext generatorContext)
        {
            bool first = true;

            var innerSource = new StringBuilder();
            foreach (var t in _types.OrderBy(t => t.Name, StringComparer.InvariantCulture).ThenBy(t => t.FullName, StringComparer.InvariantCulture))
            {
                if (!first)
                {
                    innerSource.Append(config.NewLine);
                }
                var tsTypeDefinition = await TypeBuilder.BuildTsTypeDefinitionAsync(t, typeBuilderConfig, generatorContext);
                innerSource.Append(tsTypeDefinition.GetSource(FilePath, config, generatorContext));
                first = false;
            }

            return "declare namespace " + _namespaceName + " {" + config.NewLine + innerSource + "}" + config.NewLine;
        }

        public async Task ApplyAsync(TypeBuilderConfig typeBuilderConfig, Config config, GeneratorContext generatorContext)
        {
            bool exists = File.Exists(FilePath);
            var origContent = exists ? ReadAllTextSafe(FilePath) : "";
            var newContent = await GetContentAsync(typeBuilderConfig, config, generatorContext);

            if (newContent != origContent)
            {
                await File.WriteAllTextAsync(FilePath, newContent);
                Console.WriteLine($"{(exists ? "Updated" : "Created")} file {FilePath}.");
            }

            await ApplyCompanionFileAsync(GetConstsFilePath(), BuildConstsSource(newContent, config.NewLine));
            await ApplyCompanionFileAsync(GetTypeNamesFilePath(), BuildTypeNamesSource(newContent, config.NewLine));
            await ApplyCompanionFileAsync(GetTypeNamesJsonFilePath(), BuildTypeNamesJsonSource(newContent));
            await ApplyCompanionFileAsync(GetEnumsFilePath(), BuildEnumsSource(newContent, config.NewLine));
        }

        public async Task<bool> VerifyAsync(TypeBuilderConfig typeBuilderConfig, Config config, GeneratorContext generatorContext)
        {
            if (!File.Exists(FilePath))
            {
                Program.WriteError($"File {FilePath} does not exist.");
                return false;
            }

            try
            {
                var origContent = await File.ReadAllTextAsync(FilePath);
                var newContent = await GetContentAsync(typeBuilderConfig, config, generatorContext);
                if (newContent.Replace("\r\n", "\n") != origContent.Replace("\r\n" ,"\n"))
                {
                    Program.WriteError($"Generated file {FilePath} does not match the source definition. Run the frontend build and commit all changes to generated types.");
                    return false;
                }

                if (!await VerifyCompanionFileAsync(GetConstsFilePath(), BuildConstsSource(newContent, config.NewLine)))
                    return false;

                if (!await VerifyCompanionFileAsync(GetTypeNamesFilePath(), BuildTypeNamesSource(newContent, config.NewLine)))
                    return false;

                if (!await VerifyCompanionFileAsync(GetTypeNamesJsonFilePath(), BuildTypeNamesJsonSource(newContent)))
                    return false;

                if (!await VerifyCompanionFileAsync(GetEnumsFilePath(), BuildEnumsSource(newContent, config.NewLine)))
                    return false;
            }
            catch (Exception ex)
            {
                Program.WriteError($"Error verifying generated file {FilePath}: {ex.Message}");
                return false;
            }

            return true;
        }

        public static string ReadAllTextSafe(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string NormalizeNewLines(string s)
        {
            return s.Replace("\r\n", "\n");
        }

        private string GetBaseFilePathWithoutDeclarationSuffix()
        {
            if (FilePath.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase))
                return FilePath.Substring(0, FilePath.Length - ".d.ts".Length);

            return Path.Combine(Path.GetDirectoryName(FilePath) ?? "", Path.GetFileNameWithoutExtension(FilePath));
        }

        private string GetConstsFilePath()
        {
            return GetBaseFilePathWithoutDeclarationSuffix() + ".consts.ts";
        }

        private string GetTypeNamesFilePath()
        {
            return GetBaseFilePathWithoutDeclarationSuffix() + ".typeNames.ts";
        }

        private string GetEnumsFilePath()
        {
            return GetBaseFilePathWithoutDeclarationSuffix() + ".enums.ts";
        }

        private string GetTypeNamesJsonFilePath()
        {
            return GetBaseFilePathWithoutDeclarationSuffix() + ".typeNames.json";
        }

        private async Task ApplyCompanionFileAsync(string filePath, string newContent)
        {
            if (string.IsNullOrEmpty(newContent))
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine($"Deleted file {filePath}.");
                }

                return;
            }

            var exists = File.Exists(filePath);
            var oldContent = exists ? await File.ReadAllTextAsync(filePath) : "";
            if (NormalizeNewLines(oldContent) != NormalizeNewLines(newContent))
            {
                await File.WriteAllTextAsync(filePath, newContent);
                Console.WriteLine($"{(exists ? "Updated" : "Created")} file {filePath}.");
            }
        }

        private async Task<bool> VerifyCompanionFileAsync(string filePath, string expectedContent)
        {
            if (string.IsNullOrEmpty(expectedContent))
            {
                if (File.Exists(filePath))
                {
                    Program.WriteError($"Generated file {filePath} should not exist.");
                    return false;
                }

                return true;
            }

            if (!File.Exists(filePath))
            {
                Program.WriteError($"File {filePath} does not exist.");
                return false;
            }

            var actualContent = await File.ReadAllTextAsync(filePath);
            if (NormalizeNewLines(actualContent) != NormalizeNewLines(expectedContent))
            {
                Program.WriteError($"Generated file {filePath} does not match the source definition. Run the frontend build and commit all changes to generated types.");
                return false;
            }

            return true;
        }

        private static string BuildConstsSource(string declarationFileContent, string newLine)
        {
            var enumRegex = new Regex(@"^\s*const enum\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{(?<body>[\s\S]*?)^\s*\}", RegexOptions.Multiline);
            var memberRegex = new Regex(@"^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*'(?<value>[^']*)',?\s*$", RegexOptions.Multiline);
            var matches = enumRegex.Matches(declarationFileContent);
            if (matches.Count == 0)
                return null;

            var declarations = new List<string>();
            foreach (Match enumMatch in matches)
            {
                var enumName = enumMatch.Groups["name"].Value;
                var body = enumMatch.Groups["body"].Value;
                var members = memberRegex.Matches(body);

                var sb = new StringBuilder();
                sb.Append("export const ").Append(enumName).Append(" = {").Append(newLine);
                foreach (Match member in members)
                {
                    sb.Append("  ")
                      .Append(member.Groups["name"].Value)
                      .Append(": '")
                      .Append(EscapeSingleQuotedString(member.Groups["value"].Value))
                      .Append("',")
                      .Append(newLine);
                }
                sb.Append("} as const;").Append(newLine);
                sb.Append("export type ").Append(enumName)
                  .Append(" = (typeof ").Append(enumName).Append(")[keyof typeof ")
                  .Append(enumName).Append("];");
                declarations.Add(sb.ToString());
            }

            return string.Join(newLine + newLine, declarations) + newLine;
        }

        private string BuildTypeNamesSource(string declarationFileContent, string newLine)
        {
            var (typeNameEntries, _) = GetDotNetTypeNameEntries(declarationFileContent);

            if (typeNameEntries.Count == 0)
                return null;

            var sb = new StringBuilder();
            sb.Append("export const dotNetTypeNames = {").Append(newLine);
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in typeNameEntries)
            {
                var key = _namespaceName + "." + entry.Key;
                if (seenKeys.Contains(key))
                    continue;
                seenKeys.Add(key);

                sb.Append("  \"")
                  .Append(EscapeSingleQuotedString(key))
                  .Append("\": '")
                  .Append(EscapeSingleQuotedString(entry.Value))
                  .Append("',")
                  .Append(newLine);
            }
            sb.Append("} as const;").Append(newLine);
            return sb.ToString();
        }

        private static string BuildEnumsSource(string declarationFileContent, string newLine)
        {
            var enumRegex = new Regex(@"^\s*const enum\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{(?<body>[\s\S]*?)^\s*\}", RegexOptions.Multiline);
            var memberRegex = new Regex(@"^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*'(?<value>[^']*)',?\s*$", RegexOptions.Multiline);
            var enumMatches = enumRegex.Matches(declarationFileContent);

            var (typeNameEntries, canonicalNameEntries) = GetDotNetTypeNameEntries(declarationFileContent);

            var declarations = new List<string>();

            foreach (Match enumMatch in enumMatches)
            {
                var enumName = enumMatch.Groups["name"].Value;
                var body = enumMatch.Groups["body"].Value;
                var members = memberRegex.Matches(body);

                var sb = new StringBuilder();
                sb.Append("export const enum ").Append(enumName).Append(" {").Append(newLine);
                foreach (Match member in members)
                {
                    sb.Append("  ")
                      .Append(member.Groups["name"].Value)
                      .Append(" = '")
                      .Append(EscapeSingleQuotedString(member.Groups["value"].Value))
                      .Append("',")
                      .Append(newLine);
                }
                sb.Append("}");
                declarations.Add(sb.ToString());
            }

            if (typeNameEntries.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append("export const enum DotNetTypeNames {").Append(newLine);
                foreach (var entry in typeNameEntries)
                {
                    sb.Append("  ")
                      .Append(SanitizeTypeScriptIdentifier(entry.Key))
                      .Append(" = '")
                      .Append(EscapeSingleQuotedString(entry.Value))
                      .Append("',")
                      .Append(newLine);
                }
                sb.Append("}");
                declarations.Add(sb.ToString());
            }

            if (declarations.Count == 0)
                return null;

            return string.Join(newLine + newLine, declarations) + newLine;
        }

        private static (List<KeyValuePair<string, string>> typeNameEntries, List<KeyValuePair<string, string>> canonicalNameEntries) GetDotNetTypeNameEntries(string declarationFileContent)
        {
            var dotNetTypeNameAndInterfaceRegex = new Regex(@"@DotNetTypeName\s+(?<dotnet>[^\r\n*]+)[\s\S]*?interface\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b", RegexOptions.Multiline);
            var dotNetCanonicalTypeNameAndInterfaceRegex = new Regex(@"@DotNetCanonicalTypeName\s+(?<dotnet>[^\r\n*]+)[\s\S]*?interface\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b", RegexOptions.Multiline);

            var typeNameMatches = dotNetTypeNameAndInterfaceRegex.Matches(declarationFileContent);
            var canonicalNameMatches = dotNetCanonicalTypeNameAndInterfaceRegex.Matches(declarationFileContent);

            var typeNameEntries = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (Match match in typeNameMatches)
            {
                var name = match.Groups["name"].Value;
                var dotNetValue = match.Groups["dotnet"].Value.Trim();

                if (!typeNameEntries.ContainsKey(name))
                {
                    typeNameEntries[name] = dotNetValue;
                }
            }

            foreach (Match match in canonicalNameMatches)
            {
                var name = match.Groups["name"].Value;
                var canonicalValue = match.Groups["dotnet"].Value.Trim();
                typeNameEntries[name] = canonicalValue;
            }

            var orderedEntries = typeNameEntries
                .OrderBy(x => x.Key, StringComparer.InvariantCulture)
                .ToList();

            return (orderedEntries, new List<KeyValuePair<string, string>>());
        }

        private static string EscapeSingleQuotedString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("'", "\\'");
        }

        private static string SanitizeTypeScriptIdentifier(string name)
        {
            var result = new StringBuilder();
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    result.Append(c);
                }
                else
                {
                    result.Append('_');
                }
            }
            return result.ToString();
        }

        private string BuildTypeNamesJsonSource(string declarationFileContent)
        {
            var (typeNameEntries, _) = GetDotNetTypeNameEntries(declarationFileContent);

            if (typeNameEntries.Count == 0)
                return null;

            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            var dict = new Dictionary<string, string>();

            foreach (var entry in typeNameEntries)
            {
                var key = _namespaceName + "." + entry.Key;
                if (seenKeys.Contains(key))
                    continue;
                seenKeys.Add(key);
                dict[key] = entry.Value;
            }

            return System.Text.Json.JsonSerializer.Serialize(dict, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }
}