using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Execution;

internal sealed class FusionExecutionContext : IDisposable
{
    private readonly GraphQLClientFactory _clientFactory;
    private readonly OperationContextOwner _operationContextOwner;

    public FusionExecutionContext(
        FusionGraphConfiguration serviceConfig,
        QueryPlan queryPlan,
        OperationContextOwner operationContextOwner,
        GraphQLClientFactory clientFactory)
    {
        ServiceConfig = serviceConfig ??
            throw new ArgumentNullException(nameof(serviceConfig));
        QueryPlan = queryPlan ??
            throw new ArgumentNullException(nameof(queryPlan));
        _operationContextOwner = operationContextOwner ??
            throw new ArgumentNullException(nameof(operationContextOwner));
        _clientFactory = clientFactory;
    }

    public FusionGraphConfiguration ServiceConfig { get; }

    public QueryPlan QueryPlan { get; }

    public IExecutionState State { get; } = new ExecutionState();

    public OperationContext OperationContext => _operationContextOwner.OperationContext;

    public ISchema Schema => OperationContext.Schema;

    public ResultBuilder Result => OperationContext.Result;

    public IOperation Operation => OperationContext.Operation;

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

    public IAsyncEnumerable<GraphQLResponse> SubscribeAsync(
        string subgraphName,
        GraphQLRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
        => _operationContextOwner.Dispose();

    public static FusionExecutionContext CreateFrom(
        FusionExecutionContext context,
        OperationContextOwner operationContextOwner)
        => new FusionExecutionContext(
            context.ServiceConfig,
            context.QueryPlan,
            operationContextOwner,
            context._clientFactory);
}
