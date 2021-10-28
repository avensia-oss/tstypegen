using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestGenericTypeWrapperBase : WrapMe
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestGenericTypeWrapperBase1: TestGenericTypeWrapperBase
    {
        public string Prop2 { get; set; }
    }
}
