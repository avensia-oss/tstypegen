using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public record TestRecord1
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public record TestRecord2
    {
        public TestRecord1 Prop1 { get; set; }
    }
}
