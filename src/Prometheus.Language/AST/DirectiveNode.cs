using System.Collections.Generic;

namespace Prometheus.Language
{
    public class DirectiveNode
        : ISyntaxNode
    {
        public DirectiveNode(Location location, NameNode name,
            IReadOnlyCollection<ArgumentNode> arguments)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (Arguments == null)
            {
                throw new System.ArgumentNullException(nameof(Arguments));
            }

            Location = location;
            Name = name;
            Arguments = arguments;
        }

        public NodeKind Kind { get; } = NodeKind.Directive;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<ArgumentNode> Arguments { get; }
    }
}