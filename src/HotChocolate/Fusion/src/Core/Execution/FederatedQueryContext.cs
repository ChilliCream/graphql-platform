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

    public bool NeedsMoreData(ISelectionSet selectionSet)
        => QueryPlan.HasNodes(selectionSet);

    public async Task<GraphQLResponse> ExecuteAsync(string subgraphName, GraphQLRequest request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.Create(subgraphName);
        return await client.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GraphQLResponse>> ExecuteAsync(
        string subgraphName,
        IReadOnlyList<GraphQLRequest> requests,
        CancellationToken cancellationToken)
    {
        using var client = _clientFactory.Create(subgraphName);
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
