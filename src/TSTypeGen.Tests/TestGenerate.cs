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
        [TestCase("\n", false)]
        [TestCase("\n", true)]
        [TestCase("\r\n", false)]
        [TestCase("\r\n", true)]
        public async Task RunTestCase(string newLine, bool generateEnumFiles)
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var outputPath = Path.Join(Path.GetTempPath(), "TSTypeGen.Tests-" + Guid.NewGuid());
            Directory.CreateDirectory(outputPath);
            try
            {
                await RunTestCaseAsync(assemblyLocation, outputPath, newLine, generateEnumFiles);
            }
            finally
            {
                Directory.Delete(outputPath, true);
            }
        }

        private async Task RunTestCaseAsync(string assemblyLocation, string outputPath, string newLine, bool generateEnumFiles)
        {
            var config = new Config
            {
                BasePath = assemblyLocation,
                OutputPath = outputPath,
                DllPatterns = new List<string> { "TSTypeGen.Tests.Main.dll", "TSTypeGen.Tests.Shared.dll" },
                CustomTypeScriptIgnoreAttributeFullName = "TSTypeGen.Tests.Shared.CustomTypeScriptIgnoreAttribute",
                NewLine = newLine,
                GenerateEnumFiles = generateEnumFiles,
                PropertyWrappers = new Dictionary<string, string>() { { "TSTypeGen.Tests.Main.WrapMe", "Wrapper" } }
            };

            var processor = new Processor(config);

            Assert.IsTrue(await processor.UpdateTypesAsync());
            Assert.IsTrue(await processor.VerifyTypesAsync());
            var generatedFiles = Directory.GetFiles(outputPath);

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

            var testFixtures = Directory.GetFiles(testFixturesPath, "*.d.ts")
                .Concat(Directory.GetFiles(testFixturesPath, "*.ts"))
                .Concat(Directory.GetFiles(testFixturesPath, "*.json"))
                .ToArray();
            foreach (var testFixture in testFixtures)
            {
                if (!generateEnumFiles && testFixture.EndsWith(".enums.ts"))
                {
                    Assert.IsFalse(File.Exists(Path.Join(outputPath, Path.GetFileName(testFixture))));
                    continue;
                }

                var generatedTestFixture = generatedFiles.FirstOrDefault(g => Path.GetFileName(g) == Path.GetFileName(testFixture));
                if (generatedTestFixture == null)
                    throw new Exception($"Expected to find a generated file called {Path.GetFileName(testFixture)} but no such file was generated");

                var testFixtureSource = await File.ReadAllTextAsync(testFixture);
                var generatedTestFixtureSource = await File.ReadAllTextAsync(generatedTestFixture);

                Assert.AreEqual(testFixtureSource.Replace("\r\n", "\n"), generatedTestFixtureSource.Replace("\r\n", "\n"), Path.GetFileName(testFixture));
                if (testFixture.EndsWith(".ts"))
                    Assert.AreEqual(generatedTestFixtureSource.Replace("\r\n", "\n").Replace("\n", newLine), generatedTestFixtureSource, "Configured line endings: " + Path.GetFileName(testFixture));
            }

            if (generateEnumFiles)
            {
                var enumFile = Path.Join(outputPath, "Test.enums.ts");
                File.Delete(enumFile);
                Assert.IsFalse(await processor.VerifyTypesAsync(), "Missing enabled enum files must fail verification");
                Assert.IsTrue(await processor.UpdateTypesAsync());

                config.GenerateEnumFiles = false;
                Assert.IsFalse(await processor.VerifyTypesAsync(), "Stale disabled enum files must fail verification");
                Assert.IsTrue(await processor.UpdateTypesAsync());
                Assert.IsEmpty(Directory.GetFiles(outputPath, "*.enums.ts"));
                Assert.IsTrue(await processor.VerifyTypesAsync());
            }
        }
    }
}
