namespace TSTypeGen {
    public class ImportedModule
    {
        public string DefaultVariableName { get; }
        public string SourceFile { get; }
        public string NamedImportName { get; }

        private ImportedModule(string defaultVariableName, string sourceFileWithExtension, string namedImportName)
        {
            DefaultVariableName = defaultVariableName;
            SourceFile = sourceFileWithExtension;
            NamedImportName = namedImportName;
        }

        public static ImportedModule DefaultImport(string defaultVariableName, string sourceFileWithExtension) => new ImportedModule(defaultVariableName, sourceFileWithExtension, null);
        public static ImportedModule NamedImport(string defaultVariableName, string sourceFileWithExtension, string namedImportName) => new ImportedModule(defaultVariableName, sourceFileWithExtension, namedImportName);
    }
}