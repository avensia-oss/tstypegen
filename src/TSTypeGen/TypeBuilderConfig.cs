using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSTypeGen
{
    public class TypeBuilderConfig
    {
        public ImmutableDictionary<string, TsTypeReference> TypeMappings { get; }
        public string CustomTypeScriptIgnoreAttributeFullName { get; set; }
        public bool WrapConstEnumsInTemplateStrings { get; set; }

        public TypeBuilderConfig(ImmutableDictionary<string, TsTypeReference> typeMappings, string customTypeScriptIgnoreAttributeFullName, bool wrapConstEnumsInTemplateStrings)
        {
            TypeMappings = typeMappings;
            CustomTypeScriptIgnoreAttributeFullName = customTypeScriptIgnoreAttributeFullName;
            WrapConstEnumsInTemplateStrings = wrapConstEnumsInTemplateStrings;
        }
    }
}