using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace TSTypeGen.Tests
{
    public static class TestUtil
    {
        public static void CreateSharedProject(string tempDirectory)
        {
            var avensiaProjectDirectory = Path.Combine(tempDirectory, "Avensia");
            Directory.CreateDirectory(avensiaProjectDirectory);

            File.WriteAllText(Path.Combine(avensiaProjectDirectory, "Main.cs"),
                @"using System;
                namespace Avensia {
                    public sealed class GenerateTypeScriptDefinitionAttribute : Attribute { public GenerateTypeScriptDefinitionAttribute() {} public GenerateTypeScriptDefinitionAttribute(bool generate) {} }
                    public sealed class GenerateTypeScriptNamespaceAttribute : Attribute { public GenerateTypeScriptNamespaceAttribute(string name) {} }
                    public sealed class GenerateDotNetTypeNamesAsJsDocCommentAttribute : Attribute { public GenerateDotNetTypeNamesAsJsDocCommentAttribute() {} }
                    public sealed class GenerateCanonicalDotNetTypeScriptTypeAttribute : Attribute { public GenerateCanonicalDotNetTypeScriptTypeAttribute() {} }
                    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)] public sealed class DefineTypeScriptTypeForExternalTypeAttribute : Attribute { public DefineTypeScriptTypeForExternalTypeAttribute(Type type, string name) {} public DefineTypeScriptTypeForExternalTypeAttribute(string qualifiedTypeName, string name) {} }
                    public sealed class TypeScriptTypeAttribute : Attribute { public TypeScriptTypeAttribute(Type type) {} public TypeScriptTypeAttribute(string type) {} }
                    [TypeScriptType(\""string"")] public class TypeScriptStringAttribute : Attribute {}
                    public sealed class TypeScriptOptionalAttribute : Attribute {}
                    public sealed class TypeScriptAugumentParentAttribute : Attribute {}
                    public sealed class TypeScriptIgnoreAttribute : Attribute {}
                    public sealed class GenerateTypeScriptDerivedTypesUnionAttribute : Attribute { public GenerateTypeScriptDerivedTypesUnionAttribute(string name = null) {} }
                    public sealed class GenerateTypeScriptTypeMemberAttribute : Attribute { public GenerateTypeScriptTypeMemberAttribute(string name = null) {} }
                    public sealed class GenerateTypeScriptConstEnumAttribute : Attribute {}
                    public sealed class GenerateTypeScriptDotNetNameAttribute : Attribute { public GenerateTypeScriptDotNetNameAttribute(Type type) {} }
                    public sealed class CustomTypeScriptIgnoreAttribute : Attribute {}
                    public interface IStructuralSubsetOf<T> {}
                    public interface IPartialContentData {}
                }
                namespace Cms {
                    public interface CmsData {}
                }
                namespace Newtonsoft.Json {
                    public class JsonPropertyAttribute : Attribute {
                        public string PropertyName { get; set; }

                        public JsonPropertyAttribute() {}
                        public JsonPropertyAttribute(string propertyName) {}
                    }
                    public class JsonIgnoreAttribute : Attribute {}
                }");


            File.WriteAllText(Path.Combine(avensiaProjectDirectory, "Avensia.csproj"),
                $@"<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
                  <PropertyGroup>
                    <ProjectGuid>{{46AE3EBC-BF60-4A4F-BC8F-9684F71C116C}}</ProjectGuid>
                    <AssemblyName>Avensia</AssemblyName>
                    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
                  </PropertyGroup>
                  <ItemGroup>
                    <Compile Include=""Main.cs"" />
                  </ItemGroup>
                  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
                </Project>");
        }
    }
}
