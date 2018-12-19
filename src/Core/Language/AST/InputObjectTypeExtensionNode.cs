using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class InputObjectTypeExtensionNode
        : InputObjectTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public InputObjectTypeExtensionNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<InputValueDefinitionNode> fields)
            : base(location, name, directives, fields)
        {
        }

        public override NodeKind Kind { get; } =
            NodeKind.InputObjectTypeExtension;

        public InputObjectTypeExtensionNode WithLocation(Location location)
        {
            return new InputObjectTypeExtensionNode(
                location, Name, Directives, Fields);
        }

        public InputObjectTypeExtensionNode WithName(NameNode name)
        {
            return new InputObjectTypeExtensionNode(
                Location, name, Directives, Fields);
        }

        public InputObjectTypeExtensionNode WithDirectives(
            IReadOnlyCollection<DirectiveNode> directives)
        {
            return new InputObjectTypeExtensionNode(
                Location, Name, directives, Fields);
        }

        public InputObjectTypeExtensionNode WithFields(
            IReadOnlyCollection<InputValueDefinitionNode> fields)
        {
            return new InputObjectTypeExtensionNode(
                Location, Name, Directives, fields);
        }
    }
}
