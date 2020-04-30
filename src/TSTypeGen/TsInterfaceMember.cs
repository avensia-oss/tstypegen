namespace TSTypeGen
{
    public class TsInterfaceMember
    {
        public bool IsOptional { get; set; }
        public string Name { get; }
        public TsTypeReference Type { get; }

        public TsInterfaceMember(string name, TsTypeReference type, bool isOptional)
        {
            Name = name;
            Type = type;
            IsOptional = isOptional;
        }
    }
}