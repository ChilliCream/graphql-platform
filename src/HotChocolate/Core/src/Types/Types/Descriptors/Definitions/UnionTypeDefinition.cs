using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class UnionTypeDefinition
        : TypeDefinitionBase<UnionTypeDefinitionNode>
    {
        public ResolveAbstractType ResolveAbstractType { get; set; }

        public ICollection<ITypeReference> Types { get; } =
            new List<ITypeReference>();
    }
}
