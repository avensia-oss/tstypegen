using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    [TypeScriptGenericWrapperTypeForMembers("Scope.EpiProperty")]
    public class TestGenericTypeWrapperBase
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestGenericTypeWrapperBase1: TestGenericTypeWrapperBase
    {

        public string Prop2 { get; set; }

    }
}
