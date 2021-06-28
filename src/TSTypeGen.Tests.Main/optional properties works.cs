using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public enum TestOptionalPropertiesEnum
    {
        Value1,
        Value2
    }

    [GenerateTypeScriptDefinition]
    public struct TestOptionalPropertiesNestedClass
    {
        public int NestedProp1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestOptionalPropertiesClass
    {
        [TypeScriptOptional]
        public int Prop1 { get; set; }
        [TypeScriptOptional]
        public string Prop2 { get; set; }
        [TypeScriptOptional]
        public List<string> Prop3 { get; set; }
        [TypeScriptOptional]
        public Dictionary<string, string> Prop4 { get; set; }
        [TypeScriptOptional]
        public TestOptionalPropertiesEnum Prop5 { get; set; }
        [TypeScriptOptional]
        public TestOptionalPropertiesNestedClass Prop6 { get; set; }
    }
}
