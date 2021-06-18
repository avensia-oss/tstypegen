using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDerivedTypesUnion]
    [GenerateTypeScriptDefinition]
    public abstract class TestUnionFieldsBase
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestUnionFieldsClass1 : TestUnionFieldsBase
    {
        public int Prop2 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestUnionFieldsClass2 : TestUnionFieldsBase
    {
        public int Prop3 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestUnionFieldsMainTestClass
    {
        public TestUnionFieldsBase Prop1 { get; set; }
        public List<TestUnionFieldsBase> Prop2 { get; set; }
        public TestUnionFieldsBase[] Prop3 { get; set; }
        public Dictionary<string, TestUnionFieldsBase> Prop4 { get; set; }
    }
}
