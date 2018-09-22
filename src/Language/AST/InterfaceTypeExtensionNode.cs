using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class InterfaceTypeExtensionNode
        : InterfaceTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public InterfaceTypeExtensionNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<FieldDefinitionNode> fields)
            : base(location, name, directives, fields)
        {
        }

        public override NodeKind Kind { get; } =
            NodeKind.InterfaceTypeExtension;
    }
}
