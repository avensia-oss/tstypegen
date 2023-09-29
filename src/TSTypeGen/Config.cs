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
        public string OutputPath { get; set; }
        public List<string> DllPatterns { get; set; }
        public Dictionary<string, string> TypeMappings { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> PropertyWrappers { get; set;  } = new Dictionary<string, string>();
        public string CustomTypeScriptIgnoreAttributeFullName { get; set; }
        // Babel doesn't support ambient const enums but there's one wierd trick where if you wrap the const enum in a template
        // string then const enums becomes literal string unions instead. E.g. you'll have to do this when const enums are not supported:
        // const x: TheEnum = 'value';
        // Instead of:
        // const x: TheEnum = TheEnum.Value;
        // This makes Typescipt a bit sad, unless the enum is converted into a template literal type:
        // const x: `${TheEnum}` = 'value';
        // Then both Babel and Typscript will be satisfied
        public bool WrapConstEnumsInTemplateStrings { get; set; }
        public bool UseEmbeddedDeclarations { get; set; }
        public bool UseConstEnums { get; set; } = true;
        public bool GenerateComments { get; set; } = true;
        public bool GenerateInterfaceProperties { get; set; } = true;
        public List<string> PackagesDirectories { get; set; }
        public string TargetFrameworkVersion { get; set; }

        public static Config ReadFromFile(string path)
        {
            var result = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path), new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            if (string.IsNullOrEmpty(result.BasePath))
            {
                result.BasePath = Path.GetDirectoryName(path);
            }
            else
            {
                result.BasePath = Path.Combine(Path.GetDirectoryName(path), result.BasePath);
            }

            if (!Path.IsPathRooted(result.BasePath))
            {
                result.BasePath = Path.Combine(Directory.GetCurrentDirectory(), result.BasePath);
            }

            if (string.IsNullOrEmpty(result.OutputPath))
            {
                result.OutputPath = result.BasePath;
            }

            if (!Path.IsPathRooted(result.OutputPath))
            {
                result.OutputPath = Path.Join(result.BasePath, result.OutputPath);
            }

            if (string.IsNullOrEmpty(result.NewLine))
            {
                result.NewLine = Environment.NewLine;
            }

            return result;
        }
    }
}