using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Planning;

internal sealed class ParallelNode : QueryPlanNode
{
    public ParallelNode(int id) : base(id) { }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Parallel;

    protected override async Task OnExecuteNodesAsync(
        IFederationContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        var tasks = new Task[Nodes.Count];

        for (var i = 0; i < Nodes.Count; i++)
        {
            tasks[i] = Nodes[i].ExecuteAsync(context, cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
