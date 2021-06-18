using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestJsonPropertyClass
    {
        [JsonProperty]
        public string Prop1 { get; set; }
        [JsonProperty("RenamedProp2")]
        public string Prop2 { get; set; }
        [JsonProperty(PropertyName = "RenamedProp3")]
        public string Prop3 { get; set; }
        [JsonProperty("SpuriousIgnoredName", PropertyName = "RenamedProp4")]
        public string Prop4 { get; set; }
        [JsonProperty("0IsNotAValidJsIdentifier")]
        public string Prop5 { get; set; }
    }
}
