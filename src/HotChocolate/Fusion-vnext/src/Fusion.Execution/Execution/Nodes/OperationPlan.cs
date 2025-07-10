using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed record OperationExecutionPlan
{
    public required Operation Operation { get; init; }

    public required OperationDefinitionNode OperationDefinition { get; init; }

    public IReadOnlyList<VariableDefinitionNode> VariableDefinitions => OperationDefinition.VariableDefinitions;

    public string? OperationName => OperationDefinition.Name?.Value;

    public required ImmutableArray<ExecutionNode> RootNodes { get; init; }

    public required ImmutableArray<ExecutionNode> AllNodes { get; init; }
}
