using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace TSTypeGen
{
    public abstract class GeneratedFile
    {
        public string FilePath { get; }

        protected GeneratedFile(string filePath)
        {
            FilePath = filePath;
        }

        protected abstract Task<string> GetContentAsync(TypeBuilderConfig typeBuilderConfig, GetSourceConfig getSourceConfig, Solution solution);

        private static string RemoveTsExtension(string s)
        {
            return s.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ? s.Substring(0, s.Length - 3) : s;
        }

        private string GetRelativePath(string path, GetSourceConfig config)
        {
            var fromUri = new Uri(FilePath);
            var toUri = new Uri(path);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            var parts = relativePath.Split('/');
            if (parts.Any(p => p == "..") && parts.Count(p => p != "..") >= 2)
            {
                // This means that we need to navigate both up and down the directory structure, so prefer another kind of path, if possible
                foreach (var alias in config.PathAliases)
                {
                    if (path.StartsWith(alias.Value, StringComparison.OrdinalIgnoreCase))
                        return alias.Key + "/" + path.Substring(alias.Value.Length).Replace("\\" ,"/");
                }

                if (!string.IsNullOrEmpty(config.RootPath) && path.StartsWith(config.RootPath, StringComparison.OrdinalIgnoreCase))
                    return path.Substring(config.RootPath.Length).Replace("\\" ,"/");
            }

            if (!relativePath.StartsWith("."))
                relativePath = "./" + relativePath;

            return relativePath;
        }

        protected string GetImportPath(string path, GetSourceConfig config)
        {
            if (Path.IsPathRooted(path))
            {
                path = GetRelativePath(path, config);
            }
            return RemoveTsExtension(path);
        }

        public async Task ApplyAsync(TypeBuilderConfig typeBuilderConfig, GetSourceConfig getSourceConfig, Solution solution)
        {
            bool exists = File.Exists(FilePath);
            var origContent = exists ? FileUtils.ReadAllTextSafe(FilePath) : "";
            var newContent = await GetContentAsync(typeBuilderConfig, getSourceConfig, solution);

            if (newContent != origContent)
            {
                File.WriteAllText(FilePath, newContent);
                Console.WriteLine($"{(exists ? "Updated" : "Created")} file {FilePath}.");
            }
        }

        public async Task<bool> VerifyAsync(TypeBuilderConfig typeBuilderConfig, GetSourceConfig getSourceConfig, Solution solution)
        {
            if (!File.Exists(FilePath))
            {
                Program.WriteError($"File {FilePath} does not exist.");
                return false;
            }

            try
            {
                var origContent = File.ReadAllText(FilePath);
                var newContent = await GetContentAsync(typeBuilderConfig, getSourceConfig, solution);
                if (newContent.Replace("\r\n", "\n") != origContent.Replace("\r\n" ,"\n"))
                {
                    Program.WriteError($"Generated file {FilePath} does not match the source definition. Run the frontend build and commit all changes to generated types.");
                    return false;
                }
            }
            catch (Exception ex) {
                Program.WriteError($"Error verifying generated file {FilePath}: {ex.Message}");
                return false;
            }

            return true;
        }
    }
}