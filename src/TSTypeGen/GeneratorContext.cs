using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TSTypeGen
{
    public class GeneratorContext
    {
        public IReadOnlyList<Assembly> Assemblies { get; }
        public IReadOnlyList<Type> AllTypes => new List<Type>(Assemblies.SelectMany(t => t.GetTypes()));

        private IDictionary<Assembly, AssemblyXmlComments> AssemblyComments { get; }

        public GeneratorContext(IReadOnlyList<(Assembly Assembly, string XmlCommentFile)> assemblies)
        {
            Assemblies = assemblies.Select(t => t.Assembly).ToList();

            AssemblyComments = new Dictionary<Assembly, AssemblyXmlComments>();
            foreach (var (assembly, xmlCommentFile) in assemblies)
            {
                if (xmlCommentFile != null)
                {
                    AssemblyComments.Add(assembly, new AssemblyXmlComments(xmlCommentFile));
                }
            }
        }

        public List<string> GetTypeScriptComment(Type type)
        {
            if (!AssemblyComments.ContainsKey(type.Assembly))
                return null;

            return AssemblyComments[type.Assembly].GetTypeScriptComment(type);
        }

        public List<string> GetTypeScriptComment(MemberInfo memberInfo)
        {
            if (!AssemblyComments.ContainsKey(memberInfo.DeclaringType.Assembly))
                return null;

            return AssemblyComments[memberInfo.DeclaringType.Assembly].GetTypeScriptComment(memberInfo);
        }

        public IReadOnlyList<Type> FindDerivedTypes(Type type)
        {
            return AllTypes.Where(t =>
            {
                try
                {
                    return type.IsAssignableFrom(t);
                }
                catch
                {
                    return false;
                }
            }).ToList();
        }
    }
}
