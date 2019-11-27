using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ScalarTypeDefinitionNode
        : ScalarTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public ScalarTypeDefinitionNode(
            Location? location,
            NameNode name,
            StringValueNode? description,
            IReadOnlyList<DirectiveNode> directives)
            : base(location, name, directives)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.ScalarTypeDefinition;

        public StringValueNode? Description { get; }

        public ScalarTypeDefinitionNode WithLocation(Location? location)
        {
            return new ScalarTypeDefinitionNode(
                location, Name, Description,
                Directives);
        }

        public ScalarTypeDefinitionNode WithName(NameNode name)
        {
            return new ScalarTypeDefinitionNode(
                Location, name, Description,
                Directives);
        }

        public ScalarTypeDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new ScalarTypeDefinitionNode(
                Location, Name, description,
                Directives);
        }

        public ScalarTypeDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new ScalarTypeDefinitionNode(
                Location, Name, Description,
                directives);
        }
    }
}
