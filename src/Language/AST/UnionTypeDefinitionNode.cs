using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class UnionTypeDefinitionNode
        : UnionTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public UnionTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<NamedTypeNode> types)
            : base(location, name, directives, types)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.UnionTypeDefinition;
        public StringValueNode Description { get; }
    }
}
