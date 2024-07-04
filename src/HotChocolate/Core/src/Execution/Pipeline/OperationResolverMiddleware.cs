using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ErrorHelper;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ObjectPool<OperationCompiler> _operationCompilerPool;
    private readonly IOperationCompilerOptimizer[]? _optimizers;

    private OperationResolverMiddleware(
        RequestDelegate next,
        ObjectPool<OperationCompiler> operationCompilerPool,
        IEnumerable<IOperationCompilerOptimizer> optimizers)
    {
        if (optimizers is null)
        {
            throw new ArgumentNullException(nameof(optimizers));
        }

        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _operationCompilerPool = operationCompilerPool ??
            throw new ArgumentNullException(nameof(operationCompilerPool));
        _optimizers = optimizers.ToArray();

        if (_optimizers.Length == 0)
        {
            _optimizers = null;
        }
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
        var compiler = _operationCompilerPool.Get();
        var operation = compiler.Compile(
            operationId,
            operationDefinition,
            operationType,
            context.Document!,
            context.Schema,
            _optimizers,
            IsNullBubblingEnabled(context));
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

    private bool IsNullBubblingEnabled(IRequestContext context)
        => !context.Schema.ContextData.ContainsKey(EnableTrueNullability) || 
            !context.ContextData.ContainsKey(EnableTrueNullability);

    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var operationCompilerPool = core.Services.GetRequiredService<ObjectPool<OperationCompiler>>();
            var optimizers1 = core.Services.GetRequiredService<IEnumerable<IOperationCompilerOptimizer>>();
            var optimizers2 = core.SchemaServices.GetRequiredService<IEnumerable<IOperationCompilerOptimizer>>();
            optimizers1 = optimizers1.Concat(optimizers2);
            var middleware = new OperationResolverMiddleware(next, operationCompilerPool, optimizers1);
            return context => middleware.InvokeAsync(context);
        };
}