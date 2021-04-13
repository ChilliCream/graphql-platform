using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public sealed class InputObjectTypeDefinitionNode
        : InputObjectTypeDefinitionNodeBase
        , ITypeDefinitionNode
    {
        public InputObjectTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode? description,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<InputValueDefinitionNode> fields)
            : base(location, name, directives, fields)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } =
            NodeKind.InputObjectTypeDefinition;

        public StringValueNode? Description { get; }

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            if (Description is { })
            {
                yield return Description;
            }

            yield return Name;

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            foreach (InputValueDefinitionNode field in Fields)
            {
                yield return field;
            }
        }

        public InputObjectTypeDefinitionNode WithLocation(Location location)
        {
            return new InputObjectTypeDefinitionNode(
                location, Name, Description,
                Directives, Fields);
        }

        public InputObjectTypeDefinitionNode WithName(NameNode name)
        {
            return new InputObjectTypeDefinitionNode(
                Location, name, Description,
                Directives, Fields);
        }

        public InputObjectTypeDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new InputObjectTypeDefinitionNode(
                Location, Name, description,
                Directives, Fields);
        }

        public InputObjectTypeDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new InputObjectTypeDefinitionNode(
                Location, Name, Description,
                directives, Fields);
        }

        public InputObjectTypeDefinitionNode WithFields(
            IReadOnlyList<InputValueDefinitionNode> fields)
        {
            return new InputObjectTypeDefinitionNode(
                Location, Name, Description,
                Directives, fields);
        }
    }
}
