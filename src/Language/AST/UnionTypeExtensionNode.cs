using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class UnionTypeExtensionNode
        : UnionTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public UnionTypeExtensionNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<NamedTypeNode> types)
            : base(location, name, directives, types)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.UnionTypeExtension;
    }
}
