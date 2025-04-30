using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Nodes;

public abstract record ExecutionNode(int id)
{
    public int Id => id;

    public ImmutableArray<ExecutionNode> Dependencies { get; init; } = [];

    public abstract Task<ExecutionStatus> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default);
}
