using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class FieldNode
        : NamedSyntaxNode
        , ISelectionNode
    {
        public FieldNode(
            Location? location,
            NameNode name,
            NameNode? alias,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<ArgumentNode> arguments,
            SelectionSetNode? selectionSet)
            : base(location, name, directives)
        {
            Alias = alias;
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
            SelectionSet = selectionSet;
        }

        public override SyntaxKind Kind { get; } = SyntaxKind.Field;

        public NameNode? Alias { get; }

        public IReadOnlyList<ArgumentNode> Arguments { get; }

        public SelectionSetNode? SelectionSet { get; }

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            if (Alias is { })
            {
                yield return Alias;
            }

            yield return Name;

            foreach (ArgumentNode argument in Arguments)
            {
                yield return argument;
            }

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            if (SelectionSet is { })
            {
                yield return SelectionSet;
            }
        }

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
        public override string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public FieldNode WithLocation(Location? location)
        {
            return new FieldNode(location, Name, Alias,
                Directives, Arguments, SelectionSet);
        }

        public FieldNode WithName(NameNode name)
        {
            return new FieldNode(Location, name, Alias,
                Directives, Arguments, SelectionSet);
        }

        public FieldNode WithAlias(NameNode? alias)
        {
            return new FieldNode(Location, Name, alias,
                Directives, Arguments, SelectionSet);
        }

        public FieldNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new FieldNode(Location, Name, Alias,
                directives, Arguments, SelectionSet);
        }

        public FieldNode WithArguments(
            IReadOnlyList<ArgumentNode> arguments)
        {
            return new FieldNode(Location, Name, Alias,
                Directives, arguments, SelectionSet);
        }

        public FieldNode WithSelectionSet(SelectionSetNode? selectionSet)
        {
            return new FieldNode(Location, Name, Alias,
                Directives, Arguments, selectionSet);
        }
    }
}
