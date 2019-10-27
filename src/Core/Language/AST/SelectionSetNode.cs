using System;
using System.Collections.Generic;

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

        public NodeKind Kind { get; } = NodeKind.SelectionSet;

        public Location? Location { get; }

        public IReadOnlyList<ISelectionNode> Selections { get; }

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
