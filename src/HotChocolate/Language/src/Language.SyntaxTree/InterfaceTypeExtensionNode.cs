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
            IReadOnlyList<NamedTypeNode> interfaces,
            IReadOnlyList<FieldDefinitionNode> fields)
            : base(location, name, directives, interfaces, fields)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.InterfaceTypeExtension;

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            foreach (FieldDefinitionNode field in Fields)
            {
                yield return field;
            }
        }

        public InterfaceTypeExtensionNode WithLocation(Location? location)
        {
            return new InterfaceTypeExtensionNode(
                location, Name, Directives, Interfaces, Fields);
        }

        public InterfaceTypeExtensionNode WithName(NameNode name)
        {
            return new InterfaceTypeExtensionNode(
                Location, name, Directives, Interfaces, Fields);
        }

        public InterfaceTypeExtensionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new InterfaceTypeExtensionNode(
                Location, Name, directives, Interfaces, Fields);
        }

        public InterfaceTypeExtensionNode WithFields(
            IReadOnlyList<FieldDefinitionNode> fields)
        {
            return new InterfaceTypeExtensionNode(
                Location, Name, Directives, Interfaces, fields);
        }

        public InterfaceTypeExtensionNode WithInterfaces(
            IReadOnlyList<NamedTypeNode> interfaces)
        {
            return new InterfaceTypeExtensionNode(
                Location, Name, Directives, interfaces, Fields);
        }
    }
}
