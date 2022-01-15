using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.OperationCompiler;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IReadOnlyList<ISelectionOptimizer>? _optimizers;
    private readonly InputParser _inputParser;

    public OperationResolverMiddleware(
        RequestDelegate next,
        InputParser inputParser,
        IEnumerable<ISelectionOptimizer> optimizers)
    {
        if (optimizers is null)
        {
            throw new ArgumentNullException(nameof(optimizers));
        }

        _next = next ?? throw new ArgumentNullException(nameof(next));
        _inputParser = inputParser ?? throw new ArgumentNullException(nameof(inputParser));
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
            OperationDefinitionNode operation =
                context.Document.GetOperation(context.Request.OperationName);

            ObjectType? rootType = ResolveRootType(operation.Operation, context.Schema);

            if (rootType is null)
            {
                context.Result = ErrorHelper.RootTypeNotFound(operation.Operation);
                return;
            }

            context.Operation = Compile(
                context.OperationId ?? Guid.NewGuid().ToString("N"),
                context.Document,
                operation,
                context.Schema,
                rootType,
                _inputParser,
                _optimizers);
            context.OperationId = context.Operation.Id;

            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = ErrorHelper.StateInvalidForOperationResolver();
        }
    }

    private static ObjectType? ResolveRootType(
        OperationType operationType, ISchema schema) =>
        operationType switch
        {
            OperationType.Query => schema.QueryType,
            OperationType.Mutation => schema.MutationType,
            OperationType.Subscription => schema.SubscriptionType,
            _ => throw ThrowHelper.RootTypeNotSupported(operationType)
        };
}
