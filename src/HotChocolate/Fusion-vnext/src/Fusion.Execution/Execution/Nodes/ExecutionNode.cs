using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Nodes;

public abstract record ExecutionNode(int id, ImmutableArray<ExecutionNode> dependencies)
{
    public int Id => id;

    public ImmutableArray<ExecutionNode> Dependencies => dependencies;

    public abstract Task<ExecutionStatus> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default);
}
