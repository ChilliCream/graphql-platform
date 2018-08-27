using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class FragmentDefinitionNode
        : IExecutableDefinitionNode
        , IHasDirectives
    {
        public FragmentDefinitionNode(
            Location location,
            NameNode name,
            IReadOnlyCollection<VariableDefinitionNode> variableDefinitions,
            NamedTypeNode typeCondition,
            IReadOnlyCollection<DirectiveNode> directives,
            SelectionSetNode selectionSet)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (variableDefinitions == null)
            {
                throw new ArgumentNullException(nameof(variableDefinitions));
            }

            if (typeCondition == null)
            {
                throw new ArgumentNullException(nameof(typeCondition));
            }

            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            if (selectionSet == null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            Location = location;
            Name = name;
            VariableDefinitions = variableDefinitions;
            TypeCondition = typeCondition;
            Directives = directives;
            SelectionSet = selectionSet;
        }

        public NodeKind Kind { get; } = NodeKind.FragmentDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<VariableDefinitionNode> VariableDefinitions { get; }
        public NamedTypeNode TypeCondition { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public SelectionSetNode SelectionSet { get; }
    }
}
