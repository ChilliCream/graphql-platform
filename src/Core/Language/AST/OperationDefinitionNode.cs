using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class OperationDefinitionNode
        : IExecutableDefinitionNode
        , IHasDirectives
    {
        public OperationDefinitionNode(
            Location location,
            NameNode name,
            OperationType operation,
            IReadOnlyCollection<VariableDefinitionNode> variableDefinitions,
            IReadOnlyCollection<DirectiveNode> directives,
            SelectionSetNode selectionSet)
        {
            Location = location;
            Name = name;
            Operation = operation;
            VariableDefinitions = variableDefinitions 
                ?? throw new ArgumentNullException(nameof(variableDefinitions));
            Directives = directives 
                ?? throw new ArgumentNullException(nameof(directives));
            SelectionSet = selectionSet 
                ?? throw new ArgumentNullException(nameof(selectionSet));
        }

        public NodeKind Kind { get; } = NodeKind.OperationDefinition;

        public Location Location { get; }

        public NameNode Name { get; }

        public OperationType Operation { get; }

        public IReadOnlyCollection<VariableDefinitionNode> VariableDefinitions
        { get; }

        public IReadOnlyCollection<DirectiveNode> Directives { get; }

        public SelectionSetNode SelectionSet { get; }

        public OperationDefinitionNode WithLocation(Location location)
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
            IReadOnlyCollection<VariableDefinitionNode> variableDefinitions)
        {
            return new OperationDefinitionNode(
                Location, Name, Operation,
                variableDefinitions,
                Directives, SelectionSet);
        }

        public OperationDefinitionNode WithDirectives(
            IReadOnlyCollection<DirectiveNode> directives)
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
