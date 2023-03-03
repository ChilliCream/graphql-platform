using System.Buffers;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Serialization;
using HotChocolate.Fetching;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Pipeline;

internal sealed class OperationExecutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly FederatedQueryExecutor _executor;
    private readonly ServiceConfiguration _serviceConfig;
    private readonly ISchema _schema;
    private readonly ObjectPool<OperationContext> _operationContextPool;
    private readonly GraphQLClientFactory _clientFactory;

    public OperationExecutionMiddleware(
        RequestDelegate next,
        ObjectPool<OperationContext> operationContextPool,
        [SchemaService] FederatedQueryExecutor executor,
        [SchemaService] ServiceConfiguration serviceConfig,
        [SchemaService] GraphQLClientFactory clientFactory,
        [SchemaService] ISchema schema)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _operationContextPool = operationContextPool ??
            throw new ArgumentNullException(nameof(operationContextPool));
        _executor = executor ??
            throw new ArgumentNullException(nameof(executor));
        _serviceConfig = serviceConfig ??
            throw new ArgumentNullException(nameof(serviceConfig));
        _schema = schema ??
            throw new ArgumentNullException(nameof(schema));
        _clientFactory = clientFactory ??
            throw new ArgumentNullException(nameof(clientFactory));
    }

    public async ValueTask InvokeAsync(
        IRequestContext context,
        IBatchDispatcher batchDispatcher)
    {
        if (context.Operation is not null &&
            context.Variables is not null &&
            context.Operation.ContextData.TryGetValue(PipelineProps.QueryPlan, out var value) &&
            value is QueryPlan queryPlan)
        {
            var operationContext = _operationContextPool.Get();

            operationContext.Initialize(
                context,
                context.Services,
                batchDispatcher,
                context.Operation,
                context.Variables,
                new object(), // todo: we can use static representations for these
                () => new object());  // todo: we can use static representations for these

            var federatedQueryContext = new FederatedQueryContext(
                _serviceConfig,
                queryPlan,
                operationContext,
                _clientFactory);

            if (context.ContextData.ContainsKey(WellKnownContextData.IncludeQueryPlan))
            {
                var bufferWriter = new ArrayBufferWriter<byte>();

                queryPlan.Format(bufferWriter);

                operationContext.Result.SetExtension(
                    "queryPlan",
                    new RawJsonValue(bufferWriter.WrittenMemory));
            }

            // we store the context on the result for unit tests.
            operationContext.Result.SetContextData("queryPlan", queryPlan);

            context.Result = await _executor.ExecuteAsync(
                federatedQueryContext,
                context.RequestAborted)
                .ConfigureAwait(false);

            _operationContextPool.Return(operationContext);

            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = ErrorHelper.StateInvalidForOperationExecution();
        }
    }
}
