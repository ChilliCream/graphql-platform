using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

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
        _clientFactory = clientFactory ??
            throw new ArgumentNullException(nameof(clientFactory));
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

    public async Task<GraphQLResponse> ExecuteAsync(
        string subgraphName,
        GraphQLRequest request,
        CancellationToken cancellationToken)
    {
        await using var client = _clientFactory.CreateClient(subgraphName);
        return await client.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GraphQLResponse>> ExecuteAsync(
        string subgraphName,
        IReadOnlyList<GraphQLRequest> requests,
        CancellationToken cancellationToken)
    {
        await using var client = _clientFactory.CreateClient(subgraphName);
        var responses = new GraphQLResponse[requests.Count];

        for (var i = 0; i < requests.Count; i++)
        {
            responses[i] =
                await client.ExecuteAsync(requests[i], cancellationToken)
                    .ConfigureAwait(false);
        }

        return responses;
    }

    public async IAsyncEnumerable<GraphQLResponse> SubscribeAsync(
        string subgraphName,
        GraphQLRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var client = _clientFactory.CreateSubscriptionClient(subgraphName);

        await foreach (var response in client.SubscribeAsync(request, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return response;
        }
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
