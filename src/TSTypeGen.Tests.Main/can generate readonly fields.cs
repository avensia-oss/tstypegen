using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestGenerateFieldsClass1
    {
        public int NotAConstant1 = 1;
        public string NotAConstant2 = "string";

        public const int Prop1 = 1;
        public const long Prop2 = 2;
        public const double Prop3 = 3.3;
        public const string Prop4 = "prop4";
    }
}
