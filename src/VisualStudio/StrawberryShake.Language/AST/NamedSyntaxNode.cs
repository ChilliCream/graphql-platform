using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class NamedSyntaxNode
        : INamedSyntaxNode
    {
        protected NamedSyntaxNode(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives)
        {
            Location = location;
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Directives = directives
                ?? throw new ArgumentNullException(nameof(directives));
        }

        public abstract NodeKind Kind { get; }

        public Location? Location { get; }

        public NameNode Name { get; }

        public IReadOnlyList<DirectiveNode> Directives { get; }
    }
}
