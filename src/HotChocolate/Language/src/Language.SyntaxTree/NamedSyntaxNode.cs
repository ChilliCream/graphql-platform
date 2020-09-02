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

        public abstract SyntaxKind Kind { get; }

        public Location? Location { get; }

        public NameNode Name { get; }

        public IReadOnlyList<DirectiveNode> Directives { get; }

        public abstract IEnumerable<ISyntaxNode> GetNodes();

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <param name="indented">
        /// A value that indicates whether the GraphQL output should be formatted,
        /// which includes indenting nested GraphQL tokens, adding
        /// new lines, and adding white space between property names and values.
        /// </param>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public abstract string ToString(bool indented);
    }
}
