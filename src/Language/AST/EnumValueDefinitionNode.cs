using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class EnumValueDefinitionNode
        : NamedSyntaxNode
    {
        public EnumValueDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<DirectiveNode> directives)
            : base(location, name, directives)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.EnumValueDefinition;

        public StringValueNode Description { get; }
    }
}
