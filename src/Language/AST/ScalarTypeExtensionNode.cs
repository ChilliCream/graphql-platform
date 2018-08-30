using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ScalarTypeExtensionNode
        : ScalarTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public ScalarTypeExtensionNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives)
            : base(location, name, directives)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.ScalarTypeExtension;
    }
}
