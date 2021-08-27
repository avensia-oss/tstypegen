using System;

namespace TSTypeGen.Tests.Shared
{
    public sealed class GenerateTypeScriptDefinitionAttribute : Attribute { public GenerateTypeScriptDefinitionAttribute() { } public GenerateTypeScriptDefinitionAttribute(bool generate) { } }

    public sealed class TypeScriptGenericWrapperTypeForMembersAttribute : Attribute { public TypeScriptGenericWrapperTypeForMembersAttribute(string name) { } }

    public sealed class GenerateTypeScriptNamespaceAttribute : Attribute { public GenerateTypeScriptNamespaceAttribute(string name) { } }
    public sealed class GenerateDotNetTypeNamesAsJsDocCommentAttribute : Attribute { public GenerateDotNetTypeNamesAsJsDocCommentAttribute() { } }
    public sealed class GenerateCanonicalDotNetTypeScriptTypeAttribute : Attribute { public GenerateCanonicalDotNetTypeScriptTypeAttribute() { } }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)] public sealed class DefineTypeScriptTypeForExternalTypeAttribute : Attribute { public DefineTypeScriptTypeForExternalTypeAttribute(Type type, string name) { } public DefineTypeScriptTypeForExternalTypeAttribute(string qualifiedTypeName, string name) { } }
    public sealed class TypeScriptTypeAttribute : Attribute { public TypeScriptTypeAttribute(Type type) { } public TypeScriptTypeAttribute(string type) { } }
    public sealed class TypeScriptOptionalAttribute : Attribute { }
    public sealed class TypeScriptAugumentParentAttribute : Attribute { }
    public sealed class TypeScriptIgnoreAttribute : Attribute { }
    public sealed class GenerateTypeScriptDerivedTypesUnionAttribute : Attribute { public GenerateTypeScriptDerivedTypesUnionAttribute(string name = null) { } }
    public sealed class GenerateTypeScriptTypeMemberAttribute : Attribute { public GenerateTypeScriptTypeMemberAttribute(string name = null) { } }
    public sealed class GenerateTypeScriptDotNetNameAttribute : Attribute { public GenerateTypeScriptDotNetNameAttribute(Type type) { } }
    public sealed class CustomTypeScriptIgnoreAttribute : Attribute { }
    public interface IStructuralSubsetOf<T> { }
    public interface IPartialContentData { }
    [TypeScriptType("string")] public class TypeScriptStringAttribute : Attribute { }
}

namespace Newtonsoft.Json
{
    public class JsonPropertyAttribute : Attribute
    {
        public string PropertyName { get; set; }

        public JsonPropertyAttribute()
        {
        }

        public JsonPropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }

    public class JsonIgnoreAttribute : Attribute
    {
    }
}