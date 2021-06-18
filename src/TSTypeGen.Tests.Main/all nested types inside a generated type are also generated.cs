using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestNestedClasses
    {
        public int Prop1 { get; set; }

        public class TestNestedInnerClass1
        {
            public int Prop2 { get; set; }

            public class TestNestedInnerClass2
            {
                public int Prop3 { get; set; }
            }
        }
    }
}
