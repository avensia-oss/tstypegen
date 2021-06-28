using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public interface TestJsonIgnoreInterface
    {
        [JsonIgnore] public string InterfaceProp1 { get; set; }
        [TypeScriptIgnore] public string InterfaceProp2 { get; set; }
        [CustomTypeScriptIgnore] public string InterfaceProp3 { get; set; }
        public string InterfaceProp4 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestJsonIgnoreClass : TestJsonIgnoreInterface
    {
        public string InterfaceProp1 { get; set; }
        public string InterfaceProp2 { get; set; }
        public string InterfaceProp3 { get; set; }
        public string InterfaceProp4 { get; set; }
        public string Prop1 { get; set; }
        [JsonIgnore] public string Prop2 { get; set; }
        [TypeScriptIgnore] public string Prop3 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public abstract class TestJsonIgnoreBaseClass
    {
        [JsonIgnore]
        public abstract string IgnoreMe { get; }
    }

    [GenerateTypeScriptDefinition]
    public class TestJsonIgnoreChildClass : TestJsonIgnoreBaseClass
    {
        public override string IgnoreMe => "ignore me";
    }
}
