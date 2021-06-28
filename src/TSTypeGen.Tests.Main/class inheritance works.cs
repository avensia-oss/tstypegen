using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestInheritanceBase
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestInheritanceClass : TestInheritanceBase
    {
        public int Prop2 { get; set; }
    }
}
