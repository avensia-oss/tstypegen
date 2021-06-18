using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    [GenerateTypeScriptNamespace("parentns")]
    public interface TestTypeScriptAugumentParentInterface
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    [TypeScriptAugumentParent]
    public class TestTypeScriptAugumentParentInterfaceImpl : TestTypeScriptAugumentParentInterface
    {
        public int Prop2 { get; set; }
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    [GenerateTypeScriptNamespace("parentns")]
    public class TestTypeScriptAugumentParentBaseClass
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    [TypeScriptAugumentParent]
    public class TestTypeScriptAugumentParentChildClass : TestTypeScriptAugumentParentBaseClass
    {
        public int Prop2 { get; set; }
    }
}
