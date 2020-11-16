using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class SelectionSetNode
        : ISyntaxNode
    {
        public SelectionSetNode(IReadOnlyList<ISelectionNode> selections)
            : this(null, selections)
        {
        }

        public SelectionSetNode(
            Location? location,
            IReadOnlyList<ISelectionNode> selections)
        {
            Location = location;
            Selections = selections
                ?? throw new ArgumentNullException(nameof(selections));
        }

        public SyntaxKind Kind { get; } = SyntaxKind.SelectionSet;

        public Location? Location { get; }

        public IReadOnlyList<ISelectionNode> Selections { get; }

        public IEnumerable<ISyntaxNode> GetNodes() => Selections;

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

        public SelectionSetNode WithLocation(Location? location)
        {
            return new SelectionSetNode(
                location, Selections);
        }

        public SelectionSetNode WithSelections(
            IReadOnlyList<ISelectionNode> selections)
        {
            if (selections == null)
            {
                throw new ArgumentNullException(nameof(selections));
            }

            return new SelectionSetNode(
                Location, selections);
        }

        public SelectionSetNode AddSelection(
            ISelectionNode selection)
        {
            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            var selections = new List<ISelectionNode>(Selections);
            selections.Add(selection);

            return new SelectionSetNode(
                Location, selections);
        }

        public SelectionSetNode AddSelections(
            params ISelectionNode[] selection)
        {
            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            var selections = new List<ISelectionNode>(Selections);
            selections.AddRange(selection);

            return new SelectionSetNode(
                Location, selections);
        }

        public SelectionSetNode RemoveSelection(
            ISelectionNode selection)
        {
            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            var selections = new List<ISelectionNode>(Selections);
            selections.Remove(selection);

            return new SelectionSetNode(
                Location, selections);
        }

        public SelectionSetNode RemoveSelections(
            params ISelectionNode[] selection)
        {
            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            var selections = new List<ISelectionNode>(Selections);
            foreach (ISelectionNode node in selection)
            {
                selections.Remove(node);
            }

            return new SelectionSetNode(
                Location, selections);
        }
    }
}
