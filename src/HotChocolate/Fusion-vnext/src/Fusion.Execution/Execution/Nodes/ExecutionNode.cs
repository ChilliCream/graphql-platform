using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Nodes;

public abstract record ExecutionNode
{
    public required int Id { get; init; }

    public ImmutableArray<ExecutionNode> Dependencies { get; init; } = [];

    public abstract Task<ExecutionStatus> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default);
}

public record ExecutionStatus(int Id);
