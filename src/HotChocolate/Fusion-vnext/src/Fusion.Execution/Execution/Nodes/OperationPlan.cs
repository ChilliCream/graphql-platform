using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed record OperationExecutionPlan
{
    public ImmutableArray<ExecutionNode> RootNodes { get; init; } = [];

    public ImmutableArray<ExecutionNode> AllNodes { get; init; } = [];
}
