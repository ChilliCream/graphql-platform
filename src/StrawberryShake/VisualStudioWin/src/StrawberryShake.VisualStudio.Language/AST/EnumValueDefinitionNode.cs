using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public sealed class EnumValueDefinitionNode
        : NamedSyntaxNode
    {
        public EnumValueDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode? description,
            IReadOnlyList<DirectiveNode> directives)
            : base(location, name, directives)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.EnumValueDefinition;

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
        }

        public EnumValueDefinitionNode WithLocation(Location location)
        {
            return new EnumValueDefinitionNode(
                location, Name, Description, Directives);
        }

        public EnumValueDefinitionNode WithName(NameNode name)
        {
            return new EnumValueDefinitionNode(
                Location, name, Description, Directives);
        }

        public EnumValueDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new EnumValueDefinitionNode(
                Location, Name, description, Directives);
        }

        public EnumValueDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new EnumValueDefinitionNode(
                Location, Name, Description, directives);
        }
    }
}
