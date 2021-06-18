using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    public interface JsonPropertyInterfaceTest
    {
        [JsonProperty("InterfaceProp1")]
        public string Prop1 { get; set; }

        [JsonProperty("InterfaceProp2")]
        public string Prop2 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class JsonPropertyInterfaceTestClass : JsonPropertyInterfaceTest
    {
        public string Prop1 { get; set; }

        [JsonProperty("ClassProp2")]
        public string Prop2 { get; set; }
    }
}
