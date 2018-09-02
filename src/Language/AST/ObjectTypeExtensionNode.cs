using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ObjectTypeExtensionNode
        : ObjectTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public ObjectTypeExtensionNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<NamedTypeNode> interfaces,
            IReadOnlyCollection<FieldDefinitionNode> fields)
            : base(location, name, directives, interfaces, fields)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.ObjectTypeExtension;
    }
}
