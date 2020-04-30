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
    public class TestCaseRunner
    {
        private class FileData
        {
            public string Path { get; set; }
            public string Content { get; set; }

            public FileData(string path, string content)
            {
                Path = path;
                Content = content;
            }
        }

        private class TestCase
        {
            public List<FileData> Inputs { get; } = new List<FileData>();
            public List<FileData> Updates { get; } = new List<FileData>();
            public List<FileData> Outputs { get; } = new List<FileData>();
            public List<string> ReferenceSources { get; } = new List<string>();
            public Config Config { get; set; }
        }

        private const string TestCaseNamespace = "TSTypeGen.Tests.TestCases";

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

        private string ReadTestCase(string name)
        {
            using (var strm = Assembly.GetExecutingAssembly().GetManifestResourceStream(TestCaseNamespace + "." + name + ".txt"))
            using (var rdr = new StreamReader(strm))
            {
                return rdr.ReadToEnd();
            }
        }

        private TestCase ParseTestCase(string testCaseSource)
        {
            var result = new TestCase { Config = new Config() };
            string currentFile = null;
            var currentContent = new StringBuilder();

            void Flush()
            {
                if (currentFile != null)
                {
                    if (currentFile.Equals("config", StringComparison.OrdinalIgnoreCase))
                    {
                        JsonConvert.PopulateObject(currentContent.ToString(), result.Config);
                    }
                    else if (currentFile.Equals("compile", StringComparison.OrdinalIgnoreCase))
                    {
                        result.ReferenceSources.Add(currentContent.ToString());
                    }
                    else if (currentFile.StartsWith("write ", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Updates.Add(new FileData(currentFile.Substring(6), currentContent.ToString()));
                    }
                    else if (currentFile.StartsWith("delete ", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Updates.Add(new FileData(currentFile.Substring(7), null));
                    }
                    else
                    {
                        var target = currentFile.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? result.Inputs : result.Outputs;
                        target.Add(new FileData(currentFile, currentContent.ToString()));
                    }
                }
                currentContent.Length = 0;
            }

            foreach (var line in testCaseSource.Replace("\r\n", "\n").Split('\n'))
            {
                if (line.StartsWith("@"))
                {
                    Flush();
                    currentFile = line.Substring(1).Trim().Replace("\\", "/");
                }
                else if (line.StartsWith("--"))
                {
                    Flush();
                    currentFile = null;
                }
                else
                {
                    currentContent.Append(line).Append('\n');
                }
            }

            Flush();

            result.Config.BasePath = TempDirectory;

            return result;
        }

        private string CreateProjectFile(TestCase testCase)
        {
            foreach (var f in testCase.Inputs)
            {
                var path = Path.Combine(TempDirectory, f.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, f.Content);
            }

            var referenceLib = "";
            if (testCase.ReferenceSources.Any())
            {
                var references = new List<string>();
                for (int i = 0; i < testCase.ReferenceSources.Count; i++)
                {
                    var currentName = "Reference" + i.ToString(CultureInfo.InvariantCulture) + ".cs";
                    references.Add(currentName);

                    var referenceFile = Path.Combine(TempDirectory, currentName);
                    File.WriteAllText(referenceFile, testCase.ReferenceSources[i]);
                }

                var referenceProjectFile = Path.Combine(TempDirectory, "Reference.csproj");
                var guid = Guid.NewGuid();
                File.WriteAllText(referenceProjectFile,
                    $@"<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
                                <PropertyGroup>
                                  <ProjectGuid>{guid}</ProjectGuid>
                                  <AssemblyName>ReferenceLib</AssemblyName>
                                  <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
                                </PropertyGroup>
                                <ItemGroup>
                                  <ProjectReference Include=""Avensia\Avensia.csproj"">
                                    <Project>{{46AE3EBC-BF60-4A4F-BC8F-9684F71C116C}}</Project>
                                    <Name>Avensia</Name>
                                  </ProjectReference>
                                  {string.Join(Environment.NewLine, references.Select(r => $"<Compile Include=\"{r}\" />"))}
                                </ItemGroup>
                                <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
                              </Project>");

                referenceLib = $@"
                    <ProjectReference Include=""Reference.csproj"">
                        <Project>{guid}</Project>
                        <Name>ReferenceLib</Name>
                    </ProjectReference>
                ";
            }

            var projectFile = Path.Combine(TempDirectory, "Test.csproj");
            File.WriteAllText(projectFile,
$@"<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <ProjectGuid>{{36AE3EBC-BF60-4A4F-BC8F-9684F71C116C}}</ProjectGuid>
    <AssemblyName>Test</AssemblyName>
    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""Avensia\Avensia.csproj"">
      <Project>{{46AE3EBC-BF60-4A4F-BC8F-9684F71C116C}}</Project>
      <Name>Avensia</Name>
    </ProjectReference>
    {referenceLib}
    {string.Join(Environment.NewLine, testCase.Inputs.Select(f => $"<Compile Include=\"{f.Path}\" />"))}
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>");

            return projectFile;
        }

        private void ApplyChanges(TestCase testCase)
        {
            foreach (var f in testCase.Updates)
            {
                var path = Path.Combine(TempDirectory, f.Path);
                if (f.Content != null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, f.Content);
                }
                else
                {
                    File.Delete(path);
                }
            }
        }

        private void AssertGeneratedTypes(IList<FileData> expected)
        {
            var actualGeneratedFiles = Directory.GetFiles(TempDirectory, "*.ts", SearchOption.AllDirectories);
            Assert.That(actualGeneratedFiles.Select(x => x.Substring(TempDirectory.Length + 1).Replace(Path.DirectorySeparatorChar, '/')), Is.EquivalentTo(expected.Select(x => x.Path)));

            foreach (var expectedFile in expected)
            {
                var actualContent = File.ReadAllText(Path.Combine(TempDirectory, expectedFile.Path));
                var currentExpected = expectedFile.Content.Trim('\n');
                var currentActual = actualContent.Replace("\r\n", "\n").Trim('\n');
                string message = $"Error in {expectedFile.Path}\nExpected:\n{currentExpected}\n\nActual:\n{currentActual}".Replace("\n", Environment.NewLine);
                Assert.That(currentActual, Is.EqualTo(currentExpected), message);
            }
        }

        public static string[] GetTestCases()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(r => r.StartsWith(TestCaseNamespace + ".") && r.EndsWith(".txt")).Select(x => x.Substring(TestCaseNamespace.Length + 1, x.Length - TestCaseNamespace.Length - 5)).ToArray();
        }

        [Test, TestCaseSource(nameof(GetTestCases))]
        public async Task RunTestCase(string name)
        {
            var data = ReadTestCase(name);

            if (string.IsNullOrEmpty(data))
            {
                Assert.Inconclusive("Test case is empty");
            }

            var testCase = ParseTestCase(data);

            var slnFile = CreateProjectFile(testCase);
            var processor = new Processor(slnFile, testCase.Config);

            if (testCase.Updates.Count == 0)
            {
                await processor.UpdateTypesAsync();
            }
            else
            {
                using (var tokenSource = new CancellationTokenSource())
                {
                    var stopwatch = Stopwatch.StartNew();
                    var watchTask = processor.WatchAsync(tokenSource.Token);
                    while (!processor.IsWatching)
                    {
                        if (watchTask.IsCompleted)
                        {
                            Assert.Fail($"The watch task completed with result {watchTask.Result} without starting the watch");
                        }

                        if (stopwatch.Elapsed > TimeSpan.FromSeconds(10))
                        {
                            Assert.Fail("It took more than 10 seconds to start the watch");
                        }
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }

                    ApplyChanges(testCase);

                    await Task.Delay(TimeSpan.FromMilliseconds(100));

                    tokenSource.Cancel();

                    watchTask.Wait(TimeSpan.FromSeconds(10));

                    Assert.That(watchTask.Result, Is.True);
                }
            }

            AssertGeneratedTypes(testCase.Outputs);
        }
    }
}
