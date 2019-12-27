using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class InterfaceTypeDefinitionNode
        : InterfaceTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public InterfaceTypeDefinitionNode(
            Location? location,
            NameNode name,
            StringValueNode? description,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<FieldDefinitionNode> fields)
            : base(location, name, directives, fields)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.InterfaceTypeDefinition;

        public StringValueNode? Description { get; }

        public InterfaceTypeDefinitionNode WithLocation(Location? location)
        {
            return new InterfaceTypeDefinitionNode(
                location, Name, Description,
                Directives, Fields);
        }

        public InterfaceTypeDefinitionNode WithName(NameNode name)
        {
            return new InterfaceTypeDefinitionNode(
                Location, name, Description,
                Directives, Fields);
        }

        public InterfaceTypeDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new InterfaceTypeDefinitionNode(
                Location, Name, description,
                Directives, Fields);
        }

        public InterfaceTypeDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new InterfaceTypeDefinitionNode(
                Location, Name, Description,
                directives, Fields);
        }

        public InterfaceTypeDefinitionNode WithFields(
            IReadOnlyList<FieldDefinitionNode> fields)
        {
            return new InterfaceTypeDefinitionNode(
                Location, Name, Description,
                Directives, fields);
        }
    }
}
