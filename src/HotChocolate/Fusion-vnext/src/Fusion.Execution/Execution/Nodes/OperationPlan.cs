using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed record OperationExecutionPlan
{
    public required OperationDefinitionNode Operation { get; init; }

    public string? OperationName => Operation.Name?.Value;

    public required ImmutableArray<ExecutionNode> RootNodes { get; init; }

    public required ImmutableArray<ExecutionNode> AllNodes { get; init; }
}
