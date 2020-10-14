using System.Collections.Immutable;

namespace TSTypeGen
{
    public class GetSourceConfig
    {
        public string RootPath { get; set; }
        public string NewLine { get; set; }
        public ImmutableDictionary<string, string> PathAliases { get; }
        public bool UseConstEnums { get; set; }
        public bool UseOptionalForNullables { get; set; }

        public GetSourceConfig(string rootPath, string newLine, ImmutableDictionary<string, string> pathAliases, bool useConstEnums, bool useOptionalForNullables)
        {
            RootPath = rootPath;
            NewLine = newLine;
            PathAliases = pathAliases;
            UseConstEnums = useConstEnums;
            UseOptionalForNullables = useOptionalForNullables;
        }
    }
}