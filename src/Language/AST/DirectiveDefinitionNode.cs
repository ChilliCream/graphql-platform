using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class DirectiveDefinitionNode
        : ITypeSystemDefinitionNode
    {
        public DirectiveDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<InputValueDefinitionNode> arguments,
            IReadOnlyCollection<NameNode> locations)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (arguments == null)
            {
                throw new System.ArgumentNullException(nameof(arguments));
            }

            if (locations == null)
            {
                throw new System.ArgumentNullException(nameof(locations));
            }

            Location = location;
            Name = name;
            Description = description;
            Arguments = arguments;
            Locations = locations;
        }

        public NodeKind Kind { get; } = NodeKind.DirectiveDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<InputValueDefinitionNode> Arguments { get; }
        public IReadOnlyCollection<NameNode> Locations { get; }
    }
}