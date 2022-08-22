using HotChocolate.Execution;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Execution;

internal sealed partial class FederatedQueryExecutor
{
    private readonly ServiceConfiguration _serviceConfiguration;
    private readonly GraphQLClientFactory _executorFactory;

    public FederatedQueryExecutor(
        ServiceConfiguration serviceConfiguration,
        GraphQLClientFactory executorFactory)
    {
        _serviceConfiguration = serviceConfiguration ??
            throw new ArgumentNullException(nameof(serviceConfiguration));
        _executorFactory = executorFactory ??
            throw new ArgumentNullException(nameof(executorFactory));
    }

    public async Task<IQueryResult> ExecuteAsync(
        IFederationContext context,
        CancellationToken cancellationToken = default)
    {
        // Enqueue root node to initiate the execution process.
        var rootSelectionSet = context.Operation.RootSelectionSet;
        var rootResult = context.Result.RentObject(rootSelectionSet.Selections.Count);
        context.Result.SetData(rootResult);
        context.Work.Enqueue(new WorkItem(rootSelectionSet, rootResult));

        // We will execute the work backlog as long as there is work to do.
        // The work that is enqueued on the backlog is defined by the query plan.
        while (context.Work.Count > 0)
        {
            await ExecuteWorkItemsAsync(context, cancellationToken).ConfigureAwait(false);
        }

        // build the result
        return context.Result.BuildResult();
    }
}
