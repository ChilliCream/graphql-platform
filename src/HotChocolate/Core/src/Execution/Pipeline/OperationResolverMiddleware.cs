using System.Runtime.CompilerServices;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ObjectPool<OperationCompiler> _operationCompilerPool;
    private readonly OperationCompilerOptimizers _operationCompilerOptimizers;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;

    private OperationResolverMiddleware(
        RequestDelegate next,
        ObjectPool<OperationCompiler> operationCompilerPool,
        OperationCompilerOptimizers operationCompilerOptimizer,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(operationCompilerPool);
        ArgumentNullException.ThrowIfNull(operationCompilerOptimizer);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _next = next;
        _operationCompilerPool = operationCompilerPool;
        _operationCompilerOptimizers = operationCompilerOptimizer;
        _diagnosticEvents = diagnosticEvents;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        if (context.TryGetOperation(out var operation, out var operationId))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var documentInfo = context.OperationDocumentInfo;
        if (documentInfo.Document is not null && documentInfo.IsValidated)
        {
            using (_diagnosticEvents.CompileOperation(context))
            {
                var operationDef = documentInfo.Document.GetOperation(context.Request.OperationName);
                var operationType = ResolveOperationType(operationDef.Operation, Unsafe.As<Schema>(context.Schema));

                if (operationType is null)
                {
                    context.Result = RootTypeNotFound(operationDef.Operation);
                    return;
                }

                operation = CompileOperation(
                    documentInfo.Document,
                    operationDef,
                    operationType,
                    operationId ?? Guid.NewGuid().ToString("N"),
                    context.Schema);

                context.SetOperation(operation);
            }

            await _next(context).ConfigureAwait(false);
            return;
        }

        context.Result = StateInvalidForOperationResolver();
    }

    private IOperation CompileOperation(
        DocumentNode document,
        OperationDefinitionNode operationDefinition,
        ObjectType operationType,
        string operationId,
        ISchemaDefinition schema)
    {
        var request = new OperationCompilerRequest(
            operationId,
            document,
            operationDefinition,
            operationType,
            schema,
            _operationCompilerOptimizers.OperationOptimizers,
            _operationCompilerOptimizers.SelectionSetOptimizers);

        var compiler = _operationCompilerPool.Get();
        var operation = compiler.Compile(request);
        _operationCompilerPool.Return(compiler);

        return operation;
    }

    private static ObjectType? ResolveOperationType(
        OperationType operationType,
        Schema schema)
        => operationType switch
        {
            OperationType.Query => schema.QueryType,
            OperationType.Mutation => schema.MutationType,
            OperationType.Subscription => schema.SubscriptionType,
            _ => throw ThrowHelper.RootTypeNotSupported(operationType)
        };

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var operationCompilerPool = core.Services.GetRequiredService<ObjectPool<OperationCompiler>>();
                var optimizers = core.SchemaServices.GetRequiredService<OperationCompilerOptimizers>();
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
                var middleware = new OperationResolverMiddleware(
                    next,
                    operationCompilerPool,
                    optimizers,
                    diagnosticEvents);
                return context => middleware.InvokeAsync(context);
            },
            nameof(OperationResolverMiddleware));
}
