using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class OperationDefinitionNode
        : IExecutableDefinitionNode
    {
        public OperationDefinitionNode(
            Location location,
            NameNode name,
            OperationType operation,
            IReadOnlyCollection<VariableDefinitionNode> variableDefinitions,
            IReadOnlyCollection<DirectiveNode> directives,
            SelectionSetNode selectionSet)
        {
            if (variableDefinitions == null)
            {
                throw new ArgumentNullException(nameof(variableDefinitions));
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
            Operation = operation;
            VariableDefinitions = variableDefinitions;
            Directives = directives;
            SelectionSet = selectionSet;
        }

        public NodeKind Kind { get; } = NodeKind.OperationDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public OperationType Operation { get; }
        public IReadOnlyCollection<VariableDefinitionNode> VariableDefinitions { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public SelectionSetNode SelectionSet { get; }
    }
}