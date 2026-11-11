using System;
using System.IO;
using NUnit.Framework;

namespace TSTypeGen.Tests
{
    [TestFixture]
    public class ConfigTests
    {
        [TestCase("{}", false)]
        [TestCase("{\"generateEnumFiles\": false}", false)]
        [TestCase("{\"generateEnumFiles\": true}", true)]
        public void EnumFilesAreOptIn(string json, bool expected)
        {
            Assert.IsFalse(new Config().GenerateEnumFiles);
            var path = Path.Join(Path.GetTempPath(), "TSTypeGen.Config-" + Guid.NewGuid() + ".json");
            try
            {
                File.WriteAllText(path, json);
                Assert.AreEqual(expected, Config.ReadFromFile(path).GenerateEnumFiles);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
