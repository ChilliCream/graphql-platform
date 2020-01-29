using System;
using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public sealed class FragmentDefinitionNode
        : NamedSyntaxNode
        , IExecutableDefinitionNode
        , INamedSyntaxNode
    {
        public FragmentDefinitionNode(
            Location location,
            NameNode name,
            NamedTypeNode typeCondition,
            IReadOnlyList<DirectiveNode> directives,
            SelectionSetNode selectionSet)
            : base(location, name, directives)
        {
            TypeCondition = typeCondition
                ?? throw new ArgumentNullException(nameof(typeCondition));
            SelectionSet = selectionSet
                ?? throw new ArgumentNullException(nameof(selectionSet));
        }

        public override NodeKind Kind { get; } = NodeKind.FragmentDefinition;

        public NamedTypeNode TypeCondition { get; }

        public SelectionSetNode SelectionSet { get; }

        public override IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;
            yield return TypeCondition;

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            yield return SelectionSet;
        }

        public FragmentDefinitionNode WithLocation(Location location)
        {
            return new FragmentDefinitionNode(
                location, Name,
                TypeCondition,
                Directives, SelectionSet);
        }

        public FragmentDefinitionNode WithName(NameNode name)
        {
            return new FragmentDefinitionNode(
                Location, name,
                TypeCondition,
                Directives, SelectionSet);
        }

        public FragmentDefinitionNode WithTypeCondition(
            NamedTypeNode typeCondition)
        {
            return new FragmentDefinitionNode(
                Location, Name,
                typeCondition,
                Directives, SelectionSet);
        }

        public FragmentDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new FragmentDefinitionNode(
                Location, Name,
                TypeCondition,
                directives, SelectionSet);
        }

        public FragmentDefinitionNode WithSelectionSet(
            SelectionSetNode selectionSet)
        {
            return new FragmentDefinitionNode(
                Location, Name,
                TypeCondition,
                Directives, selectionSet);
        }
    }
}
