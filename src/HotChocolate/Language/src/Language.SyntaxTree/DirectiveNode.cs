using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

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
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public SyntaxKind Kind { get; } = SyntaxKind.Directive;

        public Location? Location { get; }

        public NameNode Name { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;

            foreach (ArgumentNode argument in Arguments)
            {
                yield return argument;
            }
        }

        public IReadOnlyList<ArgumentNode> Arguments { get; }

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public override string ToString() => SyntaxPrinter.Print(this, true);

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
        public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

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
