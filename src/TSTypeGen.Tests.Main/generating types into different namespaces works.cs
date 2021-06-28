using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class ExplicitNamespaceModuleClass
    {
        public int Prop { get; set; }
    }

    [GenerateTypeScriptNamespace("testexplicit")]
    public class ExplicitNamespaceTestClass1
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptNamespace("testexplicit2")]
    public class ExplicitNamespaceTestClass4
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptNamespace("testexplicit2")]
    public enum ExplicitNamespaceTestEnum
    {
        FirstValue,
        SecondValue,
        ThirdValue
    }

    [GenerateTypeScriptNamespace("testexplicit")]
    public class ExplicitNamespaceTestClass2 : ExplicitNamespaceTestClass1
    {
        public int Prop2 { get; set; }
        public ExplicitNamespaceTestEnum Prop3 { get; set; }
    }
}
