using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

internal sealed partial class FederatedQueryExecutor
{
    public async Task<IQueryResult> ExecuteAsync(
        IFederationContext context,
        CancellationToken cancellationToken = default)
    {
        // Enqueue root node to initiate the execution process.
        var rootSelectionSet = context.Operation.RootSelectionSet;
        var rootResult = context.Result.RentObject(rootSelectionSet.Selections.Count);
        context.Result.SetData(rootResult);
        context.State.RegisterState(new WorkItem(rootSelectionSet, rootResult));

        await context.QueryPlan.ExecuteAsync(context, cancellationToken)
            .ConfigureAwait(false);

        // build the result
        return context.Result.BuildResult();
    }
}
