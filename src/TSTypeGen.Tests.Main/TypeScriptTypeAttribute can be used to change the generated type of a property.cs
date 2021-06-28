using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestTypeScriptTypeClass1
    {
        [TypeScriptType(typeof(string))]
        public int Prop1 { get; set; }

        [TypeScriptType(typeof(TestTypeScriptTypeClass2))]
        public int Prop2 { get; set; }

        [TypeScriptType("external.Type")]
        public int Prop3 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestTypeScriptTypeClass2
    {
    }
}
