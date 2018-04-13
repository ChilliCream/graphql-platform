using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class SelectionSetNode
        : ISyntaxNode
    {
        public SelectionSetNode(
            Location location, 
            IReadOnlyCollection<ISelectionNode> selections)
        {
            if (selections == null)
            {
                throw new ArgumentNullException(nameof(selections));
            }

            Location = location;
            Selections = selections;
        }

        public NodeKind Kind { get; } = NodeKind.SelectionSet;
        public Location Location { get; }
        public IReadOnlyCollection<ISelectionNode> Selections { get; }
    }
}