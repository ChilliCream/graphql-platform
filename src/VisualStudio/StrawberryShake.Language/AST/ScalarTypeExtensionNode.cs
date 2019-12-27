using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ScalarTypeExtensionNode
        : ScalarTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public ScalarTypeExtensionNode(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives)
            : base(location, name, directives)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.ScalarTypeExtension;

        public ScalarTypeExtensionNode WithLocation(Location? location)
        {
            return new ScalarTypeExtensionNode(
                location, Name, Directives);
        }

        public ScalarTypeExtensionNode WithName(NameNode name)
        {
            return new ScalarTypeExtensionNode(
                Location, name, Directives);
        }

        public ScalarTypeExtensionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new ScalarTypeExtensionNode(
                Location, Name, directives);
        }
    }
}
