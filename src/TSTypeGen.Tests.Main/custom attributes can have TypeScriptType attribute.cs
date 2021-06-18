using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestTypeScriptStringClass
    {
        [TypeScriptString]
        public int Prop1 { get; set; }
    }
}
