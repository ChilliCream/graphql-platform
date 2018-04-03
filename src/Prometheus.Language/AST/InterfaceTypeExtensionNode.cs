using System.Collections.Generic;

namespace Prometheus.Language
{
    public class InterfaceTypeExtensionNode
        : ITypeExtensionNode
    {
        public InterfaceTypeExtensionNode(
            Location location, NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<FieldDefinitionNode> fields)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (directives == null)
            {
                throw new System.ArgumentNullException(nameof(directives));
            }

            if (fields == null)
            {
                throw new System.ArgumentNullException(nameof(fields));
            }

            Location = location;
            Name = name;
            Directives = directives;
            Fields = fields;
        }

        public NodeKind Kind { get; } = NodeKind.InterfaceTypeExtension;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<FieldDefinitionNode> Fields { get; }
    }
}