using HotChocolate.Execution;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution.Diagnostic;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Language;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using ErrorHelper = HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class DistributedOperationExecutionMiddleware(
    RequestDelegate next,
    IFactory<OperationContextOwner> contextFactory,
    [SchemaService] INodeIdSerializer nodeIdSerializer,
    [SchemaService] FusionGraphConfiguration serviceConfig,
    [SchemaService] GraphQLClientFactory clientFactory,
    [SchemaService] NodeIdParser nodeIdParser,
    [SchemaService] FusionOptions options,
    [SchemaService] IFusionDiagnosticEvents diagnosticEvents,
    [SchemaService] IErrorHandler errorHandler)
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
    private readonly INodeIdSerializer _nodeIdSerializer = nodeIdSerializer
        ?? throw new ArgumentNullException(nameof(nodeIdSerializer));
    private readonly GraphQLClientFactory _clientFactory = clientFactory
        ?? throw new ArgumentNullException(nameof(clientFactory));
    private readonly NodeIdParser _nodeIdParser = nodeIdParser
        ?? throw new ArgumentNullException(nameof(nodeIdParser));
    private readonly FusionOptions _fusionOptionsAccessor = options
        ?? throw new ArgumentNullException(nameof(options));
    private readonly IFusionDiagnosticEvents _diagnosticEvents = diagnosticEvents
        ?? throw new ArgumentNullException(nameof(diagnosticEvents));
    private readonly IErrorHandler _errorHandler = errorHandler
        ?? throw new ArgumentNullException(nameof(errorHandler));

    public async ValueTask InvokeAsync(
        IRequestContext context,
        IBatchDispatcher batchDispatcher)
    {
        // todo: we do need to add variable batching.
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
                context.Variables[0],
                GetRootObject(context.Operation),
                () => _queryRoot);

            operationContext.Result.SetSingleErrorPerPath();

            var federatedQueryContext =
                new FusionExecutionContext(
                    _serviceConfig,
                    queryPlan,
                    operationContextOwner,
                    _clientFactory,
                    _nodeIdSerializer,
                    _nodeIdParser,
                    _fusionOptionsAccessor,
                    _diagnosticEvents,
                    _errorHandler);

            using (federatedQueryContext.DiagnosticEvents.ExecuteFederatedQuery(context))
            {
                context.Result =
                    await FederatedQueryExecutor.ExecuteAsync(
                            federatedQueryContext,
                            context.RequestAborted)
                        .ConfigureAwait(false);
            }

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

    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var contextFactory = core.Services.GetRequiredService<IFactory<OperationContextOwner>>();
            var idSerializer = core.SchemaServices.GetRequiredService<INodeIdSerializer>();
            var serviceConfig = core.SchemaServices.GetRequiredService<FusionGraphConfiguration>();
            var clientFactory = core.SchemaServices.GetRequiredService<GraphQLClientFactory>();
            var nodeIdParser = core.SchemaServices.GetRequiredService<NodeIdParser>();
            var fusionOptionsAccessor = core.SchemaServices.GetRequiredService<FusionOptions>();
            var diagnosticEvents = core.SchemaServices.GetRequiredService<IFusionDiagnosticEvents>();
            var errorHandler = core.SchemaServices.GetRequiredService<IErrorHandler>();
            var middleware = new DistributedOperationExecutionMiddleware(
                next,
                contextFactory,
                idSerializer,
                serviceConfig,
                clientFactory,
                nodeIdParser,
                fusionOptionsAccessor,
                diagnosticEvents,
                errorHandler);
            return async context =>
            {
                var batchDispatcher = context.Services.GetRequiredService<IBatchDispatcher>();
                await middleware.InvokeAsync(context, batchDispatcher);
            };
        };
}
