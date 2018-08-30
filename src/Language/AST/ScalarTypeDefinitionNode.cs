using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ScalarTypeDefinitionNode
        : ScalarTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public ScalarTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<DirectiveNode> directives)
            : base(location, name, directives)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.ScalarTypeDefinition;
        public StringValueNode Description { get; }
    }
}
