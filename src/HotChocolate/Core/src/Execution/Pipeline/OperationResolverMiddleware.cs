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

    private OperationResolverMiddleware(
        RequestDelegate next,
        ObjectPool<OperationCompiler> operationCompilerPool,
        OperationCompilerOptimizers operationCompilerOptimizer)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _operationCompilerPool =
            operationCompilerPool ?? throw new ArgumentNullException(nameof(operationCompilerPool));
        _operationCompilerOptimizers = operationCompilerOptimizer
            ?? throw new ArgumentNullException(nameof(operationCompilerOptimizer));
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Operation is not null)
        {
            await _next(context).ConfigureAwait(false);
        }
        else if (context.Document is not null && context.IsValidDocument)
        {
            using (context.DiagnosticEvents.CompileOperation(context))
            {
                var operationDef = context.Document.GetOperation(context.Request.OperationName);
                var operationType = ResolveOperationType(operationDef.Operation, context.Schema);

                if (operationType is null)
                {
                    context.Result = RootTypeNotFound(operationDef.Operation);
                    return;
                }

                context.Operation = CompileOperation(
                    context,
                    context.OperationId ?? Guid.NewGuid().ToString("N"),
                    operationDef,
                    operationType);
                context.OperationId = context.Operation.Id;
            }

            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = StateInvalidForOperationResolver();
        }
    }

    private IOperation CompileOperation(
        IRequestContext context,
        string operationId,
        OperationDefinitionNode operationDefinition,
        ObjectType operationType)
    {
        var request = new OperationCompilerRequest(
            operationId,
            context.Document!,
            operationDefinition,
            operationType,
            context.Schema,
            _operationCompilerOptimizers.OperationOptimizers,
            _operationCompilerOptimizers.SelectionSetOptimizers);

        var compiler = _operationCompilerPool.Get();
        var operation = compiler.Compile(request);
        _operationCompilerPool.Return(compiler);

        return operation;
    }

    private static ObjectType? ResolveOperationType(
        OperationType operationType,
        ISchema schema)
        => operationType switch
        {
            OperationType.Query => schema.QueryType,
            OperationType.Mutation => schema.MutationType,
            OperationType.Subscription => schema.SubscriptionType,
            _ => throw ThrowHelper.RootTypeNotSupported(operationType),
        };

    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var operationCompilerPool = core.Services.GetRequiredService<ObjectPool<OperationCompiler>>();
            var optimizers = core.SchemaServices.GetRequiredService<OperationCompilerOptimizers>();
            var middleware = new OperationResolverMiddleware(next, operationCompilerPool, optimizers);
            return context => middleware.InvokeAsync(context);
        };
}
