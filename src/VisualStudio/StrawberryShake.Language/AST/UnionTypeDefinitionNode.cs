using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class UnionTypeDefinitionNode
        : UnionTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public UnionTypeDefinitionNode(
            Location? location,
            NameNode name,
            StringValueNode? description,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<NamedTypeNode> types)
            : base(location, name, directives, types)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.UnionTypeDefinition;

        public StringValueNode? Description { get; }

        public UnionTypeDefinitionNode WithLocation(Location? location)
        {
            return new UnionTypeDefinitionNode(
                location, Name, Description,
                Directives, Types);
        }

        public UnionTypeDefinitionNode WithName(NameNode name)
        {
            return new UnionTypeDefinitionNode(
                Location, name, Description,
                Directives, Types);
        }

        public UnionTypeDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new UnionTypeDefinitionNode(
                Location, Name, description,
                Directives, Types);
        }

        public UnionTypeDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new UnionTypeDefinitionNode(
                Location, Name, Description,
                directives, Types);
        }

        public UnionTypeDefinitionNode WithTypes(
            IReadOnlyList<NamedTypeNode> types)
        {
            return new UnionTypeDefinitionNode(
                Location, Name, Description,
                Directives, types);
        }
    }
}
