using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptTypeMember]
    [GenerateTypeScriptDefinition]
    public class TestGenerateTypeMemberClass1
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptTypeMember("TheTypeOfThis")]
    [GenerateTypeScriptDefinition]
    public class TestGenerateTypeMemberClass2
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptTypeMember]
    [GenerateTypeScriptDefinition]
    public abstract class TestGenerateTypeMemberBase
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestGenerateTypeMemberClass3 : TestGenerateTypeMemberBase
    {
        public int Prop2 { get; set; }
    }
}
