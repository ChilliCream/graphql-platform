using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class UnionTypeDescription
        : TypeDescriptionBase
    {
        public UnionTypeDefinitionNode SyntaxNode { get; set; }

        public List<TypeReference> Types { get; set; } = new List<TypeReference>();

        public ResolveAbstractType ResolveAbstractType { get; set; }
    }
}
