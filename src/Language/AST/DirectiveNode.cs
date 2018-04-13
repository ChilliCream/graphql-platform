using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class DirectiveNode
        : ISyntaxNode
    {
        public DirectiveNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<ArgumentNode> arguments)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (arguments == null)
            {
                throw new System.ArgumentNullException(nameof(arguments));
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