using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestTypeScriptTypeOnTypeClass1 : TestTypeScriptTypeOnTypeClass2
    {
        public TestTypeScriptTypeOnTypeClass2 Prop1 { get; set; }
        public TestTypeScriptTypeOnTypeClass3 Prop2 { get; set; }
    }

    [TypeScriptType(typeof(TypeScriptTypeOnTypeReplacedWith))]
    public class TestTypeScriptTypeOnTypeClass2
    {
    }

    [TypeScriptType("external.NewClass")]
    public class TestTypeScriptTypeOnTypeClass3
    {
    }

    [GenerateTypeScriptDefinition]
    public class TypeScriptTypeOnTypeReplacedWith
    {
    }
}
