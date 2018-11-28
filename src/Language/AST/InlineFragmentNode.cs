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

        public InlineFragmentNode WithLocation(Location location)
        {
            return new InlineFragmentNode(
                location, TypeCondition,
                Directives, SelectionSet);
        }

        public InlineFragmentNode WithTypeCondition(NamedTypeNode typeCondition)
        {
            return new InlineFragmentNode(
                Location, typeCondition,
                Directives, SelectionSet);
        }

        public InlineFragmentNode WithDirectives(
            IReadOnlyCollection<DirectiveNode> directives)
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
