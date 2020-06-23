using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
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
        private object? _cachedQueryValue = null;
        private object? _cachedMutation = null;

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

        public async ValueTask InvokeAsync(
            IRequestContext context,
            IBatchDispatcher batchDispatcher)
        {
            if (context.Operation is { } && context.Variables is { })
            {
                OperationContext operationContext = _operationContextPool.Get();

                try
                {
                    if (context.Operation.Definition.Operation == OperationType.Query)
                    {
                        object? query = RootValueResolver.TryResolve(
                            context,
                            context.Services,
                            context.Operation.RootType,
                            ref _cachedQueryValue);

                        operationContext.Initialize(
                            context,
                            context.Services,
                            batchDispatcher,
                            context.Operation,
                            query,
                            context.Variables);

                        context.Result = await _queryExecutor
                            .ExecuteAsync(operationContext)
                            .ConfigureAwait(false);
                    }
                    else if (context.Operation.Definition.Operation == OperationType.Mutation)
                    {
                        object? mutation = RootValueResolver.TryResolve(
                            context,
                            context.Services,
                            context.Operation.RootType,
                            ref _cachedMutation);

                        operationContext.Initialize(
                            context,
                            context.Services,
                            batchDispatcher,
                            context.Operation,
                            mutation,
                            context.Variables);

                        context.Result = await _mutationExecutor
                            .ExecuteAsync(operationContext)
                            .ConfigureAwait(false);
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
                context.Result = ErrorHelper.StateInvalidForOperationExecution();
            }
        }
    }
}
