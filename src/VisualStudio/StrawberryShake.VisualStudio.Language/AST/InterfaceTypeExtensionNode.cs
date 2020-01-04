using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public sealed class InterfaceTypeExtensionNode
        : InterfaceTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public InterfaceTypeExtensionNode(
            Location location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<FieldDefinitionNode> fields)
            : base(location, name, directives, fields)
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

        public InterfaceTypeExtensionNode WithLocation(Location location)
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
