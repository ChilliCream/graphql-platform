using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ObjectPool<OperationCompiler> _operationCompilerPool;
    private readonly IReadOnlyList<IOperationCompilerOptimizer>? _optimizers;

    public OperationResolverMiddleware(
        RequestDelegate next,
        ObjectPool<OperationCompiler> operationCompilerPool,
        IEnumerable<IOperationCompilerOptimizer> optimizers)
    {
        if (optimizers is null)
        {
            throw new ArgumentNullException(nameof(optimizers));
        }

        _next = next ?? throw new ArgumentNullException(nameof(next));
        _operationCompilerPool = operationCompilerPool;
        _optimizers = optimizers.ToArray();
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
                    context.Result = ErrorHelper.RootTypeNotFound(operationDef.Operation);
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
            context.Result = ErrorHelper.StateInvalidForOperationResolver();
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
            _optimizers);
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
            _ => throw ThrowHelper.RootTypeNotSupported(operationType)
        };
}
