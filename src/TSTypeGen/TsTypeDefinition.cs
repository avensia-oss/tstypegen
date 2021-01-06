using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

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

        public static TsTypeDefinition Interface(INamedTypeSymbol type, IEnumerable<TsInterfaceMember> members, IEnumerable<TsTypeReference> extends, IEnumerable<string> typeParameters, IEnumerable<TsTypeReference> mustBeAssignableFrom, DerivedTypesUnionGeneration derivedTypesUnionGeneration, string typeMemberName)
        {
            return new InterfaceType(type, members, extends, typeParameters, mustBeAssignableFrom, derivedTypesUnionGeneration, typeMemberName);
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
            private readonly INamedTypeSymbol _type;
            private readonly DerivedTypesUnionGeneration _derivedTypesUnionGeneration;
            private readonly string _typeMemberName;

            public InterfaceType(
                INamedTypeSymbol type,
                IEnumerable<TsInterfaceMember> members,
                IEnumerable<TsTypeReference> extends,
                IEnumerable<string> typeParameters,
                IEnumerable<TsTypeReference> mustBeAssignableFrom,
                DerivedTypesUnionGeneration derivedTypesUnionGeneration,
                string typeMemberName
            ) : base(type.Name)
            {
                _members = ImmutableArray.CreateRange(members);
                _extends = ImmutableArray.CreateRange(extends);
                _typeParameters = ImmutableArray.CreateRange(typeParameters);
                _mustBeAssignableFrom = ImmutableArray.CreateRange(mustBeAssignableFrom);
                _type = type;
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

                foreach (var rawImport in rawImports.GroupBy(x => x.SourceFile).Select(g => g.First())
                    .OrderBy(x => x.NamedImportName, StringComparer.InvariantCulture)
                    .ThenBy(x => x.DefaultVariableName, StringComparer.InvariantCultureIgnoreCase)
                    .ThenBy(x => x.SourceFile, StringComparer.InvariantCultureIgnoreCase))
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

                if (Processor.ShouldGenerateDotNetTypeNamesAsJsDocComment(_type))
                {
                    var dotNetTypeAttr = _type.GetAttributes().FirstOrDefault(a =>
                        a.AttributeClass?.Name == Program.GenerateTypeScriptDotNetNameAttributeName &&
                        a.ConstructorArguments.Length == 1
                    );

                    var type = _type;
                    if (dotNetTypeAttr?.ConstructorArguments[0].Value is INamedTypeSymbol nt)
                        type = nt;

                    result.Append($"{indent}/** @DotNetTypeName {GetFullNamespaceName(type)}.{type.Name},{type.ContainingAssembly.Name} */");
                    result.Append(config.NewLine);
                }

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

                result.Append(" {");
                result.Append(config.NewLine);

                if (_typeMemberName != null)
                {
                    // TODO: Should the value here be configurable? Perhaps you want the FQN?
                    result.Append($"{indent}  {_typeMemberName}: '{Name}';");
                    result.Append(config.NewLine);
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
                    result.Append($"{indent}  {FixName(m.Name)}{optional}: {m.Type.GetSource(isNamespaceFile, config.UseOptionalForNullables, importMappings)};");
                    result.Append(config.NewLine);
                }

                result.Append(indent).Append("}");
                result.Append(config.NewLine);

                if (_derivedTypesUnionGeneration?.DerivedTypeReferences.Length > 0)
                {
                    result.Append(config.NewLine);
                    result.Append($"{indent}type {_derivedTypesUnionGeneration.DerivedTypesUnionName} = {string.Join(" | ", _derivedTypesUnionGeneration.DerivedTypeReferences.Select(t => t.GetSource(isNamespaceFile, config.UseOptionalForNullables, importMappings)))};");
                    result.Append(config.NewLine);
                }

                if (!isNamespaceFile)
                {
                    if (_derivedTypesUnionGeneration?.DerivedTypeReferences.Length > 0)
                    {
                        result.Append(config.NewLine);
                        result.Append($"export {_derivedTypesUnionGeneration.DerivedTypesUnionName};");
                        result.Append(config.NewLine);
                    }

                    result.Append(config.NewLine);
                    result.Append($"export default {Name};");
                    result.Append(config.NewLine);

                    foreach (var type in _mustBeAssignableFrom)
                    {
                        result.Append(config.NewLine)
                              .Append($"// This type must be a structural subset of {type.GetSource(isNamespaceFile, config.UseOptionalForNullables, importMappings)}. A compilation error on the next line means that this is not the case.")
                              .Append(config.NewLine)
                              .Append("// Note, however, that the error message from TypeScript might be bad or misleading.")
                              .Append(config.NewLine)
                              .Append($"(function() {{ const v: {Name} = {{}} as {type.GetSource(isNamespaceFile, config.UseOptionalForNullables, importMappings)}; return v; }});")
                              .Append(config.NewLine);
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

        private string GetFullNamespaceName(INamespaceOrTypeSymbol type)
        {
            var namespaces = new List<string>();
            while (!string.IsNullOrEmpty(type.ContainingNamespace?.Name))
            {
                namespaces.Add(type.ContainingNamespace.Name);
                type = type.ContainingNamespace;
            }

            namespaces.Reverse();

            return string.Join(".", namespaces);
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
                    sb.Append($"{indent}const enum {Name} {{");
                    sb.Append(config.NewLine);
                    foreach (var member in _members)
                    {
                        sb.Append($"{indent}  {member} = '{StringUtils.ToCamelCase(member)}',");
                        sb.Append(config.NewLine);
                    }
                    sb.Append($"{indent}}}");
                    sb.Append(config.NewLine);
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
                    sb.Append(";");
                    sb.Append(config.NewLine);
                }

                if (!isNamespaceFile)
                {
                    sb.Append(config.NewLine).Append("export default ").Append(Name).Append(";").Append(config.NewLine);
                }

                return sb.ToString();
            }
        }
    }
}