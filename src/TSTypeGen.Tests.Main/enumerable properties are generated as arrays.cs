using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestEnumerablePropertiesClass
    {
        public IEnumerable<string> Prop1 { get; set; }
        public ICollection<string> Prop2 { get; set; }
        public IList<string> Prop3 { get; set; }
        public List<string> Prop4 { get; set; }
        public string[] Prop5 { get; set; }
    }
}
