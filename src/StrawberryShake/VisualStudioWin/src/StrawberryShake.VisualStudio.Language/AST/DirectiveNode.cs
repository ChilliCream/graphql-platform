using System;
using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public sealed class DirectiveNode
        : ISyntaxNode
    {
        public DirectiveNode(
            Location location,
            NameNode name,
            IReadOnlyList<ArgumentNode> arguments)
        {
            Location = location;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public NodeKind Kind { get; } = NodeKind.Directive;

        public Location Location { get; }

        public NameNode Name { get; }

        public IReadOnlyList<ArgumentNode> Arguments { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;

            foreach (ArgumentNode argument in Arguments)
            {
                yield return argument;
            }
        }

        public DirectiveNode WithLocation(Location location)
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
