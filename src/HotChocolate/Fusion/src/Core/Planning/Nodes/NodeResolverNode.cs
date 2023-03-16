using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class NodeResolverNode : QueryPlanNode
{
    public NodeResolverNode(int id) : base(id)
    {

    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.NodeResolver;

    protected override Task OnExecuteAsync(
        FusionExecutionContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
