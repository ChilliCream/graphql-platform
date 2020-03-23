﻿using System;
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

        public NodeKind Kind { get; } = NodeKind.InlineFragment;

        public Location? Location { get; }

        public NamedTypeNode? TypeCondition { get; }

        public IReadOnlyList<DirectiveNode> Directives { get; }

        public SelectionSetNode SelectionSet { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            yield return SelectionSet;
        }

        public override string ToString() => SyntaxPrinter.Print(this, true);

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
