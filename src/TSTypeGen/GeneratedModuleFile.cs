using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace TSTypeGen
{
    public class GeneratedModuleFile : GeneratedFile
    {
        private readonly INamedTypeSymbol _type;

        public GeneratedModuleFile(string filePath, INamedTypeSymbol type) : base(filePath)
        {
            _type = type;
        }

        protected override async Task<string> GetContentAsync(TypeBuilderConfig typeBuilderConfig, GetSourceConfig getSourceConfig, Solution solution)
        {
            var tsTypeDefinition = await TypeBuilder.BuildTsTypeDefinitionAsync(_type, typeBuilderConfig, solution);
            var importMappings = new Dictionary<ImportedType, string> { [new ImportedType(FilePath, null)] = tsTypeDefinition.Name };
            var source = tsTypeDefinition.GetSource(FilePath, getSourceConfig, false, importMappings);

            bool hasImport = false;
            var imports = new StringBuilder();
            foreach (var i in importMappings)
            {
                if (i.Key.FilePath != FilePath)
                {
                    hasImport = true;
                    string path = GetImportPath(i.Key.FilePath, getSourceConfig);

                    if (i.Key.ImportName != null)
                    {
                        if (i.Key.ImportName == i.Value)
                        {
                            imports.Append($"import {{ {i.Value} }} from '{path}';");
                            imports.Append(getSourceConfig.NewLine);
                        }
                        else
                        {
                            imports.Append($"import {{ {i.Key.ImportName} as {i.Value} }} from '{path}';");
                            imports.Append(getSourceConfig.NewLine);
                        }
                    }
                    else
                    {
                        imports.Append($"import {i.Value} from '{path}';");
                        imports.Append(getSourceConfig.NewLine);
                    }
                }
            }

            return hasImport ? imports + getSourceConfig.NewLine + source : source;
        }
    }
}