using System;
using System.Collections.Generic;

namespace Prometheus.Language
{
    public class UnionTypeExtensionNode
        : ITypeExtensionNode
    {
        public UnionTypeExtensionNode(
            Location location, NameNode name,
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
            Directives = directives;
            Types = types;
        }

        public NodeKind Kind { get; } = NodeKind.UnionTypeExtension;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<NamedTypeNode> Types { get; }
    }
}