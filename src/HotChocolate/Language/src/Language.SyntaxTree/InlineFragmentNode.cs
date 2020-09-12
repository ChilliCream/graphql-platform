using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class InlineFragmentNode
        : ISelectionNode
    {
        public InlineFragmentNode(
            Location? location,
            NamedTypeNode? typeCondition,
            IReadOnlyList<DirectiveNode> directives,
            SelectionSetNode selectionSet)
        {
            Location = location;
            TypeCondition = typeCondition;
            Directives = directives
                ?? throw new ArgumentNullException(nameof(directives));
            SelectionSet = selectionSet
                ?? throw new ArgumentNullException(nameof(selectionSet));
        }

        public SyntaxKind Kind { get; } = SyntaxKind.InlineFragment;

        public Location? Location { get; }

        public NamedTypeNode? TypeCondition { get; }

        public IReadOnlyList<DirectiveNode> Directives { get; }

        public SelectionSetNode SelectionSet { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            if (TypeCondition is { })
            {
                yield return TypeCondition;
            }

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            yield return SelectionSet;
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
        public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public InlineFragmentNode WithLocation(Location? location)
        {
            return new InlineFragmentNode(
                location, TypeCondition,
                Directives, SelectionSet);
        }

        public InlineFragmentNode WithTypeCondition(
            NamedTypeNode? typeCondition)
        {
            return new InlineFragmentNode(
                Location, typeCondition,
                Directives, SelectionSet);
        }

        public InlineFragmentNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new InlineFragmentNode(
                Location, TypeCondition,
                directives, SelectionSet);
        }

        public InlineFragmentNode WithSelectionSet(
            SelectionSetNode selectionSet)
        {
            return new InlineFragmentNode(
                Location, TypeCondition,
                Directives, selectionSet);
        }
    }
}
