using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class SelectionSetNode
        : ISyntaxNode
    {
        public SelectionSetNode(
            Location location,
            IReadOnlyList<ISelectionNode> selections)
        {
            Location = location;
            Selections = selections
                ?? throw new ArgumentNullException(nameof(selections));
        }

        public NodeKind Kind { get; } = NodeKind.SelectionSet;

        public Location Location { get; }

        public IReadOnlyList<ISelectionNode> Selections { get; }

        public SelectionSetNode WithLocation(Location location)
        {
            return new SelectionSetNode(
                location, Selections);
        }

        public SelectionSetNode WithSelections(
            IReadOnlyList<ISelectionNode> selections)
        {
            return new SelectionSetNode(
                Location, selections);
        }
    }
}
