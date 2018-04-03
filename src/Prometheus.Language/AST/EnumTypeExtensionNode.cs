using System.Collections.Generic;

namespace Prometheus.Language
{
    public class EnumTypeExtensionNode
        : ITypeExtensionNode
    {
        public EnumTypeExtensionNode(
            Location location, NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<EnumValueDefinitionNode> values)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (directives == null)
            {
                throw new System.ArgumentNullException(nameof(directives));
            }

            if (values == null)
            {
                throw new System.ArgumentNullException(nameof(values));
            }

            Location = location;
            Name = name;
            Directives = directives;
            Values = values;
        }

        public NodeKind Kind { get; } = NodeKind.EnumTypeExtension;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<EnumValueDefinitionNode> Values { get; }
    }
}