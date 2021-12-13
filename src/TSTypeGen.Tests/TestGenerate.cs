using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TSTypeGen.Tests
{
    [TestFixture]
    public class TestGenerate
    {
        [Test]
        public async Task RunTestCase()
        {
            var assemblyLocation = GetAssemblyLocation();

            var config = new Config
            {
                BasePath = assemblyLocation,
                OutputPath = assemblyLocation,
                DllPatterns = new List<string> {"TSTypeGen.Tests.Main.dll", "TSTypeGen.Tests.Shared.dll"},
                CustomTypeScriptIgnoreAttributeFullName = "TSTypeGen.Tests.Shared.CustomTypeScriptIgnoreAttribute",
                NewLine = "\n",
                PropertyWrappers = new Dictionary<string, string>(){ { "TSTypeGen.Tests.Main.WrapMe", "Wrapper"} }
            };

            var processor = new Processor(config);

            var oldGeneratedFiles = Directory.GetFiles(assemblyLocation, "*.d.ts");
            foreach (var oldGeneratedFile in oldGeneratedFiles)
                File.Delete(oldGeneratedFile);

            await processor.UpdateTypesAsync();
            var generatedFiles = Directory.GetFiles(assemblyLocation, "*.d.ts");

            var testFixturesPath = assemblyLocation;
            var n = 0;
            while (true)
            {
                if (!Directory.Exists(Path.Join(testFixturesPath, "TestFixtures")))
                {
                    testFixturesPath = Path.Join(testFixturesPath, "..");
                }
                else
                {
                    testFixturesPath = Path.Join(testFixturesPath, "TestFixtures");
                    break;
                }

                n++;
                if (n > 10)
                    throw new InvalidOperationException("Could not find the TestFixtures folder");
            }

            var testFixtures = Directory.GetFiles(testFixturesPath, "*.d.ts");
            foreach (var testFixture in testFixtures)
            {
                var generatedTestFixture = generatedFiles.FirstOrDefault(g => Path.GetFileName(g) == Path.GetFileName(testFixture));
                if (generatedTestFixture == null)
                    throw new Exception($"Expected to find a generated file called {Path.GetFileName(testFixture)} but no such file was generated");

                var testFixtureSource = await File.ReadAllTextAsync(testFixture);
                var generatedTestFixtureSource = await File.ReadAllTextAsync(generatedTestFixture);

                Assert.AreEqual(testFixtureSource, generatedTestFixtureSource);
            }
        }

        private string GetAssemblyLocation()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}
