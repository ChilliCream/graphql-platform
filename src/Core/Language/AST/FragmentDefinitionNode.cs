using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class FragmentDefinitionNode
        : NamedSyntaxNode
        , IExecutableDefinitionNode
    {
        public FragmentDefinitionNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<VariableDefinitionNode> variableDefinitions,
            NamedTypeNode typeCondition,
            IReadOnlyCollection<DirectiveNode> directives,
            SelectionSetNode selectionSet)
            : base(location, name, directives)
        {
            VariableDefinitions = variableDefinitions 
                ?? throw new ArgumentNullException(nameof(variableDefinitions));
            TypeCondition = typeCondition 
                ?? throw new ArgumentNullException(nameof(typeCondition));
            SelectionSet = selectionSet 
                ?? throw new ArgumentNullException(nameof(selectionSet));
        }

        public override NodeKind Kind { get; } = NodeKind.FragmentDefinition;

        public IReadOnlyCollection<VariableDefinitionNode> VariableDefinitions
        { get; }

        public NamedTypeNode TypeCondition { get; }

        public SelectionSetNode SelectionSet { get; }

        public FragmentDefinitionNode WithLocation(Location location)
        {
            return new FragmentDefinitionNode(
                location, Name,
                VariableDefinitions,
                TypeCondition,
                Directives, SelectionSet);
        }

        public FragmentDefinitionNode WithName(NameNode name)
        {
            return new FragmentDefinitionNode(
                Location, name,
                VariableDefinitions,
                TypeCondition,
                Directives, SelectionSet);
        }

        public FragmentDefinitionNode WithVariableDefinitions(
            IReadOnlyCollection<VariableDefinitionNode> variableDefinitions)
        {
            return new FragmentDefinitionNode(
                Location, Name,
                variableDefinitions,
                TypeCondition,
                Directives, SelectionSet);
        }

        public FragmentDefinitionNode WithTypeCondition(ITypeNode typeCondition)
        {
            return new FragmentDefinitionNode(
                Location, Name,
                VariableDefinitions,
                TypeCondition,
                Directives, SelectionSet);
        }

        public FragmentDefinitionNode WithDirectives(
            IReadOnlyCollection<DirectiveNode> directives)
        {
            return new FragmentDefinitionNode(
                Location, Name,
                VariableDefinitions,
                TypeCondition,
                directives, SelectionSet);
        }

        public FragmentDefinitionNode WithSelectionSet(
            SelectionSetNode selectionSet)
        {
            return new FragmentDefinitionNode(
                Location, Name,
                VariableDefinitions,
                TypeCondition,
                Directives, selectionSet);
        }
    }
}
