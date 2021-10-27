using System.Reflection;

namespace TSTypeGen
{
    public class TsInterfaceMember
    {
        public bool IsOptional { get; set; }
        public string Name { get; }
        public TsTypeReference Type { get; }
        public MemberInfo MemberInfo { get; }

        public TsInterfaceMember(string name, TsTypeReference type, MemberInfo memberInfo, bool isOptional)
        {
            Name = name;
            Type = type;
            MemberInfo = memberInfo;
            IsOptional = isOptional;
        }
    }
}