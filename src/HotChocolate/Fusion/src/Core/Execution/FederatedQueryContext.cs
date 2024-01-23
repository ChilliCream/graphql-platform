using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// The fusion execution context holds the state of executing a distributed request.
/// </summary>
internal sealed class FusionExecutionContext : IDisposable
{
    private readonly string _schemaName;
    private readonly GraphQLClientFactory _clientFactory;
    private readonly IIdSerializer _idSerializer;
    private readonly OperationContextOwner _operationContextOwner;
    private readonly NodeIdParser _nodeIdParser;

    public FusionExecutionContext(
        FusionGraphConfiguration configuration,
        QueryPlan queryPlan,
        OperationContextOwner operationContextOwner,
        GraphQLClientFactory clientFactory,
        IIdSerializer idSerializer,
        NodeIdParser nodeIdParser)
    {
        Configuration = configuration ??
            throw new ArgumentNullException(nameof(configuration));
        QueryPlan = queryPlan ??
            throw new ArgumentNullException(nameof(queryPlan));
        _operationContextOwner = operationContextOwner ??
            throw new ArgumentNullException(nameof(operationContextOwner));
        _clientFactory = clientFactory ??
            throw new ArgumentNullException(nameof(clientFactory));
        _idSerializer = idSerializer ??
            throw new ArgumentNullException(nameof(idSerializer));
        _nodeIdParser = nodeIdParser ?? 
            throw new ArgumentNullException(nameof(nodeIdParser));
        _schemaName = Schema.Name;
    }

    /// <summary>
    /// Gets the schema that is being executed on.
    /// </summary>
    public ISchema Schema => OperationContext.Schema;

    /// <summary>
    /// Gets the fusion graph configuration.
    /// </summary>
    public FusionGraphConfiguration Configuration { get; }

    /// <summary>
    /// Gets the query plan that is being executed.
    /// </summary>
    public QueryPlan QueryPlan { get; }

    /// <summary>
    /// Gets the execution state.
    /// </summary>
    public RequestState State { get; } = new();

    /// <summary>
    /// Gets access to the underlying operation context.
    /// </summary>
    public OperationContext OperationContext => _operationContextOwner.OperationContext;

    /// <summary>
    /// Gets the operation that is being executed.
    /// </summary>
    public IOperation Operation => OperationContext.Operation;

    /// <summary>
    /// Gets the result builder that is used to build the final result.
    /// </summary>
    public ResultBuilder Result => OperationContext.Result;

    /// <summary>
    /// Defines if query plan components should emit debug infos.
    /// </summary>
    public bool ShowDebugInfo => true;

    /// <summary>
    /// Determines if all data has been fetched for the specified selection set.
    /// </summary>
    /// <param name="selectionSet">
    /// The selection set that is being evaluated.
    /// </param>
    /// <returns>
    /// <c>true</c> if more data is needed for the specified selection set; otherwise, <c>false</c>.
    /// </returns>
    public bool NeedsMoreData(ISelectionSet selectionSet)
        => QueryPlan.HasNodesFor(selectionSet);

    public string? ReformatId(string formattedId, string subgraphName)
    {
        var id = _idSerializer.Deserialize(formattedId);
        var typeName = Configuration.GetTypeName(subgraphName, id.TypeName);
        return _idSerializer.Serialize(_schemaName, typeName, id.Value);
    }
    
    public string ParseTypeNameFromId(string id)
        => _nodeIdParser.ParseTypeName(id);

    public async Task<GraphQLResponse> ExecuteAsync(
        string subgraphName,
        SubgraphGraphQLRequest request,
        CancellationToken cancellationToken)
    {
        await using var client = _clientFactory.CreateClient(subgraphName);
        return await client.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GraphQLResponse[]> ExecuteAsync(
        string subgraphName,
        IReadOnlyList<SubgraphGraphQLRequest> requests,
        CancellationToken cancellationToken)
    {
        if(requests.Count == 1)
        {
            return [await ExecuteAsync(subgraphName, requests[0], cancellationToken),];
        }

        await using var client = _clientFactory.CreateClient(subgraphName);
        var tasks = new Task<GraphQLResponse>[requests.Count];

        for (var i = 0; i < requests.Count; i++)
        {
            tasks[i] = client.ExecuteAsync(requests[i], cancellationToken);
        }

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<GraphQLResponse> SubscribeAsync(
        string subgraphName,
        SubgraphGraphQLRequest request,
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
            context.Configuration,
            context.QueryPlan,
            operationContextOwner,
            context._clientFactory,
            context._idSerializer,
            context._nodeIdParser);
}
