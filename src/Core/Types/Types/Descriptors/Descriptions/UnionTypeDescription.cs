using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    internal class UnionTypeDescription
        : TypeDescriptionBase<UnionTypeDefinitionNode>
    {
        public ResolveAbstractType ResolveAbstractType { get; set; }

        public ICollection<TypeReference> Types { get; } =
            new List<TypeReference>();
    }
}
