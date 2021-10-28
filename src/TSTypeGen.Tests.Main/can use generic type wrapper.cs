using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    public interface WrapMe
    {

    }

    [GenerateTypeScriptDefinition]
    public class TestGenericTypeWrapperClass2
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestGenericTypeWrapperClass1 : WrapMe
    {
        public int Prop1 { get; set; }

        public string Prop2 { get; set; }

        public TestGenericTypeWrapperClass2 Prop3 { get; set; }
    }
}
