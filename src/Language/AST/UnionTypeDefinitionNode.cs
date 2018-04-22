using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class UnionTypeDefinitionNode
        : ITypeDefinitionNode
    {
        public UnionTypeDefinitionNode(
            Location location,
            NameNode name,
            StringValueNode description,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<NamedTypeNode> types)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            Location = location;
            Name = name;
            Description = description;
            Directives = directives;
            Types = types;
        }

        public NodeKind Kind { get; } = NodeKind.UnionTypeDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<NamedTypeNode> Types { get; }
    }
}