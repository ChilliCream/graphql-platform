using System.Collections.Generic;

namespace Prometheus.Language
{
    public class EnumValueDefinitionNode
        : ISyntaxNode
    {
        public EnumValueDefinitionNode(
            Location location, NameNode name, 
            StringValueNode description, 
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
            Description = description;
            Directives = directives;
        }

        public NodeKind Kind { get; } = NodeKind.EnumValueDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}