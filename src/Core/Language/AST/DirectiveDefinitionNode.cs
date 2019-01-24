using System;
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
            bool isRepeatable,
            IReadOnlyList<InputValueDefinitionNode> arguments,
            IReadOnlyList<NameNode> locations)
        {
            Location = location;
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            IsRepeatable = isRepeatable;
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
            Locations = locations
                ?? throw new ArgumentNullException(nameof(locations));
        }

        public NodeKind Kind { get; } = NodeKind.DirectiveDefinition;

        public Location Location { get; }

        public NameNode Name { get; }

        public StringValueNode Description { get; }

        public bool IsRepeatable { get; }

        public IReadOnlyList<InputValueDefinitionNode> Arguments { get; }

        public IReadOnlyList<NameNode> Locations { get; }
    }
}
