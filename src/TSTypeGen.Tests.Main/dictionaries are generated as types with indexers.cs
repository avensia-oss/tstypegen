using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestDictionariesClass
    {
        public IDictionary<string, string> Prop1 { get; set; }
        public IDictionary<int, bool> Prop2 { get; set; }
        public IReadOnlyDictionary<string, string> Prop3 { get; set; }
        public IReadOnlyDictionary<int, bool> Prop4 { get; set; }
        public Dictionary<string, string> Prop5 { get; set; }
        public Dictionary<int, bool> Prop6 { get; set; }
    }
}
