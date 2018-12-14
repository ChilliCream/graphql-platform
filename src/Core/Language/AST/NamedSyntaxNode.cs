using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class NamedSyntaxNode
        : ISyntaxNode
        , IHasDirectives
    {
        protected NamedSyntaxNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives)
        {
            Location = location;
            Name = name 
                ?? throw new ArgumentNullException(nameof(name));
            Directives = directives 
                ?? throw new ArgumentNullException(nameof(directives));
        }

        public abstract NodeKind Kind { get; }

        public Location Location { get; }

        public NameNode Name { get; }

        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}
