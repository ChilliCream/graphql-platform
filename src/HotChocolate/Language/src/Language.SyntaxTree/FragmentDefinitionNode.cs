using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class FragmentDefinitionNode
        : NamedSyntaxNode
        , IExecutableDefinitionNode
    {
        public FragmentDefinitionNode(
            Location? location,
            NameNode name,
            IReadOnlyList<VariableDefinitionNode> variableDefinitions,
            NamedTypeNode typeCondition,
            IReadOnlyList<DirectiveNode> directives,
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

        public override SyntaxKind Kind { get; } = SyntaxKind.FragmentDefinition;

        public IReadOnlyList<VariableDefinitionNode> VariableDefinitions { get; }

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
        public override string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public FragmentDefinitionNode WithLocation(Location? location)
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
            IReadOnlyList<VariableDefinitionNode> variableDefinitions)
        {
            return new FragmentDefinitionNode(
                Location, Name,
                variableDefinitions,
                TypeCondition,
                Directives, SelectionSet);
        }

        public FragmentDefinitionNode WithTypeCondition(
            NamedTypeNode typeCondition)
        {
            return new FragmentDefinitionNode(
                Location, Name,
                VariableDefinitions,
                typeCondition,
                Directives, SelectionSet);
        }

        public FragmentDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
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
