using System;
using System.Collections.Generic;
using System.Text;

namespace TSTypeGen.Tests.Shared
{
    [GenerateTypeScriptDefinition]
    public class SharedClass
    {
        public int SharedProp { get; set; }
    }
}
