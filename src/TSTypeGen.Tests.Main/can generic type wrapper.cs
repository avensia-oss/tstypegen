using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{

    [GenerateTypeScriptDefinition]
    public class TestGenericTypeWrapperClass2
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    [TypeScriptGenericWrapperTypeForMembers("Scope.EpiProperty")]
    public class TestGenericTypeWrapperClass1
    {
        public int Prop1 { get; set; }

        public string Prop2 { get; set; }

        public TestGenericTypeWrapperClass2 Prop3 { get; set; }
    }
}
