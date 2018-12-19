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
            Location = location;
            Name = name 
                ?? throw new System.ArgumentNullException(nameof(name));
            Description = description;
            Arguments = arguments 
                ?? throw new System.ArgumentNullException(nameof(arguments));
            Locations = locations 
                ?? throw new System.ArgumentNullException(nameof(locations));
        }

        public NodeKind Kind { get; } = NodeKind.DirectiveDefinition;

        public Location Location { get; }

        public NameNode Name { get; }

        public StringValueNode Description { get; }

        public IReadOnlyCollection<InputValueDefinitionNode> Arguments { get; }

        public IReadOnlyCollection<NameNode> Locations { get; }
    }
}
