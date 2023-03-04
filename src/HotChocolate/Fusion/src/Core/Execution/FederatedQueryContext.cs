using System.Collections.Concurrent;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Execution;

internal sealed class FederatedQueryContext : IFederationContext
{
    private readonly GraphQLClientFactory _clientFactory;

    public FederatedQueryContext(
        FusionGraphConfiguration serviceConfig,
        QueryPlan queryPlan,
        OperationContext operationContext,
        GraphQLClientFactory clientFactory)
    {
        ServiceConfig = serviceConfig ??
            throw new ArgumentNullException(nameof(serviceConfig));
        QueryPlan = queryPlan ??
            throw new ArgumentNullException(nameof(queryPlan));
        OperationContext = operationContext ??
            throw new ArgumentNullException(nameof(operationContext));
        _clientFactory = clientFactory;
    }

    public FusionGraphConfiguration ServiceConfig { get; }

    public QueryPlan QueryPlan { get; }

    public IExecutionState State { get; } = new ExecutionState();

    public OperationContext OperationContext { get; }

    public ConcurrentQueue<WorkItem> Work { get; } = new();

    public Dictionary<QueryPlanNode, List<WorkItem>> WorkByNode { get; } = new();

    public HashSet<QueryPlanNode> Completed { get; } = new();

    public bool NeedsMoreData(ISelectionSet selectionSet)
        => QueryPlan.HasNodes(selectionSet);

    // TODO : implement batching here
    public async Task<IReadOnlyList<GraphQLResponse>> ExecuteAsync(
        string schemaName,
        IReadOnlyList<GraphQLRequest> requests,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory.Create(schemaName);
        var responses = new GraphQLResponse[requests.Count];

        for (var i = 0; i < requests.Count; i++)
        {
            responses[i] =
                await client.ExecuteAsync(requests[i], cancellationToken)
                    .ConfigureAwait(false);
        }

        return responses;
    }
}
