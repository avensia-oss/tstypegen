using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public record TestRecordInheritanceBase
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public record TestRecordInheritance : TestRecordInheritanceBase
    {
        public string Prop2 { get; set; }
    }
}
