using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public interface ITestInterfaceGeneration1
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition(true)]
    public interface ITestInterfaceGeneration2
    {
        public int Prop2 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public interface ITestInterfaceGeneration3 : ITestInterfaceGeneration1, ITestInterfaceGeneration2
    {
        public int Prop3 { get; set; }
    }
}
