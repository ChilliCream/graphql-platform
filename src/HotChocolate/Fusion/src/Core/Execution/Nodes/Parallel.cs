using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The <see cref="Parallel"/> node executes its child nodes in parallel.
/// </summary>
/// <param name="id">
/// The unique id of this node.
/// </param>
internal sealed class Parallel(int id) : QueryPlanNode(id)
{
    /// <summary>
    /// Gets the kind of this node.
    /// </summary>
    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Parallel;

    protected override async Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        RequestState state,
        CancellationToken cancellationToken)
    {
        InitializeNodes(context, cancellationToken, out var tasks);

#if DISABLED_FOR_DEBUGGING
        if(Debugger.IsAttached)
        {
            foreach (var task in tasks)
            {
                await task.ConfigureAwait(false);
            }
            return;
        }
#endif

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private void InitializeNodes(FusionExecutionContext context, CancellationToken cancellationToken, out Task[] tasks)
    {
        var nodes = GetNodesSpan();
        tasks = new Task[nodes.Length];

        ref var node = ref MemoryMarshal.GetReference(nodes);
        ref var task = ref MemoryMarshal.GetArrayDataReference(tasks);
        ref var end = ref Unsafe.Add(ref node, nodes.Length);

        while (Unsafe.IsAddressLessThan(ref node, ref end))
        {
            task = node.ExecuteAsync(context, cancellationToken);
            node = ref Unsafe.Add(ref node, 1)!;
            task = ref Unsafe.Add(ref task, 1)!;
        }
    }
}
