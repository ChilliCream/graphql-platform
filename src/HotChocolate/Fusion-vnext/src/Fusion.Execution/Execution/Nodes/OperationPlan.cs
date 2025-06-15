using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed record OperationExecutionPlan
{
    public required OperationDefinitionNode Operation { get; init; }

    public ImmutableArray<ExecutionNode> RootNodes { get; init; } = [];

    public ImmutableArray<ExecutionNode> AllNodes { get; init; } = [];
}
