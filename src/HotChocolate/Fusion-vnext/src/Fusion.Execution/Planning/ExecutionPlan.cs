using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

public sealed record ExecutionPlan
{
    public ImmutableArray<ExecutionNode> RootNodes { get; init; } = [];

    public ImmutableArray<ExecutionNode> AllNodes { get; init; } = [];
}
