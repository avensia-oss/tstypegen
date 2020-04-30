using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using NUnit.Framework;

namespace TSTypeGen.Tests
{
    [TestFixture]
    public class ProjectTests
    {
        private static readonly string TempDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TempTestProject");

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (!MSBuildLocator.IsRegistered)
                MSBuildLocator.RegisterDefaults();
        }

        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, true);
            }
            Directory.CreateDirectory(TempDirectory);
            TestUtil.CreateSharedProject(TempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, true);
            }
        }

        [Test]
        public async Task Can_update_types_for_a_single_project()
        {
            var otherProjectDirectory = Path.Combine(TempDirectory, "OtherProject");
            var mainProjectDirectory = Path.Combine(TempDirectory, "MainProject");
            Directory.CreateDirectory(otherProjectDirectory);
            Directory.CreateDirectory(mainProjectDirectory);

            File.WriteAllText(Path.Combine(mainProjectDirectory, "ProjectFile.cs"),
                @"using Avensia;
                using OtherProject;
                namespace MainProject {
                    [GenerateTypeScriptDefinition]
                    class SomeType {
                        public SomeOtherType Prop1 { get; set; }
                        public int Prop2 { get; set; }
                    }
                }");

            File.WriteAllText(Path.Combine(otherProjectDirectory, "AssemblyInfo.cs"),
                @"using Avensia;
                [assembly: GenerateTypeScriptNamespace(""OtherProject"")]");

            File.WriteAllText(Path.Combine(otherProjectDirectory, "OtherProjectFile.cs"),
                @"
                namespace OtherProject {
                    [GenerateTypeScriptDefinition]
                    class SomeOtherType {
                        public int Prop1;
                    }
                }");

            File.WriteAllText(Path.Combine(otherProjectDirectory, "OtherProject.csproj"),
                $@"<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
                  <PropertyGroup>
                    <ProjectGuid>{{36AE3EBC-BF60-4A4F-BC8F-9684F71C116C}}</ProjectGuid>
                    <AssemblyName>Test</AssemblyName>
                    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include=""..\Avensia\Avensia.csproj"">
                      <Project>{{46AE3EBC-BF60-4A4F-BC8F-9684F71C116C}}</Project>
                      <Name>Avensia</Name>
                    </ProjectReference>
                    <Reference Include=""Newtonsoft.Json"">
                      <HintPath>{typeof(JsonSerializer).Assembly.Location}</HintPath>
                    </Reference>
                    <Compile Include=""OtherProjectFile.cs"" />
                    <Compile Include=""AssemblyInfo.cs"" />
                  </ItemGroup>
                  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
                </Project>");

            File.WriteAllText(Path.Combine(mainProjectDirectory, "Project.csproj"),
                $@"<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
                  <PropertyGroup>
                    <ProjectGuid>{{EB00F436-D5AB-43EE-B2DB-B5E8FFE44BF1}}</ProjectGuid>
                    <AssemblyName>Test</AssemblyName>
                    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include=""..\Avensia\Avensia.csproj"">
                      <Project>{{46AE3EBC-BF60-4A4F-BC8F-9684F71C116C}}</Project>
                      <Name>Avensia</Name>
                    </ProjectReference>
                    <ProjectReference Include=""..\OtherProject\OtherProject.csproj"">
                      <Project>{{36AE3EBC-BF60-4A4F-BC8F-9684F71C116C}}</Project>
                      <Name>OtherProject</Name>
                    </ProjectReference>
                    <Compile Include=""ProjectFile.cs"" />
                  </ItemGroup>
                  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
                </Project>");

            var processor = new Processor(Path.Combine(mainProjectDirectory, "Project.csproj"), new Config());

            await processor.UpdateTypesAsync();

            var actualGeneratedFiles = Directory.GetFiles(TempDirectory, "*.ts", SearchOption.AllDirectories);
            Assert.That(actualGeneratedFiles, Has.Length.EqualTo(1));
            Assert.That(actualGeneratedFiles[0].Substring(TempDirectory.Length + 1).Replace(Path.DirectorySeparatorChar, '/'), Is.EqualTo("MainProject/SomeType.type.ts"));

            Assert.That(File.ReadAllText(actualGeneratedFiles[0]).Replace("\r\n", "\n"), Is.EqualTo(
@"interface SomeType {
  prop1: OtherProject.SomeOtherType;
  prop2: number;
}

export default SomeType;
".Replace("\r\n", "\n")));
        }
    }
}
