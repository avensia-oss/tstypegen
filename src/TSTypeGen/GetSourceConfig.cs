using System.Collections.Immutable;

namespace TSTypeGen
{
    public class GetSourceConfig
    {
        public string RootPath { get; set; }
        public ImmutableDictionary<string, string> PathAliases { get; }
        public bool UseConstEnums { get; set; }
        public bool UseOptionalForNullables { get; set; }

        public GetSourceConfig(string rootPath, ImmutableDictionary<string, string> pathAliases, bool useConstEnums, bool useOptionalForNullables)
        {
            RootPath = rootPath;
            PathAliases = pathAliases;
            UseConstEnums = useConstEnums;
            UseOptionalForNullables = useOptionalForNullables;
        }
    }
}