using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace TSTypeGen
{
    public class GeneratedNamespaceFile : GeneratedFile
    {
        private readonly string _namespaceName;
        private readonly ImmutableList<INamedTypeSymbol> _types;

        public GeneratedNamespaceFile(string filePath, string namespaceName, ImmutableList<INamedTypeSymbol> types) : base(filePath)
        {
            _namespaceName = namespaceName;
            _types = types;
        }

        protected override async Task<string> GetContentAsync(TypeBuilderConfig typeBuilderConfig, GetSourceConfig getSourceConfig, Solution solution)
        {
            bool first = true;

            var importMappings = new Dictionary<ImportedType, string>();

            var innerSource = new StringBuilder();
            foreach (var t in _types.OrderBy(t => t.Name, StringComparer.InvariantCulture).ThenBy(t => t.ToDisplayString(), StringComparer.InvariantCulture))
            {
                if (!first)
                {
                    innerSource.Append(getSourceConfig.NewLine);
                }
                var tsTypeDefinition = await TypeBuilder.BuildTsTypeDefinitionAsync(t, typeBuilderConfig, solution);
                innerSource.Append(tsTypeDefinition.GetSource(FilePath, getSourceConfig, true, importMappings));
                first = false;
            }

            var importSource = new StringBuilder();
            foreach (var i in importMappings)
            {
                importSource.Append($"    type {i.Value} = import('{GetImportPath(i.Key.FilePath, getSourceConfig)}').{i.Key.ImportName ?? "default"};");
                importSource.Append(getSourceConfig.NewLine);
            }

            if (importSource.Length > 0)
            {
                return "declare namespace " + _namespaceName + " {" + getSourceConfig.NewLine +
                       "  namespace __ImportedModules {" + getSourceConfig.NewLine +
                       importSource +
                       "  }" + getSourceConfig.NewLine + getSourceConfig.NewLine +
                       innerSource.ToString() + "}" + getSourceConfig.NewLine;
            }
            else
            {
                return "declare namespace " + _namespaceName + " {" + getSourceConfig.NewLine + innerSource + "}" + getSourceConfig.NewLine;
            }
        }
    }
}