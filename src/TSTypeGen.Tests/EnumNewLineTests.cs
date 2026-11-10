using NUnit.Framework;

namespace TSTypeGen.Tests
{
    [TestFixture]
    public class EnumNewLineTests
    {
        [TestCase("\n")]
        [TestCase("\r\n")]
        public void StringUnionUsesConfiguredNewLine(string newLine)
        {
            var definition = TsTypeDefinition.Enum("Example", new[] { "First", "Second" });
            var config = new Config { UseConstEnums = false, NewLine = newLine };

            var source = definition.GetSource(null, config, null);

            Assert.AreEqual("  type Example = 'first' | 'second';" + newLine, source);
        }
    }
}
