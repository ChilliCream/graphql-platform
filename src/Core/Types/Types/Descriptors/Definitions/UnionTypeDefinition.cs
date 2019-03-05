using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    internal class UnionTypeDefinition
        : TypeDefinitionBase<UnionTypeDefinitionNode>
    {
        public ResolveAbstractType ResolveAbstractType { get; set; }

        public ICollection<TypeReference> Types { get; } =
            new List<TypeReference>();
    }
}
