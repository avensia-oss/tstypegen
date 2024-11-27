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
        public abstract string GetSource(string outputFilePath, Config config, GeneratorContext generatorContext);

        private TsTypeDefinition(string name)
        {
            Name = name;
        }

        public static TsTypeDefinition Interface(
            Type type,
            IEnumerable<TsInterfaceMember> members,
            IEnumerable<TsTypeReference> extends,
            IEnumerable<string> typeParameters,
            DerivedTypesUnionGeneration derivedTypesUnionGeneration,
            TsTypeDefinition parentToAugument,
            string typeMemberName
        )
        {
            return new InterfaceType(type, members, extends, typeParameters, derivedTypesUnionGeneration, parentToAugument, typeMemberName);
        }

        public static TsTypeDefinition Enum(string name, IEnumerable<string> members)
        {
            return new EnumType(name, members);
        }

        public static TsTypeDefinition Const(string name, IEnumerable<string> members)
        {
            return new ConstType(name, members);
        }

        private class InterfaceType : TsTypeDefinition
        {
            private readonly ImmutableArray<TsInterfaceMember> _members;
            private readonly ImmutableArray<string> _typeParameters;
            private readonly ImmutableArray<TsTypeReference> _extends;
            private readonly Type _type;
            private readonly DerivedTypesUnionGeneration _derivedTypesUnionGeneration;
            private readonly TsTypeDefinition _parentToAugument;
            private readonly string _typeMemberName;

            public InterfaceType(
                Type type,
                IEnumerable<TsInterfaceMember> members,
                IEnumerable<TsTypeReference> extends,
                IEnumerable<string> typeParameters,
                DerivedTypesUnionGeneration derivedTypesUnionGeneration,
                TsTypeDefinition parentToAugument,
                string typeMemberName
            ) : base(TypeUtils.GetNameWithoutGenericArity(type))
            {
                _members = ImmutableArray.CreateRange(members);
                _extends = ImmutableArray.CreateRange(extends);
                _typeParameters = ImmutableArray.CreateRange(typeParameters);
                _type = type;
                _derivedTypesUnionGeneration = derivedTypesUnionGeneration;
                _parentToAugument = parentToAugument;
                _typeMemberName = typeMemberName;
            }

            public override string GetSource(string outputFilePath, Config config, GeneratorContext generatorContext)
            {
                var indent = "  ";

                var result = new StringBuilder();

                var name = Name;
                if (_parentToAugument != null)
                    name = _parentToAugument.Name;

                if (config.GenerateComments)
                {
                    var typeScriptClassComment = generatorContext.GetTypeScriptComment(_type);
                    if (typeScriptClassComment == null)
                    {
                        var interfaces = _type.GetInterfaces();
                        foreach (var iface in interfaces)
                        {
                            typeScriptClassComment = generatorContext.GetTypeScriptComment(iface);
                            if (typeScriptClassComment != null)
                                break;
                        }
                    }

                    var deprecatedAttr = TypeUtils.GetCustomAttributesData(_type).FirstOrDefault(a => a.AttributeType.Name == Constants.ObsoleteAttributeName);
                    var isDeprecated = deprecatedAttr != null;
                    var deprecatedComment = deprecatedAttr?.ConstructorArguments?.FirstOrDefault().Value as string;

                    var dotNetType = default(Type);
                    var canonicalType = default(Type);

                    if (TypeBuilder.ShouldGenerateDotNetTypeNamesAsJsDocComment(_type))
                    {
                        var dotNetTypeAttr = TypeUtils.GetCustomAttributesData(_type).FirstOrDefault(a =>
                            a.AttributeType.Name == Constants.GenerateTypeScriptDotNetNameAttributeName &&
                            a.ConstructorArguments.Count == 1
                        );

                        dotNetType = _type;
                        if (dotNetTypeAttr?.ConstructorArguments[0].Value is Type t)
                            dotNetType = t;

                        canonicalType = TypeBuilder.GetCanonicalDotNetType(dotNetType);
                    }

                    var interfaceComments = GetInterfaceJsDocComment(indent, config, typeScriptClassComment, dotNetType, canonicalType, isDeprecated, deprecatedComment);
                    if (interfaceComments != null)
                        result.Append(interfaceComments);
                }

                result.Append($"{indent}interface {name}");
                if (_typeParameters.Length > 0)
                {
                    result.Append("<")
                          .Append(string.Join(", ", _typeParameters))
                          .Append(">");
                }

                if (_extends.Length > 0)
                {
                    var extends = _parentToAugument != null ? _extends.Where(e => !e.Equals(_parentToAugument)).ToList() : _extends.ToList();

                    if (extends.Any())
                    {
                        result.Append(" extends ").Append(extends[0].GetSource());
                        for (int i = 1; i < extends.Count; i++)
                        {
                            result.Append(", ").Append(extends[i].GetSource());
                        }
                    }
                }

                result.Append(" {");
                result.Append(config.NewLine);

                if (_typeMemberName != null)
                {
                    // TODO: Should the value here be configurable? Perhaps you want the FQN?
                    result.Append($"{indent}  {_typeMemberName}: '{name}';");
                    result.Append(config.NewLine);
                }

                var memberTypeWrapper = TypeBuilder.GetWrapperTypeForMembers(_type, config);
                foreach (var m in _members)
                {
                    var optional = "";
                    if (m.IsOptional || m.Type.IsOptional)
                    {
                        if (m.IsOptional || m.Type.IsOptional)
                            optional = "?";
                    }

                    if (config.GenerateComments)
                    {
                        var typeScriptMemberComment = generatorContext.GetTypeScriptComment(m.MemberInfo);
                        if (typeScriptMemberComment == null)
                        {
                            var interfaceMembers = m.MemberInfo.DeclaringType.GetInterfaces().SelectMany(i => i.GetMember(m.MemberInfo.Name)).Where(m => m != null).ToList();
                            foreach (var interfaceMember in interfaceMembers)
                            {
                                typeScriptMemberComment = generatorContext.GetTypeScriptComment(interfaceMember);
                                if (typeScriptMemberComment != null)
                                    break;
                            }
                        }

                        var memberDeprecatedAttr = TypeUtils.GetCustomAttributesData(m.MemberInfo).FirstOrDefault(a => a.AttributeType.Name == Constants.ObsoleteAttributeName);
                        var memberIsDeprecated = memberDeprecatedAttr != null;
                        var memberDeprecatedComment = memberDeprecatedAttr?.ConstructorArguments?.FirstOrDefault().Value as string;

                        var memberComments = GetMemberJsDocComment(indent, config, typeScriptMemberComment, memberIsDeprecated, memberDeprecatedComment);
                        if (memberComments != null)
                            result.Append(memberComments);
                    }

                    result.Append($"{indent}  {FixName(m.Name)}{optional}: {WrapType(m.Type.GetSource(), memberTypeWrapper)};");
                    result.Append(config.NewLine);
                }

                result.Append(indent).Append("}");
                result.Append(config.NewLine);

                if (_derivedTypesUnionGeneration?.DerivedTypeReferences.Length > 0)
                {
                    result.Append(config.NewLine);
                    result.Append($"{indent}type {_derivedTypesUnionGeneration.DerivedTypesUnionName} ={config.NewLine}{indent}  | {string.Join($"{config.NewLine}{indent}  | ", _derivedTypesUnionGeneration.DerivedTypeReferences.Select(t => t.GetSource()))};");
                    result.Append(config.NewLine);
                }

                return result.ToString();
            }

            private string WrapType(string name, string memberTypeWrapper)
            {
                return string.IsNullOrEmpty(memberTypeWrapper) ? name : $"{memberTypeWrapper}<{name}>";
            }

            private static readonly Regex ValidIdentifierRegex = new Regex("^[a-zA-Z_][a-zA-Z_0-9]*$");
            private string FixName(string name)
            {
                return ValidIdentifierRegex.IsMatch(name) ? name : $"'{name}'";
            }
        }

        private string GetInterfaceJsDocComment(string indent, Config config, List<string> textComment, Type dotNetType, Type canonicalType, bool isDeprecated, string deprecatedComment)
        {
            if (textComment == null && dotNetType == null && canonicalType == null && !isDeprecated)
                return null;

            var result = new StringBuilder();

            result.Append($"{indent}/**");
            var commentSections = 0;

            if (dotNetType != null)
                commentSections++;
            if (canonicalType != null)
                commentSections++;
            if (isDeprecated)
                commentSections++;

            var shouldRenderCommentOnSingleLine = textComment == null && commentSections == 1;
            var lineIndent = shouldRenderCommentOnSingleLine ? " " : indent + " * ";

            if (!shouldRenderCommentOnSingleLine)
                result.Append(config.NewLine);

            if (textComment != null)
            {
                result.Append(FormatTypeScriptComment(textComment, indent, config.NewLine));
                result.Append(config.NewLine);
                if (commentSections > 0)
                {
                    result.Append($"{indent} *");
                    result.Append(config.NewLine);
                }
            }

            if (dotNetType != null)
            {
                result.Append($"{lineIndent}@DotNetTypeName {TypeUtils.GetFullName(dotNetType)},{dotNetType.Assembly.GetName().Name}");
                if (!shouldRenderCommentOnSingleLine)
                    result.Append(config.NewLine);
            }

            if (canonicalType != null)
            {
                result.Append($"{lineIndent}@DotNetCanonicalTypeName {TypeUtils.GetFullName(canonicalType)},{canonicalType.Assembly.GetName().Name}");
                if (!shouldRenderCommentOnSingleLine)
                    result.Append(config.NewLine);
            }

            if (isDeprecated)
            {
                result.Append($"{lineIndent}@deprecated{(string.IsNullOrEmpty(deprecatedComment) ? "" : " " + deprecatedComment)}");
                if (!shouldRenderCommentOnSingleLine)
                    result.Append(config.NewLine);
            }

            if (!shouldRenderCommentOnSingleLine)
                result.Append($"{indent} */");
            else
                result.Append($" */");

            result.Append(config.NewLine);

            return result.ToString();
        }

        private string GetMemberJsDocComment(string indent, Config config, List<string> textComment, bool isDeprecated, string deprecatedComment)
        {
            if (textComment == null && !isDeprecated)
                return null;

            var result = new StringBuilder();

            result.Append($"{indent}  /**");
            var commentSections = 0;

            if (isDeprecated)
                commentSections++;

            var shouldRenderCommentOnSingleLine = textComment == null && commentSections == 1;
            var lineIndent = shouldRenderCommentOnSingleLine ? " " : indent + "   * ";

            if (!shouldRenderCommentOnSingleLine)
                result.Append(config.NewLine);

            if (textComment != null)
            {
                result.Append(FormatTypeScriptComment(textComment, indent + "  ", config.NewLine));
                result.Append(config.NewLine);
                if (commentSections > 0)
                {
                    result.Append($"{indent}   *");
                    result.Append(config.NewLine);
                }
            }

            if (isDeprecated)
            {
                result.Append($"{lineIndent}@deprecated{(string.IsNullOrEmpty(deprecatedComment) ? "" : " " + deprecatedComment)}");
                if (!shouldRenderCommentOnSingleLine)
                    result.Append(config.NewLine);
            }

            if (!shouldRenderCommentOnSingleLine)
                result.Append($"{indent}   */");
            else
                result.Append($" */");

            result.Append(config.NewLine);

            return result.ToString();
        }

        private string FormatTypeScriptComment(List<string> textComment, string indent, string newLine)
        {
            return string.Join(newLine, textComment.Select(line => $"{indent} * {line}"));
        }

        private class EnumType : TsTypeDefinition
        {
            private readonly ImmutableArray<string> _members;

            public EnumType(string name, IEnumerable<string> members) : base(name)
            {
                _members = ImmutableArray.CreateRange(members);
            }

            public override string GetSource(string outputFilePath, Config config, GeneratorContext generatorContext)
            {
                var sb = new StringBuilder();
                var indent = "  ";

                if (config.UseConstEnums)
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
                    sb.Append(indent).Append("type ").Append(Name).Append(" = ");
                    if (_members.Length > 0)
                    {
                        sb.Append('\'').Append(StringUtils.ToCamelCase(_members[0])).Append('\'');
                        for (int i = 1; i < _members.Length; i++)
                        {
                            sb.Append(" | '").Append(StringUtils.ToCamelCase(_members[i])).Append('\'');
                        }
                    }
                    else
                    {
                        sb.Append("{}");
                    }
                    sb.AppendLine(";");
                }
                return sb.ToString();
            }
        }

        private class ConstType : TsTypeDefinition
        {
            private readonly ImmutableArray<string> _members;

            public ConstType(string name, IEnumerable<string> members) : base(name)
            {
                _members = ImmutableArray.CreateRange(members);
            }

            public override string GetSource(string outputFilePath, Config config, GeneratorContext generatorContext)
            {
                var sb = new StringBuilder();
                var indent = "  ";

                sb.Append($"{indent}{Name}: {{");
                sb.Append(config.NewLine);
                foreach (var member in _members)
                {
                    sb.Append($"{indent}  {member}: \"{StringUtils.ToCamelCase(member)}\",");
                    sb.Append(config.NewLine);
                }
                sb.Append($"{indent}}},");
                sb.Append(config.NewLine);

                return sb.ToString();
            }
        }
    }
}
