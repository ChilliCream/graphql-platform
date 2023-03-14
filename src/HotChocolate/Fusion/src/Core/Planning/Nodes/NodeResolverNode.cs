using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class NodeResolverNode : ResolverNodeBase
{
    public NodeResolverNode(
        int id,
        string subgraphName,
        DocumentNode document,
        ISelectionSet selectionSet,
        IReadOnlyList<string> requires,
        IReadOnlyList<string> path,
        IReadOnlyList<string> forwardedVariables)
        : base(
            id,
            subgraphName,
            document,
            selectionSet,
            requires,
            path,
            forwardedVariables)
    {
    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.NodeResolver;

    protected override async Task OnExecuteAsync(
        FusionExecutionContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        if (state.TryGetState(SelectionSet, out var workItems))
        {


        }
    }

    protected override Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}
