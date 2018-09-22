using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class FragmentDefinitionNode
        : NamedSyntaxNode
        , IExecutableDefinitionNode
        , IHasDirectives
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
            if (variableDefinitions == null)
            {
                throw new ArgumentNullException(nameof(variableDefinitions));
            }

            if (typeCondition == null)
            {
                throw new ArgumentNullException(nameof(typeCondition));
            }

            if (selectionSet == null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            VariableDefinitions = variableDefinitions;
            TypeCondition = typeCondition;
            SelectionSet = selectionSet;
        }

        public override NodeKind Kind { get; } = NodeKind.FragmentDefinition;

        public IReadOnlyCollection<VariableDefinitionNode> VariableDefinitions
        { get; }

        public NamedTypeNode TypeCondition { get; }

        public SelectionSetNode SelectionSet { get; }
    }
}
