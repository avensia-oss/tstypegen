using System.Collections.Immutable;

namespace TSTypeGen
{
    public class DerivedTypesUnionGeneration
    {
        public ImmutableArray<TsTypeReference> DerivedTypeReferences { get; }
        public string DerivedTypesUnionName { get; }

        public DerivedTypesUnionGeneration(ImmutableArray<TsTypeReference> derivedTypeReferences, string derivedTypesUnionName)
        {
            DerivedTypeReferences = derivedTypeReferences;
            DerivedTypesUnionName = derivedTypesUnionName;
        }
    }
}
