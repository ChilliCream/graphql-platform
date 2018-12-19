using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class UnionTypeExtensionNode
        : UnionTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public UnionTypeExtensionNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<NamedTypeNode> types)
            : base(location, name, directives, types)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.UnionTypeExtension;

        public UnionTypeExtensionNode WithLocation(Location location)
        {
            return new UnionTypeExtensionNode(
                location, Name, Directives, Types);
        }

        public UnionTypeExtensionNode WithName(NameNode name)
        {
            return new UnionTypeExtensionNode(
                Location, name, Directives, Types);
        }

        public UnionTypeExtensionNode WithDirectives(
            IReadOnlyCollection<DirectiveNode> directives)
        {
            return new UnionTypeExtensionNode(
                Location, Name, directives, Types);
        }

        public UnionTypeExtensionNode WithTypes(
            IReadOnlyCollection<NamedTypeNode> types)
        {
            return new UnionTypeExtensionNode(
                Location, Name, Directives, types);
        }
    }
}
