using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class InterfaceTypeExtensionNode
        : InterfaceTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public InterfaceTypeExtensionNode(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<FieldDefinitionNode> fields)
            : base(location, name, directives, fields)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.InterfaceTypeExtension;

        public InterfaceTypeExtensionNode WithLocation(Location? location)
        {
            return new InterfaceTypeExtensionNode(
                location, Name, Directives, Fields);
        }

        public InterfaceTypeExtensionNode WithName(NameNode name)
        {
            return new InterfaceTypeExtensionNode(
                Location, name, Directives, Fields);
        }

        public InterfaceTypeExtensionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new InterfaceTypeExtensionNode(
                Location, Name, directives, Fields);
        }

        public InterfaceTypeExtensionNode WithFields(
            IReadOnlyList<FieldDefinitionNode> fields)
        {
            return new InterfaceTypeExtensionNode(
                Location, Name, Directives, fields);
        }
    }
}
