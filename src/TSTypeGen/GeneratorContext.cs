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
        public IReadOnlyList<Type> AllTypes => new List<Type>(Assemblies.SelectMany(a => a.GetTypes()));

        public GeneratorContext(IReadOnlyList<Assembly> assemblies)
        {
            Assemblies = assemblies;
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
