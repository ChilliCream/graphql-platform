using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class EnumTypeExtensionNode
        : EnumTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public EnumTypeExtensionNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<EnumValueDefinitionNode> values)
            : base(location, name, directives, values)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.EnumTypeExtension;
    }
}
