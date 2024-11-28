using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}