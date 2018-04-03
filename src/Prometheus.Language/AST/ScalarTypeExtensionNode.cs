using System.Collections.Generic;

namespace Prometheus.Language
{
    public class ScalarTypeExtensionNode
        : ITypeExtensionNode
    {
        public ScalarTypeExtensionNode(
            Location location, NameNode name,
            IReadOnlyCollection<DirectiveNode> directives)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (directives == null)
            {
                throw new System.ArgumentNullException(nameof(directives));
            }

            Location = location;
            Name = name;
            Directives = directives;
        }

        public NodeKind Kind { get; } = NodeKind.ScalarTypeExtension;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}