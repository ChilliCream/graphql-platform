using System.Collections.Generic;

namespace Prometheus.Language
{
    public class InputValueDefinitionNode
        : ISyntaxNode
    {
        public InputValueDefinitionNode(Location location, 
            NameNode name, StringValueNode description, 
            ITypeNode type, IValueNode value, 
            IReadOnlyCollection<DirectiveNode> directives)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (type == null)
            {
                throw new System.ArgumentNullException(nameof(type));
            }

            if (directives == null)
            {
                throw new System.ArgumentNullException(nameof(directives));
            }

            Location = location;
            Name = name;
            Description = description;
            Type = type;
            Value = value;
            Directives = directives;
        }

        public NodeKind Kind { get; } = NodeKind.InputValueDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public ITypeNode Type { get; }
        public IValueNode Value { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}