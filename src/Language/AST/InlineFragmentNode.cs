using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class InlineFragmentNode
        : ISelectionNode
    {
        public InlineFragmentNode(
            Location location,
            NamedTypeNode typeCondition,
            IReadOnlyCollection<DirectiveNode> directives,
            SelectionSetNode selectionSet)
        {
            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            if (selectionSet == null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            Location = location;
            TypeCondition = typeCondition;
            Directives = directives;
            SelectionSet = selectionSet;
        }

        public NodeKind Kind { get; } = NodeKind.InlineFragment;
        public Location Location { get; }
        public NamedTypeNode TypeCondition { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public SelectionSetNode SelectionSet { get; }
    }
}
