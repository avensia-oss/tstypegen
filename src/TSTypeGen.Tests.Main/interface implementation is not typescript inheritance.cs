using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public interface InterfaceImplementationTest
    {
        int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class InterfaceImplementationTestClass : InterfaceImplementationTest
    {
        public int Prop1 { get; set; }
        public int Prop2 { get; set; }
    }
}
