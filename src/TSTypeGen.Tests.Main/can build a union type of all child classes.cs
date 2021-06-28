using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDerivedTypesUnion]
    [GenerateTypeScriptDefinition]
    public abstract class TestDerivedTypesUnionBase
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestDerivedTypesUnionClass1 : TestDerivedTypesUnionBase
    {
        public int Prop2 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    [GenerateTypeScriptDerivedTypesUnion("MyUnion")]
    public abstract class TestDerivedTypesUnionClass2 : TestDerivedTypesUnionBase
    {
        public int Prop2 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestDerivedTypesUnionClass3 : TestDerivedTypesUnionClass2
    {
        public int Prop3 { get; set; }
    }
}
