using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestDisableInnerClass1
    {
        public int Prop1 { get; set; }

        [GenerateTypeScriptDefinition(false)]
        public class TestDisableInner_ShouldNotBeGeneratedInnerClass11
        {
            public int Prop2 { get; set; }
        }

        [GenerateTypeScriptDefinition(false)]
        public class TestDisableInner_ShouldNotBeGeneratedInnerClass12
        {
            public int Prop2 { get; set; }

            public class InnerClass13
            {
                public int Prop2 { get; set; }
            }
        }

        [GenerateTypeScriptDefinition(false)]
        public class TestDisableInner_ShouldNotBeGeneratedInnerClass14
        {
            public int Prop2 { get; set; }

            [GenerateTypeScriptDefinition]
            public class InnerClass15
            {
                public int Prop2 { get; set; }
            }
        }
    }
}
