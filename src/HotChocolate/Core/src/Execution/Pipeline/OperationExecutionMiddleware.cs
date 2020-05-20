using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Fetching;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class OperationExecutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly ObjectPool<OperationContext> _operationContextPool;
        private readonly QueryExecutor _queryExecutor;
        private readonly MutationExecutor _mutationExecutor;
        private object? _cachedRootValue = null;

        public OperationExecutionMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
            ObjectPool<OperationContext> operationContextPool,
            QueryExecutor queryExecutor,
            MutationExecutor mutationExecutor)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _operationContextPool = operationContextPool ??
                throw new ArgumentNullException(nameof(operationContextPool));
            _queryExecutor = queryExecutor ??
                throw new ArgumentNullException(nameof(queryExecutor));
            _mutationExecutor = mutationExecutor ??
                throw new ArgumentNullException(nameof(mutationExecutor));
        }

        public async Task InvokeAsync(IRequestContext context, IBatchDispatcher batchDispatcher)
        {
            if (context.Operation is { } && context.Variables is { })
            {
                OperationContext operationContext = _operationContextPool.Get();

                try
                {
                    operationContext.Initialize(
                        context,
                        batchDispatcher,
                        context.Operation,
                        ResolveRootValue(context, context.Operation.RootType),
                        context.Variables);

                    switch (context.Operation.Definition.Operation)
                    {
                        case OperationType.Query:
                            context.Result = await _queryExecutor
                                .ExecuteAsync(operationContext, context.RequestAborted)
                                .ConfigureAwait(false);
                            break;

                        case OperationType.Mutation:
                            context.Result = await _mutationExecutor
                                .ExecuteAsync(operationContext, context.RequestAborted)
                                .ConfigureAwait(false);
                            break;

                        case OperationType.Subscription:
                            throw new NotSupportedException();

                        default:
                            // TODO : ERRORHELPER
                            throw new NotSupportedException();
                    }

                    await _next(context).ConfigureAwait(false);
                }
                finally
                {
                    _operationContextPool.Return(operationContext);
                }
            }
            else
            {
                // TODO : ERRORHELPER
                context.Result = QueryResultBuilder.CreateError((IError)null);
            }
        }

        private object? ResolveRootValue(IRequestContext context, ObjectType rootType)
        {
            if (context.Request.InitialValue is { })
            {
                return context.Request.InitialValue;
            }

            if (_cachedRootValue is { })
            {
                return _cachedRootValue;
            }

            if (rootType.ClrType != typeof(object))
            {
                object? rootValue = context.Services.GetService(rootType.ClrType);

                if (rootValue is null &&
                    !rootType.ClrType.IsAbstract &&
                    !rootType.ClrType.IsInterface)
                {
                    _cachedRootValue = rootValue = context.Activator.CreateInstance(rootType.ClrType);
                }

                return rootValue;
            }

            return null;
        }
    }
}
