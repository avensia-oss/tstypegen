using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestOnlyInstancePropertiesGeneratedClass
    {
        public int Prop1 { get; set; }
        public void Method1() { }
        public int Field1;
        public event System.EventHandler Event1;

        public static int Prop2 { get; set; }
        public static void Method2() { }
        public static int Field2;
        public static event System.EventHandler Event2;

        public int this[int x] { get { return x; } set { } }
    }
}
