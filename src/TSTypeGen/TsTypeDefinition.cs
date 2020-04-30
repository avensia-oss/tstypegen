using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TSTypeGen
{
    public abstract class TsTypeDefinition
    {
        public string Name { get; }
        public abstract string GetSource(string outputFilePath, GetSourceConfig config, bool isNamespaceFile, Dictionary<ImportedType, string> importMappings);

        private TsTypeDefinition(string name)
        {
            Name = name;
        }

        public static TsTypeDefinition Interface(string name, IEnumerable<TsInterfaceMember> members, IEnumerable<TsTypeReference> extends, IEnumerable<string> typeParameters, IEnumerable<TsTypeReference> mustBeAssignableFrom, DerivedTypesUnionGeneration derivedTypesUnionGeneration, string typeMemberName)
        {
            return new InterfaceType(name, members, extends, typeParameters, mustBeAssignableFrom, derivedTypesUnionGeneration, typeMemberName);
        }

        public static TsTypeDefinition Enum(string name, IEnumerable<string> members, bool useConstEnum)
        {
            return new EnumType(name, members, useConstEnum);
        }

        private class InterfaceType : TsTypeDefinition
        {
            private readonly ImmutableArray<TsInterfaceMember> _members;
            private readonly ImmutableArray<string> _typeParameters;
            private readonly ImmutableArray<TsTypeReference> _extends;
            private readonly ImmutableArray<TsTypeReference> _mustBeAssignableFrom;
            private readonly DerivedTypesUnionGeneration _derivedTypesUnionGeneration;
            private readonly string _typeMemberName;

            public InterfaceType(
                string name,
                IEnumerable<TsInterfaceMember> members,
                IEnumerable<TsTypeReference> extends,
                IEnumerable<string> typeParameters,
                IEnumerable<TsTypeReference> mustBeAssignableFrom,
                DerivedTypesUnionGeneration derivedTypesUnionGeneration,
                string typeMemberName
            ) : base(name)
            {
                _members = ImmutableArray.CreateRange(members);
                _extends = ImmutableArray.CreateRange(extends);
                _typeParameters = ImmutableArray.CreateRange(typeParameters);
                _mustBeAssignableFrom = ImmutableArray.CreateRange(mustBeAssignableFrom);
                _derivedTypesUnionGeneration = derivedTypesUnionGeneration;
                _typeMemberName = typeMemberName;
            }

            private void EnsureImports(Dictionary<ImportedType, string> importMappings)
            {
                var rawImports = _members.SelectMany(m => m.Type.GetImportStatements());
                if (_extends != null)
                    rawImports = rawImports.Concat(_extends.SelectMany(e => e.GetImportStatements()));
                rawImports = rawImports.Concat(_mustBeAssignableFrom.SelectMany(t => t.GetImportStatements()));
                if (_derivedTypesUnionGeneration != null)
                {
                    rawImports = rawImports.Concat(_derivedTypesUnionGeneration.DerivedTypeReferences.SelectMany(t => t.GetImportStatements()));
                }

                foreach (var rawImport in rawImports.GroupBy(x => x.SourceFile).Select(g => g.First()).OrderBy(x => x.NamedImportName).ThenBy(x => x.DefaultVariableName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.SourceFile, StringComparer.OrdinalIgnoreCase))
                {
                    var key = new ImportedType(rawImport.SourceFile, rawImport.NamedImportName);
                    if (importMappings.ContainsKey(key))
                    {
                        continue;
                    }

                    if (!importMappings.ContainsValue(rawImport.DefaultVariableName))
                    {
                        importMappings.Add(key, rawImport.DefaultVariableName);
                    }
                    else
                    {
                        string name;
                        for (int i = 1;; i++)
                        {
                            name = rawImport.DefaultVariableName + i.ToString(CultureInfo.InvariantCulture);
                            if (!importMappings.ContainsValue(name))
                            {
                                break;
                            }
                        }

                        importMappings.Add(key, name);
                    }
                }
            }

            public override string GetSource(string outputFilePath, GetSourceConfig config, bool isNamespaceFile, Dictionary<ImportedType, string> importMappings)
            {
                var indent = isNamespaceFile ? "  " : "";

                var result = new StringBuilder();

                EnsureImports(importMappings);

                result.Append($"{indent}interface {Name}");
                if (_typeParameters.Length > 0)
                {
                    result.Append("<")
                          .Append(string.Join(", ", _typeParameters))
                          .Append(">");
                }

                if (_extends.Length > 0)
                {
                    result.Append(" extends ").Append(_extends[0].GetSource(isNamespaceFile, config.UseOptionalForNullables, importMappings));
                    for (int i = 1; i < _extends.Length; i++)
                    {
                        result.Append(", ").Append(_extends[i].GetSource(isNamespaceFile, config.UseOptionalForNullables, importMappings));
                    }
                }

                result.AppendLine(" {");

                if (_typeMemberName != null)
                {
                    // TODO: Should the value here be configurable? Perhaps you want the FQN?
                    result.AppendLine($"{indent}  {_typeMemberName}: '{Name}';");
                }

                foreach (var m in _members)
                {
                    var optional = "";
                    if (m.IsOptional || m.Type.IsOptional)
                    {
                        if (m.IsOptional)
                            optional = "?";
                        else if (m.Type.IsOptional)
                            optional = config.UseOptionalForNullables ? "?" : "";
                    }
                    result.AppendLine($"{indent}  {FixName(m.Name)}{optional}: {m.Type.GetSource(isNamespaceFile, config.UseOptionalForNullables, importMappings)};");
                }

                result.Append(indent).AppendLine("}");

                if (_derivedTypesUnionGeneration?.DerivedTypeReferences.Length > 0)
                {
                    result.AppendLine();
                    result.AppendLine($"{indent}type {_derivedTypesUnionGeneration.DerivedTypesUnionName} = {string.Join(" | ", _derivedTypesUnionGeneration.DerivedTypeReferences.Select(t => t.GetSource(isNamespaceFile, config.UseOptionalForNullables, importMappings)))};");
                }

                if (!isNamespaceFile)
                {
                    if (_derivedTypesUnionGeneration?.DerivedTypeReferences.Length > 0)
                    {
                        result.AppendLine().AppendLine($"export {_derivedTypesUnionGeneration.DerivedTypesUnionName};");
                    }

                    result.AppendLine().AppendLine($"export default {Name};");

                    foreach (var type in _mustBeAssignableFrom)
                    {
                        result.AppendLine()
                              .AppendLine($"// This type must be a structural subset of {type.GetSource(isNamespaceFile, config.UseOptionalForNullables, importMappings)}. A compilation error on the next line means that this is not the case.")
                              .AppendLine("// Note, however, that the error message from TypeScript might be bad or misleading.")
                              .AppendLine($"(function() {{ const v: {Name} = {{}} as {type.GetSource(isNamespaceFile, config.UseOptionalForNullables, importMappings)}; return v; }});");
                    }
                }

                return result.ToString();
            }

            private static readonly Regex ValidIdentifierRegex = new Regex("^[a-zA-Z_][a-zA-Z_0-9]*$");
            private string FixName(string name)
            {
                return ValidIdentifierRegex.IsMatch(name) ? name : $"'{name}'";
            }
        }

        private class EnumType : TsTypeDefinition
        {
            private readonly bool _useConstEnum;
            private readonly ImmutableArray<string> _members;

            public EnumType(string name, IEnumerable<string> members, bool useConstEnum) : base(name)
            {
                _useConstEnum = useConstEnum;
                _members = ImmutableArray.CreateRange(members);
            }

            public override string GetSource(string outputFilePath, GetSourceConfig config, bool isNamespaceFile, Dictionary<ImportedType, string> importMappings)
            {
                var sb = new StringBuilder();
                var indent = isNamespaceFile ? "  " : "";

                if (_useConstEnum || config.UseConstEnums)
                {
                    sb.AppendLine($"{indent}const enum {Name} {{");
                    foreach (var member in _members)
                    {
                        sb.AppendLine($"{indent}  {member} = '{StringUtils.ToCamelCase(member)}',");
                    }
                    sb.AppendLine($"{indent}}}");
                }
                else
                {
                    sb.Append($"{indent}type ").Append(Name).Append(" = ");
                    if (_members.Length > 0)
                    {
                        sb.Append("'").Append(StringUtils.ToCamelCase(_members[0])).Append("'");
                        for (int i = 1; i < _members.Length; i++)
                        {
                            sb.Append(" | '").Append(StringUtils.ToCamelCase(_members[i])).Append("'");
                        }
                    }
                    else
                    {
                        sb.Append("{}");
                    }
                    sb.AppendLine(";");
                }

                if (!isNamespaceFile)
                {
                    sb.AppendLine().Append("export default ").Append(Name).AppendLine(";");
                }

                return sb.ToString();
            }
        }
    }
}