using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TSTypeGen
{
    public class Config
    {
        public string NewLine { get; set; } = Environment.NewLine;
        public string BasePath { get; set; }
        public Dictionary<string, string> TypeMappings { get; } = new Dictionary<string, string>();
        public string PropertyTypeDefinitionFile { get; set; }
        public string PropertyTypeName { get; set; }
        public string StructuralSubsetOfInterfaceFullName { get; set; }
        public string CustomTypeScriptIgnoreAttributeFullName { get; set; }
        public List<string> TypesToWrapPropertiesFor { get; set; } = new List<string>();
        public Dictionary<string, string> PathAliases { get; } = new Dictionary<string, string>();
        public string RootPath { get; set; }
        public bool UseConstEnums { get; set; } = true;
        public bool UseOptionalForNullables { get; set; } = true;

        public static Config ReadFromFile(string path)
        {
            var result = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path), new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            if (string.IsNullOrEmpty(result.BasePath))
            {
                result.BasePath = Path.GetDirectoryName(path);
            }

            if (string.IsNullOrEmpty(result.NewLine))
            {
                result.NewLine = Environment.NewLine;
            }

            return result;
        }
    }
}