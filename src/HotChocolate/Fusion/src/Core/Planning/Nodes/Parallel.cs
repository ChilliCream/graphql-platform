using System.Diagnostics;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Planning;

internal sealed class Parallel : QueryPlanNode
{
    public Parallel(int id) : base(id) { }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Parallel;

    protected override async Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        RequestState state,
        CancellationToken cancellationToken)
    {
        if (Debugger.IsAttached)
        {
            await base.OnExecuteNodesAsync(context, state, cancellationToken).ConfigureAwait(false);
            return;
        }
        
        var tasks = new Task[Nodes.Count];

        for (var i = 0; i < Nodes.Count; i++)
        {
            tasks[i] = Nodes[i].ExecuteAsync(context, cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
