using System;
using System.Collections.Generic;

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

        public NodeKind Kind { get; } = NodeKind.InlineFragment;

        public Location? Location { get; }

        public NamedTypeNode? TypeCondition { get; }

        public IReadOnlyList<DirectiveNode> Directives { get; }

        public SelectionSetNode SelectionSet { get; }

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
