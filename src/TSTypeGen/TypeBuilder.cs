using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TSTypeGen
{
    public class TypeBuilder
    {
        private static string FindNameFromJsonPropertyAttribute(PropertyInfo property)
        {
            string LookupSingle(PropertyInfo p)
            {
                var jsonPropertyAttribute = TypeUtils.GetCustomAttributesData(p).FirstOrDefault(a => a.AttributeType.FullName == "Newtonsoft.Json.JsonPropertyAttribute");
                if (jsonPropertyAttribute != null)
                {
                    if (jsonPropertyAttribute.NamedArguments?.Any(x => x.MemberName == "PropertyName") == true)
                    {
                        return jsonPropertyAttribute.NamedArguments.First(x => x.MemberName == "PropertyName").TypedValue.Value as string;
                    }
                    else if (jsonPropertyAttribute.ConstructorArguments.Count > 0)
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

            foreach (var interfaceProperty in property.DeclaringType?.GetInterfaces().SelectMany(i => TypeUtils.GetRelevantAndBaseProperties(i).Where(p => p.Name == property.Name)) ?? new List<PropertyInfo>())
            {
                if (LookupSingle(interfaceProperty) is string interfaceResult)
                {
                    return interfaceResult;
                }
            }

            var currentType = property.DeclaringType?.BaseType;
            while (currentType != null)
            {
                var baseProperties = TypeUtils.GetRelevantAndBaseProperties(currentType).Where(p => p.Name == property.Name);
                foreach (var baseProperty in baseProperties)
                {
                    if (LookupSingle(baseProperty) is string baseResult)
                    {
                        return baseResult;
                    }
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        private static TsInterfaceMember BuildMember(PropertyInfo property, IList<Type> interfaces, TypeBuilderConfig config, string currentTsNamespace)
        {
            var interfaceProperties = interfaces.SelectMany(i => TypeUtils.GetRelevantAndBaseProperties(i).Where(p => p.Name == property.Name));

            var allPropertiesToCheckForIgnore = new List<PropertyInfo>();
            allPropertiesToCheckForIgnore.Add(property);
            allPropertiesToCheckForIgnore.AddRange(interfaceProperties);

            var currentType = property.DeclaringType?.BaseType;
            while (currentType != null)
            {
                var baseProperties = TypeUtils.GetRelevantAndBaseProperties(currentType).Where(p => p.Name == property.Name);
                allPropertiesToCheckForIgnore.AddRange(baseProperties);
                currentType = currentType.BaseType;
            }

            foreach (var propertyToCheckForIgnore in allPropertiesToCheckForIgnore)
            {
                var attributes = TypeUtils.GetCustomAttributesData(propertyToCheckForIgnore);
                if (attributes.All(a => a.AttributeType.Name != Constants.TypeScriptTypeAttributeName))
                {
                    if (attributes.Any(a => a.AttributeType.FullName == "Newtonsoft.Json.JsonIgnoreAttribute"))
                        return null;

                    if (attributes.Any(a => a.AttributeType.FullName == config.CustomTypeScriptIgnoreAttributeFullName))
                        return null;

                    if (attributes.Any(a => a.AttributeType.Name == Constants.TypeScriptIgnoreAttributeName))
                        return null;
                }
            }

            string name = FindNameFromJsonPropertyAttribute(property);

            if (string.IsNullOrEmpty(name))
            {
                name = StringUtils.ToCamelCase(property.Name);
            }

            var isOptional = TypeUtils.GetCustomAttributesData(property).FirstOrDefault(a => a.AttributeType.Name == Constants.TypeScriptOptionalAttributeName) != null;

            return new TsInterfaceMember(name, BuildTsTypeReferenceToPropertyType(property, config, currentTsNamespace), isOptional);
        }

        private static TsTypeReference BuildTsTypeReferenceToPropertyType(PropertyInfo property, TypeBuilderConfig config, string currentTsNamespace)
        {
            var type = property.PropertyType;
            var typeScriptTypeAttribute = GetTypeScriptTypeAttribute(property);
            if (typeScriptTypeAttribute != null)
            {
                if (typeScriptTypeAttribute.ConstructorArguments[0].Value is string typeString)
                {
                    return TsTypeReference.Simple(typeString);
                }
                else if (typeScriptTypeAttribute.ConstructorArguments[0].Value is Type replaceWithType)
                {
                    type = replaceWithType;
                }
            }

            return BuildTsTypeReference(type, config, currentTsNamespace, false);
        }

        private static CustomAttributeData GetTypeScriptTypeAttribute(PropertyInfo property)
        {
            return GetTypeScriptTypeAttribute(TypeUtils.GetCustomAttributesData(property));
        }

        private static CustomAttributeData GetTypeScriptTypeAttribute(Type type)
        {
            return GetTypeScriptTypeAttribute(TypeUtils.GetCustomAttributesData(type));
        }

        private static CustomAttributeData GetTypeScriptTypeAttribute(List<CustomAttributeData> attributes)
        {
            var explicitTypeAttribute = attributes.FirstOrDefault(a => a.AttributeType?.Name == Constants.TypeScriptTypeAttributeName && a.ConstructorArguments.Count == 1);
            if (explicitTypeAttribute != null)
                return explicitTypeAttribute;

            foreach (var attribute in attributes)
            {
                var attributeAttributes = TypeUtils.GetCustomAttributesData(attribute.AttributeType);

                var attributeAttribute = attributeAttributes.FirstOrDefault(a => a.AttributeType?.Name == Constants.TypeScriptTypeAttributeName && a.ConstructorArguments.Count == 1);
                if (attributeAttribute != null)
                    return attributeAttribute;
            }

            return null;
        }

        private static TsTypeReference BuildTsTypeReference(Type type, TypeBuilderConfig config, string currentTsNamespace, bool returnTheMainTypeEvenIfItHasADerivedTypesUnion)
        {
            var isOptional = false;

            var underlyingNullableType = GetUnderlyingNullableType(type);
            if (underlyingNullableType != null)
            {
                type = underlyingNullableType;
                isOptional = true;
            }

            if (type.IsGenericParameter)
            {
                return TsTypeReference.Simple(type.Name, isOptional);
            }

            var typeFullName = TypeUtils.GetFullNameWithGenericArguments(type);

            if (typeFullName != null && config.TypeMappings.TryGetValue(typeFullName, out var mappedType))
            {
                return mappedType;
            }

            var typeScriptTypeAttribute = GetTypeScriptTypeAttribute(type);
            if (typeScriptTypeAttribute != null)
            {
                if (typeScriptTypeAttribute.ConstructorArguments[0].Value is string typeString)
                {
                    return TsTypeReference.Simple(typeString);
                }
                else if (typeScriptTypeAttribute.ConstructorArguments[0].Value is Type replaceWithType)
                {
                    type = replaceWithType;
                }
            }

            if (
                TypeUtils.Is<byte>(type) ||
                TypeUtils.Is<sbyte>(type) ||
                TypeUtils.Is<int>(type) ||
                TypeUtils.Is<uint>(type) ||
                TypeUtils.Is<long>(type) ||
                TypeUtils.Is<ulong>(type) ||
                TypeUtils.Is<short>(type) ||
                TypeUtils.Is<ushort>(type) ||
                TypeUtils.Is<double>(type) ||
                TypeUtils.Is<decimal>(type) ||
                TypeUtils.Is<float>(type)
            )
            {
                return TsTypeReference.Simple("number", isOptional);
            }

            if (TypeUtils.Is<string>(type))
            {
                return TsTypeReference.Simple("string", isOptional);
            }

            if (TypeUtils.Is<bool>(type))
            {
                return TsTypeReference.Simple("boolean", isOptional);
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

            if (typeFullName != null && (type.IsClass || type.IsInterface || type.IsValueType))
            {
                var namespaceName = GetTypescriptNamespace(type);
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    var derivedTypesUnionName = returnTheMainTypeEvenIfItHasADerivedTypesUnion ? null : GetDerivedTypesUnionName(type);

                    var typeName = TypeUtils.GetNameWithoutGenericArity(type);

                    var result = default(TsTypeReference);
                    if (namespaceName == currentTsNamespace)
                    {
                        result = TsTypeReference.Simple(derivedTypesUnionName ?? typeName);
                    }
                    else
                    {
                        result = TsTypeReference.Simple(namespaceName + "." + (derivedTypesUnionName ?? typeName));
                    }

                    if (result != null)
                    {
                        if (type.GenericTypeArguments.Length > 0)
                        {
                            var typeArgs = type.GenericTypeArguments.Select(t => BuildTsTypeReference(t, config, currentTsNamespace, false));
                            result = TsTypeReference.Generic(result, typeArgs);
                        }

                        return result;
                    }
                }
            }

            return TsTypeReference.Simple("unknown");
        }

        private static Type GetUnderlyingNullableType(Type type)
        {
            if (type.FullName?.StartsWith("System.Nullable`") == true)
            {
                return type.GenericTypeArguments.First();
            }

            return null;
        }

        private static Type GetEnumerableUnderlyingType(Type type)
        {
            var types = type.GetInterfaces().ToList();
            types.Add(type);
            var enumerable = types.FirstOrDefault(i => i.IsGenericType && TypeUtils.Equals(typeof(IEnumerable<>), i.GetGenericTypeDefinition()));
            if (enumerable != null)
                return enumerable.GenericTypeArguments.FirstOrDefault();

            return null;
        }

        private static bool IsDictionaryInterface(Type type)
        {
            return type.IsGenericType && (TypeUtils.Equals(type.GetGenericTypeDefinition(), typeof(IDictionary<,>)) || TypeUtils.Equals(type.GetGenericTypeDefinition(), typeof(IReadOnlyDictionary<,>)));
        }

        private static Tuple<Type, Type> GetDictionaryUnderlyingTypes(Type type)
        {
            if (IsDictionaryInterface(type))
            {
                return Tuple.Create(type.GenericTypeArguments[0], type.GenericTypeArguments[1]);
            }

            var iface = type.GetInterfaces().FirstOrDefault(IsDictionaryInterface);
            if (iface != null)
            {
                return Tuple.Create(iface.GenericTypeArguments[0], iface.GenericTypeArguments[1]);
            }

            return null;
        }

        private static string GetDerivedTypesUnionName(Type type)
        {
            var attr = TypeUtils.GetCustomAttributesData(type).FirstOrDefault(a => a.AttributeType.Name == Constants.GenerateTypeScriptDerivedTypesUnionAttributeName);
            if (attr == null)
                return null;

            return attr.ConstructorArguments.Count > 0 && attr.ConstructorArguments[0].Value is string unionName ? unionName : (TypeUtils.GetNameWithoutGenericArity(type) + "Types");
        }

        private static DerivedTypesUnionGeneration GetDerivedTypes(Type type, TypeBuilderConfig config, string currentTsNamespace, GeneratorContext generatorContext)
        {
            var derivedTypesUnionName = GetDerivedTypesUnionName(type);
            if (derivedTypesUnionName == null)
                return null;

            var derivedClasses = generatorContext.FindDerivedTypes(type);

            var derivedTypes = derivedClasses
                .Where(t => !t.IsAbstract)
                .OrderBy(t => t.Name, StringComparer.InvariantCulture);

            return new DerivedTypesUnionGeneration(ImmutableArray.CreateRange(derivedTypes.Select(i => BuildTsTypeReference(i, config, currentTsNamespace, true))), derivedTypesUnionName);
        }

        private static string GetTypeMemberName(Type type)
        {
            // No reason to create a type member on an abstract type since the purpose of the type member
            // is to identify a concrete type
            if (type.IsAbstract)
                return null;

            var typeMemberName = (string)null;
            var currentType = type;
            while (currentType != null)
            {
                var attr = TypeUtils.GetCustomAttributesData(currentType).FirstOrDefault(a => a.AttributeType.Name == Constants.GenerateTypeScriptTypeMemberAttributeName);
                if (attr != null)
                {
                    if (attr.ConstructorArguments[0].Value is string name)
                    {
                        typeMemberName = name;
                    }
                    else
                    {
                        typeMemberName = Constants.DefaultTypeMemberName;
                    }
                    break;
                }

                currentType = currentType.BaseType;
            }

            return typeMemberName;
        }

        internal static Type GetParentTypeToAugument(Type type)
        {
            var attr = TypeUtils.GetCustomAttributesData(type).FirstOrDefault(a => a.AttributeType.Name == Constants.TypeScriptAugumentParentAttributeName);

            if (attr != null)
            {
                var baseTypes = type.GetInterfaces().ToList();
                if (type.BaseType != null && !TypeUtils.Is<object>(type.BaseType))
                    baseTypes.Insert(0, type.BaseType);

                return baseTypes.FirstOrDefault();
            }

            return null;
        }

        internal static string GetTypescriptNamespace(Type type)
        {
            var parentToAugument = TypeBuilder.GetParentTypeToAugument(type);
            if (parentToAugument != null)
                type = parentToAugument;

            string DoGetTypescriptNamespace(object typeOrAssembly)
            {
                var customAttributes = typeOrAssembly is Type type ? TypeUtils.GetCustomAttributesData(type) : TypeUtils.GetAssemblyCustomAttributesData((Assembly)typeOrAssembly);

                var attr = customAttributes.FirstOrDefault(a => a.AttributeType.Name == Constants.GenerateTypeScriptNamespaceAttributeName && a.ConstructorArguments.Count == 1 && a.ConstructorArguments[0].Value is string);
                return (string)attr?.ConstructorArguments[0].Value;
            }

            for (var currentType = type; currentType != null; currentType = currentType.DeclaringType)
            {
                var ns = DoGetTypescriptNamespace(currentType);
                if (ns != null)
                {
                    return ns;
                }
            }

            return DoGetTypescriptNamespace(type.Assembly);
        }

        internal static bool ShouldGenerateDotNetTypeNamesAsJsDocComment(Type type)
        {
            bool DoShouldGenerateDotNetTypeNamesAsJsDocComment(object typeOrAssembly)
            {
                var customAttributes = typeOrAssembly is Type type ? TypeUtils.GetCustomAttributesData(type) : TypeUtils.GetAssemblyCustomAttributesData((Assembly) typeOrAssembly);
                var attr = customAttributes.FirstOrDefault(a => a.AttributeType.Name == Constants.GenerateDotNetTypeNamesAsJsDocCommentAttributeName);
                return attr != null;
            }

            if (DoShouldGenerateDotNetTypeNamesAsJsDocComment(type.Assembly))
                return true;

            for (var currentType = type; currentType != null; currentType = currentType.DeclaringType)
            {
                var shouldGenerate = DoShouldGenerateDotNetTypeNamesAsJsDocComment(currentType);
                if (shouldGenerate)
                {
                    return true;
                }
            }

            return false;
        }

        internal static Type GetCanonicalDotNetType(Type type)
        {
            var baseTypes = type.GetInterfaces().ToList();
            if (type.BaseType != null)
                baseTypes.Add(type.BaseType);

            foreach (var baseType in baseTypes)
            {
                var attr = TypeUtils.GetCustomAttributesData(baseType).FirstOrDefault(a => a.AttributeType.Name == Constants.GenerateCanonicalDotNetTypeScriptTypeAttributeAttributeName);

                if (attr != null)
                    return baseType;

                var parentCanonical = GetCanonicalDotNetType(baseType);
                if (parentCanonical != null)
                    return parentCanonical;
            }

            return null;
        }

        public static async Task<TsTypeDefinition> BuildTsTypeDefinitionAsync(Type type, TypeBuilderConfig config, GeneratorContext generatorContext)
        {
            var tsNamespace = GetTypescriptNamespace(type);

            if (type.IsEnum)
            {
                var members = Enum.GetNames(type);
                return TsTypeDefinition.Enum(type.Name, members);
            }
            else
            {
                var properties = TypeUtils.GetRelevantProperties(type);

                IList<Type> extends;
                if (type.IsInterface)
                {
                    extends = type.GetInterfaces();
                }
                else if (type.IsValueType || TypeUtils.Is<object>(type.BaseType))
                {
                    extends = new List<Type>();
                }
                else
                {
                    extends = new List<Type> { type.BaseType };
                }

                foreach (var iface in type.GetInterfaces())
                {
                    // We look for default interface properties because we don't generate an extends clause for C# interfaces
                    // but that means we loose default interface properties. If any of the types interface has default properties
                    // we include it in the extends clause.
                    var defaultInterfaceProperties = TypeUtils.GetRelevantProperties(iface).Where(p => p.GetGetMethod()?.IsAbstract == false);

                    properties.AddRange(defaultInterfaceProperties);
                }

                var parentToAugument = GetParentTypeToAugument(type);
                var parentDefToAugument = default(TsTypeDefinition);
                if (parentToAugument != null)
                {
                    parentDefToAugument = await BuildTsTypeDefinitionAsync(parentToAugument, config, generatorContext);
                }

                return TsTypeDefinition.Interface(type,
                                                  properties.Select(p => BuildMember(p, type.GetInterfaces(), config, tsNamespace)).Where(x => x != null),
                                                  extends.Select(e => BuildTsTypeReference(e, config, tsNamespace, true)),
                                                  type.GetTypeInfo().GenericTypeParameters.Select(TypeUtils.GetNameWithoutGenericArity),
                                                  GetDerivedTypes(type, config, tsNamespace, generatorContext),
                                                  parentDefToAugument,
                                                  GetTypeMemberName(type));
            }
        }
    }
}