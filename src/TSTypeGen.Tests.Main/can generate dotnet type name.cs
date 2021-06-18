using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateDotNetTypeNamesAsJsDocComment]
    public class TestDotNetNames
    {
        [GenerateTypeScriptDefinition]
        public class TestDotNetNameClass1
        {

        }

        [GenerateTypeScriptDefinition]
        public class TestDotNetNameClass2
        {

        }

        [GenerateTypeScriptDefinition]
        [GenerateTypeScriptDotNetName(typeof(TestDotNetNameClass1))]
        public class TestDotNetNameClass3
        {

        }
    }
}
