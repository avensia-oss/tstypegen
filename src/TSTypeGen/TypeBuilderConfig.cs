using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSTypeGen
{
    public class TypeBuilderConfig
    {
        public ImmutableDictionary<string, TsTypeReference> TypeMappings { get; }
        public TsTypeReference PropertyTypeReference { get; }
        public string StructuralSubsetOfInterfaceFullName { get; set; }
        public string CustomTypeScriptIgnoreAttributeFullName { get; set; }
        public List<string> TypesToWrapPropertiesFor { get; set; }

        public TypeBuilderConfig(ImmutableDictionary<string, TsTypeReference> typeMappings,
            TsTypeReference propertyTypeReference, List<string> typesToWrapPropertiesFor,
            string structuralSubsetOfInterfaceFullName, string customTypeScriptIgnoreAttributeFullName)
        {
            TypeMappings = typeMappings;
            PropertyTypeReference = propertyTypeReference;
            StructuralSubsetOfInterfaceFullName = structuralSubsetOfInterfaceFullName;
            CustomTypeScriptIgnoreAttributeFullName = customTypeScriptIgnoreAttributeFullName;
            TypesToWrapPropertiesFor = typesToWrapPropertiesFor;
        }
    }
}