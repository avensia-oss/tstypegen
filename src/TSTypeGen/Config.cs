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
        public string CustomTypeScriptIgnoreAttributeFullName { get; set; }

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