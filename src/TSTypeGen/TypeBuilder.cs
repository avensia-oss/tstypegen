using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace TSTypeGen
{
    public class TypeBuilder
    {
        private static bool IsNullableType(ITypeSymbol type)
        {
            var namedType = type as INamedTypeSymbol;
            return namedType != null && namedType.IsGenericType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;
        }

        private static string FindNameFromJsonPropertyAttribute(IPropertySymbol property)
        {
            string LookupSingle(IPropertySymbol p)
            {
                var jsonPropertyAttribute = p.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == "Newtonsoft.Json.JsonPropertyAttribute");
                if (jsonPropertyAttribute != null)
                {
                    if (jsonPropertyAttribute.NamedArguments.Any(x => x.Key == "PropertyName"))
                    {
                        return jsonPropertyAttribute.NamedArguments.First(x => x.Key == "PropertyName").Value.Value as string;
                    }
                    else if (jsonPropertyAttribute.ConstructorArguments.Length > 0)
                    {
                        return jsonPropertyAttribute.ConstructorArguments[0].Value as string;
                    }
                }
                return null;
            }

            if (LookupSingle(property) is string result)
            {
                return result;
            }

            foreach (var interfaceProperty in property.ContainingType.AllInterfaces.SelectMany(i => i.GetMembers(property.Name)).OfType<IPropertySymbol>())
            {
                if (Equals(property.ContainingType.FindImplementationForInterfaceMember(interfaceProperty), property))
                {
                    if (LookupSingle(interfaceProperty) is string interfaceResult)
                    {
                        return interfaceResult;
                    }
                }
            }

            return null;
        }

        private static TsInterfaceMember BuildMember(IPropertySymbol property, ImmutableArray<INamedTypeSymbol> interfaces, TypeBuilderConfig config, string currentTsNamespace)
        {
            var interfaceProperties = interfaces.SelectMany(i => i.GetMembers(property.Name).OfType<IPropertySymbol>());

            var allPropertiesToCheckForIgnore = new List<IPropertySymbol>();
            allPropertiesToCheckForIgnore.AddRange(interfaceProperties);
            allPropertiesToCheckForIgnore.Add(property);

            foreach (var propertyToCheckForIgnore in allPropertiesToCheckForIgnore)
            {
                if (propertyToCheckForIgnore.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Newtonsoft.Json.JsonIgnoreAttribute"))
                    return null;

                if (propertyToCheckForIgnore.GetAttributes().Any(a => a.AttributeClass?.Name == Program.TypeScriptIgnoreAttributeName))
                    return null;
            }

            string name = FindNameFromJsonPropertyAttribute(property);

            if (string.IsNullOrEmpty(name))
            {
                name = StringUtils.ToCamelCase(property.Name);
            }

            var isOptional = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == Program.TypeScriptOptionalAttributeName) != null;

            return new TsInterfaceMember(name, BuildTsTypeReferenceToPropertyType(property, config, currentTsNamespace), isOptional);
        }

        private static TsTypeReference BuildTsTypeReferenceToPropertyType(IPropertySymbol property, TypeBuilderConfig config, string currentTsNamespace)
        {
            ITypeSymbol type = property.Type;
            var typeScriptTypeAttribute = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == Program.TypeScriptTypeAttributeName && a.ConstructorArguments.Length == 1);
            if (typeScriptTypeAttribute != null)
            {
                if (typeScriptTypeAttribute.ConstructorArguments[0].Value is string typeString)
                {
                    return TsTypeReference.Simple(typeString);
                }
                else if (typeScriptTypeAttribute.ConstructorArguments[0].Value is ITypeSymbol replaceWithType)
                {
                    type = replaceWithType;
                }
            }

            return BuildTsTypeReference(type, config, currentTsNamespace, false);
        }

        private static TsTypeReference BuildTsTypeReference(ITypeSymbol type, TypeBuilderConfig config, string currentTsNamespace, bool returnTheMainTypeEvenIfItHasADerivedTypesUnion)
        {
            var isOptional = false;
            if (IsNullableType(type))
            {
                type = ((INamedTypeSymbol)type).TypeArguments[0];
                isOptional = true;
            }

            if (type.TypeKind == TypeKind.TypeParameter)
            {
                return TsTypeReference.Simple(type.Name, isOptional);
            }

            var typeScriptTypeAttribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == Program.TypeScriptTypeAttributeName && a.ConstructorArguments.Length == 1);
            if (typeScriptTypeAttribute != null)
            {
                if (typeScriptTypeAttribute.ConstructorArguments[0].Value is string typeString)
                {
                    return TsTypeReference.Simple(typeString);
                }
                else if (typeScriptTypeAttribute.ConstructorArguments[0].Value is ITypeSymbol replaceWithType)
                {
                    type = replaceWithType;
                }
            }

            var typeName = type.ToDisplayString();
            if (type is INamedTypeSymbol nt)
            {
                if (!config.TypeMappings.TryGetValue(typeName, out var result))
                {
                    var namespaceName = Processor.GetTypescriptNamespace(type);
                    var derivedTypesUnionName = returnTheMainTypeEvenIfItHasADerivedTypesUnion ? null : GetDerivedTypesUnionName(nt);

                    if (namespaceName != null)
                    {
                        if (namespaceName == currentTsNamespace)
                        {
                            result = TsTypeReference.Simple(derivedTypesUnionName ?? type.Name);
                        }
                        else
                        {
                            result = TsTypeReference.Simple(namespaceName + "." + (derivedTypesUnionName ?? type.Name));
                        }
                    }
                    else
                    {
                        if (type.DeclaringSyntaxReferences.Length > 0)
                        {
                            string path = Processor.GetTypeFilePath(nt);
                            result = derivedTypesUnionName != null ? TsTypeReference.NameImportedType(derivedTypesUnionName, path, isOptional) : TsTypeReference.DefaultImportedType(type.Name, path, isOptional);
                        }
                    }
                }

                if (result != null)
                {
                    if (nt.Arity > 0)
                    {
                        var typeArgs = nt.TypeArguments.Select(t => BuildTsTypeReference(t, config, currentTsNamespace, false));
                        result = TsTypeReference.Generic(result, typeArgs);
                    }
                    return result;
                }
            }

            if (type.SpecialType >= SpecialType.System_SByte && type.SpecialType <= SpecialType.System_Double)
            {
                return TsTypeReference.Simple("number", isOptional);
            }

            if (type.SpecialType == SpecialType.System_String)
            {
                return TsTypeReference.Simple("string", isOptional);
            }

            if (type.SpecialType == SpecialType.System_Boolean)
            {
                return TsTypeReference.Simple("boolean", isOptional);
            }

            if (type.ContainingNamespace?.ToDisplayString() == "Newtonsoft.Json.Linq")
            {
                switch (type.Name)
                {
                    case "JToken":
                    case "JValue":
                        return TsTypeReference.Simple("any", isOptional);
                    case "JArray":
                        return TsTypeReference.Array(TsTypeReference.Simple("any", isOptional));
                    case "JObject":
                        return TsTypeReference.Dictionary(TsTypeReference.Simple("string", isOptional), TsTypeReference.Simple("any", isOptional));
                }
            }

            var dictionaryTypes = GetDictionaryUnderlyingTypes(type);
            if (dictionaryTypes != null)
            {
                return TsTypeReference.Dictionary(BuildTsTypeReference(dictionaryTypes.Item1, config, currentTsNamespace, false), BuildTsTypeReference(dictionaryTypes.Item2, config, currentTsNamespace, false));
            }

            var enumerableUnderlyingType = GetEnumerableUnderlyingType(type);
            if (enumerableUnderlyingType != null)
            {
                return TsTypeReference.Array(BuildTsTypeReference(enumerableUnderlyingType, config, currentTsNamespace, false));
            }

            return TsTypeReference.Simple("any");
        }

        private static ITypeSymbol GetEnumerableUnderlyingType(ITypeSymbol type)
        {
            var enumerable = type.AllInterfaces.FirstOrDefault(i => i.IsGenericType && i.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
            if (enumerable != null)
                return enumerable.TypeArguments.FirstOrDefault();

            var nt = type as INamedTypeSymbol;
            if (nt?.ConstructedFrom?.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                return nt.TypeArguments.FirstOrDefault();
            }

            return null;
        }

        private static bool IsDictionaryInterface(INamedTypeSymbol type)
        {
            return type.IsGenericType
                && type.ContainingNamespace.ToDisplayString() == "System.Collections.Generic"
                && (type.Name == "IDictionary" || type.Name == "IReadOnlyDictionary");
        }

        private static Tuple<ITypeSymbol, ITypeSymbol> GetDictionaryUnderlyingTypes(ITypeSymbol type)
        {
            var nt = type as INamedTypeSymbol;
            if (nt != null && IsDictionaryInterface(nt))
            {
                return Tuple.Create(((INamedTypeSymbol)type).TypeArguments[0], ((INamedTypeSymbol)type).TypeArguments[1]);
            }

            var iface = type.AllInterfaces.FirstOrDefault(IsDictionaryInterface);
            if (iface != null)
            {
                return Tuple.Create(iface.TypeArguments[0], iface.TypeArguments[1]);
            }

            return null;
        }

        private static TsInterfaceMember WrapProperty(TsInterfaceMember member, TypeBuilderConfig config)
        {
            return new TsInterfaceMember(member.Name, TsTypeReference.Generic(config.PropertyTypeReference, new[] { member.Type }), member.IsOptional);
        }

        private static IEnumerable<TsTypeReference> GetMustBeAssignableFromList(INamedTypeSymbol type, TypeBuilderConfig config, string currentTsNamespace)
        {
            var structuralSubsetOfInterfaceNamespace = !string.IsNullOrEmpty(config.StructuralSubsetOfInterfaceFullName) ? config.StructuralSubsetOfInterfaceFullName.Substring(0, config.StructuralSubsetOfInterfaceFullName.LastIndexOf('.')) : "";
            var structuralSubsetOfInterfaceName = config.StructuralSubsetOfInterfaceFullName?.Split('.').LastOrDefault();

            return type.AllInterfaces.Where(i => i.Name == structuralSubsetOfInterfaceName && i.ContainingNamespace.ToDisplayString() == structuralSubsetOfInterfaceNamespace && i.TypeArguments.Length == 1)
                                     .Select(i => BuildTsTypeReference(i.TypeArguments[0], config, currentTsNamespace, true));
        }

        private static string GetDerivedTypesUnionName(INamedTypeSymbol type)
        {
            var attr = type.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == Program.GenerateTypeScriptDerivedTypesUnionAttributeName);
            if (attr == null)
                return null;

            return attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string unionName ? unionName : (type.Name + "Types");
        }

        private static async Task<DerivedTypesUnionGeneration> GetDerivedTypesAsync(INamedTypeSymbol type, TypeBuilderConfig config, string currentTsNamespace, Solution solution)
        {
            var derivedTypesUnionName = GetDerivedTypesUnionName(type);
            if (derivedTypesUnionName == null)
                return null;

            var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(type, solution);

            var derivedTypes = derivedClasses
                .Where(t => !t.IsAbstract)
                .OrderBy(t => t.Name);

            return new DerivedTypesUnionGeneration(ImmutableArray.CreateRange(derivedTypes.Select(i => BuildTsTypeReference(i, config, currentTsNamespace, true))), derivedTypesUnionName);
        }

        private static string GetTypeMemberName(INamedTypeSymbol type)
        {
            // No reason to create a type member on an abstract type since the purpose of the type member
            // is to identify a concrete type
            if (type.IsAbstract)
                return null;

            var typeMemberName = (string)null;
            var currentType = type;
            while (currentType != null)
            {
                var attr = currentType.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == Program.GenerateTypeScriptTypeMemberAttributeName);
                if (attr != null)
                {
                    if (attr.ConstructorArguments[0].Value is string name)
                    {
                        typeMemberName = name;
                    }
                    else
                    {
                        typeMemberName = Program.DefaultTypeMemberName;
                    }
                    break;
                }

                currentType = currentType.BaseType;
            }

            return typeMemberName;
        }

        public static async Task<TsTypeDefinition> BuildTsTypeDefinitionAsync(INamedTypeSymbol type, TypeBuilderConfig config, Solution solution)
        {
            var tsNamespace = Processor.GetTypescriptNamespace(type);

            if (type.TypeKind == TypeKind.Enum)
            {
                var useConstEnum = type.GetAttributes().Any(a =>
                    a.AttributeClass.Name == Program.GenerateTypeScriptTypeConstEnumAttributeName);

                var members = type.GetMembers().OfType<IFieldSymbol>().Where(f => f.IsConst).Select(f => f.Name);
                return TsTypeDefinition.Enum(type.Name, members, useConstEnum);
            }
            else
            {
                var properties = type.GetMembers()
                                 .OfType<IPropertySymbol>()
                                 .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsOverride && p.Parameters.Length == 0 && !p.IsStatic);

                IEnumerable<ITypeSymbol> extends;
                if (type.TypeKind == TypeKind.Interface)
                {
                    extends = type.Interfaces;
                }
                else if (type.TypeKind == TypeKind.Struct || type.BaseType.SpecialType == SpecialType.System_Object)
                {
                    extends = new INamedTypeSymbol[0];
                }
                else
                {
                    extends = new[] { type.BaseType };
                }

                bool wrapMembers = type.AllInterfaces.Any(i => { var s = i.ToDisplayString(); return config.TypesToWrapPropertiesFor.Contains(s); });
                return TsTypeDefinition.Interface(type.Name,
                                                  properties.Select(p => BuildMember(p, type.Interfaces, config, tsNamespace)).Where(x => x != null).Select(p => wrapMembers ? WrapProperty(p, config) : p),
                                                  extends.Select(e => BuildTsTypeReference(e, config, tsNamespace, true)),
                                                  type.TypeParameters.Select(tp => tp.Name),
                                                  GetMustBeAssignableFromList(type, config, tsNamespace),
                                                  await GetDerivedTypesAsync(type, config, tsNamespace, solution),
                                                  GetTypeMemberName(type));
            }
        }
    }
}
