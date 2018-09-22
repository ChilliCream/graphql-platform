using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ObjectTypeDefinitionNode
        : ObjectTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public ObjectTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<NamedTypeNode> interfaces,
            IReadOnlyCollection<FieldDefinitionNode> fields)
            : base(location, name, directives, interfaces, fields)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.ObjectTypeDefinition;

        public StringValueNode Description { get; }
    }
}
