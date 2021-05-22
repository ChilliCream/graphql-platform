using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class NamedSyntaxNode : INamedSyntaxNode
    {
        protected NamedSyntaxNode(
            Location? location,
            NameNode name,
            IReadOnlyList<DirectiveNode> directives)
        {
            Location = location;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Directives = directives ?? throw new ArgumentNullException(nameof(directives));
        }

        /// <inheritdoc />
        public abstract SyntaxKind Kind { get; }

        /// <inheritdoc />
        public Location? Location { get; }

        /// <inheritdoc />
        public NameNode Name { get; }

        /// <inheritdoc />
        public IReadOnlyList<DirectiveNode> Directives { get; }

        /// <inheritdoc />
        public abstract IEnumerable<ISyntaxNode> GetNodes();

        /// <inheritdoc />
        public abstract string ToString(bool indented);
    }
}
