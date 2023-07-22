using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The <see cref="Parallel"/> node executes its child nodes in parallel.
/// </summary>
internal sealed class Parallel : QueryPlanNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="Parallel"/>.
    /// </summary>
    /// <param name="id">
    /// The unique id of this node.
    /// </param>
    public Parallel(int id) : base(id) { }

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