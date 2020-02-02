using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public sealed class UnionTypeExtensionNode
        : UnionTypeDefinitionNodeBase
        , ITypeExtensionNode
    {
        public UnionTypeExtensionNode(
            Location location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<NamedTypeNode> types)
            : base(location, name, directives, types)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.UnionTypeExtension;

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            foreach (NamedTypeNode type in Types)
            {
                yield return type;
            }
        }

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
            IReadOnlyList<DirectiveNode> directives)
        {
            return new UnionTypeExtensionNode(
                Location, Name, directives, Types);
        }

        public UnionTypeExtensionNode WithTypes(
            IReadOnlyList<NamedTypeNode> types)
        {
            return new UnionTypeExtensionNode(
                Location, Name, Directives, types);
        }
    }
}
