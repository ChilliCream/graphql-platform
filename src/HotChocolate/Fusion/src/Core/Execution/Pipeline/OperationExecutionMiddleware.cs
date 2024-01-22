using HotChocolate.Execution;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Language;
using HotChocolate.Types.Relay;
using ErrorHelper = HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class DistributedOperationExecutionMiddleware(
    RequestDelegate next,
    IFactory<OperationContextOwner> contextFactory,
    IIdSerializer idSerializer,
    [SchemaService] FusionGraphConfiguration serviceConfig,
    [SchemaService] GraphQLClientFactory clientFactory,
    [SchemaService] NodeIdParser nodeIdParser)
{
    private static readonly object _queryRoot = new();
    private static readonly object _mutationRoot = new();
    private static readonly object _subscriptionRoot = new();

    private readonly RequestDelegate _next = next
        ?? throw new ArgumentNullException(nameof(next));
    private readonly FusionGraphConfiguration _serviceConfig = serviceConfig
        ?? throw new ArgumentNullException(nameof(serviceConfig));
    private readonly IFactory<OperationContextOwner> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));
    private readonly IIdSerializer _idSerializer = idSerializer
        ?? throw new ArgumentNullException(nameof(idSerializer));
    private readonly GraphQLClientFactory _clientFactory = clientFactory
        ?? throw new ArgumentNullException(nameof(clientFactory));
    private readonly NodeIdParser _nodeIdParser = nodeIdParser
        ?? throw new ArgumentNullException(nameof(nodeIdParser));

    public async ValueTask InvokeAsync(
        IRequestContext context,
        IBatchDispatcher batchDispatcher)
    {
        if (context.Operation is not null &&
            context.Variables is not null &&
            context.Operation.ContextData.TryGetValue(PipelineProps.QueryPlan, out var value) &&
            value is QueryPlan queryPlan)
        {
            var operationContextOwner = _contextFactory.Create();
            var operationContext = operationContextOwner.OperationContext;

            operationContext.Initialize(
                context,
                context.Services,
                batchDispatcher,
                context.Operation,
                context.Variables,
                GetRootObject(context.Operation),
                () => _queryRoot);

            var federatedQueryContext =
                new FusionExecutionContext(
                    _serviceConfig,
                    queryPlan,
                    operationContextOwner,
                    _clientFactory,
                    _idSerializer,
                    _nodeIdParser);

            context.Result =
                await FederatedQueryExecutor.ExecuteAsync(
                    federatedQueryContext,
                    context.RequestAborted)
                    .ConfigureAwait(false);

            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = ErrorHelper.StateInvalidForOperationExecution();
        }
    }

    // We are faking root instances. Since we do not have proper resolvers and
    // all we do not need them actually. But so we can reuse components we just
    // have static instances simulating root instance.
    private static object GetRootObject(IOperation operation)
        => operation.Type switch
        {
            OperationType.Query => _queryRoot,
            OperationType.Mutation => _mutationRoot,
            OperationType.Subscription => _subscriptionRoot,
            _ => throw new NotSupportedException(),
        };
}
