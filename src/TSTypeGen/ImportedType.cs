namespace TSTypeGen
{
    public class ImportedType
    {
        public string FilePath { get; }
        public string ImportName { get; }

        public ImportedType(string filePath, string importName)
        {
            FilePath = filePath;
            ImportName = importName;
        }

        protected bool Equals(ImportedType other)
        {
            return FilePath == other.FilePath && ImportName == other.ImportName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ImportedType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((FilePath != null ? FilePath.GetHashCode() : 0) * 397) ^ (ImportName != null ? ImportName.GetHashCode() : 0);
            }
        }
    }
}