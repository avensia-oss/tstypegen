using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public struct TestStruct1
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public struct TestStruct2
    {
        public TestStruct1 Prop1 { get; set; }
    }
}
