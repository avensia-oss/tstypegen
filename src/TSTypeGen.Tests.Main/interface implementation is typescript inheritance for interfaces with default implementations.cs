using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public interface TestDefaultInterfaceImplementation
    {
        public int Prop1 => 1;
    }

    [GenerateTypeScriptDefinition]
    public class TestDefaultInterfaceImplementationClass : TestDefaultInterfaceImplementation
    {
        public int Prop2 { get; set; }
    }
}
