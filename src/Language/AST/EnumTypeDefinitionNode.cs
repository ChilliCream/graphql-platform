using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class EnumTypeDefinitionNode
        : EnumTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public EnumTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<EnumValueDefinitionNode> values)
            : base(location, name, directives, values)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.EnumTypeDefinition;
        public StringValueNode Description { get; }
    }
}
