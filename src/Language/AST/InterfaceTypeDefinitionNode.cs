using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class InterfaceTypeDefinitionNode
        : InterfaceTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public InterfaceTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<FieldDefinitionNode> fields)
            : base(location, name, directives, fields)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.InterfaceTypeDefinition;
        public StringValueNode Description { get; }
    }
}
