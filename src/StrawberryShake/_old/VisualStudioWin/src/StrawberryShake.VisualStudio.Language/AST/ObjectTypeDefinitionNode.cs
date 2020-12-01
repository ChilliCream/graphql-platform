using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public sealed class ObjectTypeDefinitionNode
        : ObjectTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public ObjectTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode? description,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<NamedTypeNode> interfaces,
            IReadOnlyList<FieldDefinitionNode> fields)
            : base(location, name, directives, interfaces, fields)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.ObjectTypeDefinition;

        public StringValueNode? Description { get; }

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            if (Description is { })
            {
                yield return Description;
            }

            yield return Name;

            foreach (NamedTypeNode interfaceName in Interfaces)
            {
                yield return interfaceName;
            }

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            foreach (FieldDefinitionNode field in Fields)
            {
                yield return field;
            }
        }

        public ObjectTypeDefinitionNode WithLocation(Location location)
        {
            return new ObjectTypeDefinitionNode(
                location, Name, Description,
                Directives, Interfaces, Fields);
        }

        public ObjectTypeDefinitionNode WithName(NameNode name)
        {
            return new ObjectTypeDefinitionNode(
                Location, name, Description,
                Directives, Interfaces, Fields);
        }

        public ObjectTypeDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new ObjectTypeDefinitionNode(
                Location, Name, description,
                Directives, Interfaces, Fields);
        }

        public ObjectTypeDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new ObjectTypeDefinitionNode(
                Location, Name, Description,
                directives, Interfaces, Fields);
        }

        public ObjectTypeDefinitionNode WithInterfaces(
            IReadOnlyList<NamedTypeNode> interfaces)
        {
            return new ObjectTypeDefinitionNode(
                Location, Name, Description,
                Directives, interfaces, Fields);
        }

        public ObjectTypeDefinitionNode WithFields(
            IReadOnlyList<FieldDefinitionNode> fields)
        {
            return new ObjectTypeDefinitionNode(
                Location, Name, Description,
                Directives, Interfaces, fields);
        }
    }
}
