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
    }
}
