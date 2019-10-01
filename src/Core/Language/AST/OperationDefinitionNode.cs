using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class OperationDefinitionNode
        : NamedSyntaxNode
        , IExecutableDefinitionNode
    {
        public OperationDefinitionNode(
            Location? location,
            NameNode name,
            OperationType operation,
            IReadOnlyList<VariableDefinitionNode> variableDefinitions,
            IReadOnlyList<DirectiveNode> directives,
            SelectionSetNode selectionSet)
            : base(location, name, directives)
        {
            Operation = operation;
            VariableDefinitions = variableDefinitions
                ?? throw new ArgumentNullException(nameof(variableDefinitions));
            SelectionSet = selectionSet
                ?? throw new ArgumentNullException(nameof(selectionSet));
        }

        public override NodeKind Kind { get; } = NodeKind.OperationDefinition;

        public OperationType Operation { get; }

        public IReadOnlyList<VariableDefinitionNode> VariableDefinitions { get; }

        public SelectionSetNode SelectionSet { get; }

        public OperationDefinitionNode WithLocation(Location? location)
        {
            return new OperationDefinitionNode(
                location, Name, Operation,
                VariableDefinitions,
                Directives, SelectionSet);
        }

        public OperationDefinitionNode WithName(NameNode name)
        {
            return new OperationDefinitionNode(
                Location, name, Operation,
                VariableDefinitions,
                Directives, SelectionSet);
        }

        public OperationDefinitionNode WithOperation(OperationType operation)
        {
            return new OperationDefinitionNode(
                Location, Name, operation,
                VariableDefinitions,
                Directives, SelectionSet);
        }

        public OperationDefinitionNode WithVariableDefinitions(
            IReadOnlyList<VariableDefinitionNode> variableDefinitions)
        {
            return new OperationDefinitionNode(
                Location, Name, Operation,
                variableDefinitions,
                Directives, SelectionSet);
        }

        public OperationDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new OperationDefinitionNode(
                Location, Name, Operation,
                VariableDefinitions,
                directives, SelectionSet);
        }

        public OperationDefinitionNode WithSelectionSet(
            SelectionSetNode selectionSet)
        {
            return new OperationDefinitionNode(
                Location, Name, Operation,
                VariableDefinitions,
                Directives, selectionSet);
        }
    }
}
