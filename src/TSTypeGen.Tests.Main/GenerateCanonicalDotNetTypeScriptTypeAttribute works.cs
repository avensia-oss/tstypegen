using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateDotNetTypeNamesAsJsDocComment]
    public class TestCanonicalDotNetTypeWrapper
    {
        [GenerateTypeScriptDefinition]
        [GenerateCanonicalDotNetTypeScriptType]
        public interface TestCanonicalDotNetTypeInterface1
        {
            int Prop1 { get; set; }
        }

        [GenerateTypeScriptDefinition]
        public class TestCanonicalDotNetTypeClass1 : TestCanonicalDotNetTypeInterface1
        {
            public int Prop1 { get; set; }
        }

        [GenerateTypeScriptDefinition]
        [GenerateCanonicalDotNetTypeScriptType]
        public class TestCanonicalDotNetTypeBaseClass2
        {
            public int Prop1 { get; set; }
        }

        [GenerateTypeScriptDefinition]
        public class TestCanonicalDotNetTypeClass2 : TestCanonicalDotNetTypeBaseClass2
        {
            public int Prop2 { get; set; }
        }

        [GenerateTypeScriptDefinition]
        [GenerateCanonicalDotNetTypeScriptType]
        public interface TestCanonicalDotNetTypeInterface3
        {
            int Prop1 { get; set; }
        }

        [GenerateTypeScriptDefinition]
        public class TestCanonicalDotNetTypeBaseClass3 : TestCanonicalDotNetTypeInterface3
        {
            public int Prop1 { get; set; }
        }

        [GenerateTypeScriptDefinition]
        public class TestCanonicalDotNetTypeClass3 : TestCanonicalDotNetTypeBaseClass3
        {
            public int Prop2 { get; set; }
        }
    }
}
