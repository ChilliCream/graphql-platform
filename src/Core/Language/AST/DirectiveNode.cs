using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class DirectiveNode
        : ISyntaxNode
    {
        public DirectiveNode(
            string name,
            params ArgumentNode[] arguments)
            : this(new NameNode(name), arguments)
        {
        }

        public DirectiveNode(
            string name,
            IReadOnlyList<ArgumentNode> arguments)
            : this(new NameNode(name), arguments)
        {
        }

        public DirectiveNode(
            NameNode name,
            IReadOnlyList<ArgumentNode> arguments)
            : this(null, name, arguments)
        {
        }

        public DirectiveNode(
            Location? location,
            NameNode name,
            IReadOnlyList<ArgumentNode> arguments)
        {
            Location = location;
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
        }

        public NodeKind Kind { get; } = NodeKind.Directive;

        public Location? Location { get; }

        public NameNode Name { get; }

        public IReadOnlyList<ArgumentNode> Arguments { get; }

        public DirectiveNode WithLocation(Location? location)
        {
            return new DirectiveNode(location, Name, Arguments);
        }

        public DirectiveNode WithName(NameNode name)
        {
            return new DirectiveNode(Location, name, Arguments);
        }

        public DirectiveNode WithArguments(
            IReadOnlyList<ArgumentNode> arguments)
        {
            return new DirectiveNode(Location, Name, arguments);
        }
    }
}
