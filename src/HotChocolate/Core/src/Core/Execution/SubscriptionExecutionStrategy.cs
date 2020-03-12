using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal sealed class SubscriptionExecutionStrategy
        : ExecutionStrategyBase
    {
        private readonly IRequestTimeoutOptionsAccessor _options;

        public SubscriptionExecutionStrategy(
            IRequestTimeoutOptionsAccessor options)
        {
            _options = options ??
                throw new ArgumentNullException(nameof(options));
        }

        public override Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            return ExecuteInternalAsync(executionContext);
        }

        private async Task<IExecutionResult> ExecuteInternalAsync(
            IExecutionContext executionContext)
        {
            object rootValue = executionContext.Operation.RootValue;

            FieldSelection fieldSelection = executionContext.CollectFields(
                executionContext.Schema.SubscriptionType,
                executionContext.Operation.Definition.SelectionSet,
                null)
                .Single();

            ImmutableStack<object> source = ImmutableStack.Create(rootValue);

            var subscribeContext = ResolverContext.Rent(
                executionContext,
                fieldSelection,
                source,
                new FieldData(1));

            SubscribeResolverDelegate subscribeResolver = fieldSelection.Field.SubscribeResolver;

            if (subscribeResolver is null)
            {
                throw new QueryException("The subscribe resolver is not configured.");
            }

            try
            {
                IAsyncEnumerable<object> sourceStream =
                    await subscribeResolver(subscribeContext)
                        .ConfigureAwait(false);

                return new SubscriptionResult(
                    sourceStream,
                    message =>
                    {
                        IExecutionContext cloned = executionContext.Clone();
                        cloned.ContextData[WellKnownContextData.EventMessage] = message;
                        return cloned;
                    },
                    ExecuteSubscriptionQueryAsync,
                    executionContext.ServiceScope,
                    executionContext.RequestAborted);
            }
            finally
            {
                ResolverContext.Return(subscribeContext);
            }
        }

        private async ValueTask<IReadOnlyQueryResult> ExecuteSubscriptionQueryAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            BatchOperationHandler batchOperationHandler =
                CreateBatchOperationHandler(executionContext);
            var requestTimeoutCts = new CancellationTokenSource(
                _options.ExecutionTimeout);

            try
            {
                using (var combinedCts = CancellationTokenSource
                    .CreateLinkedTokenSource(
                        requestTimeoutCts.Token,
                        cancellationToken))
                {
                    return await ExecuteQueryAsync(
                        executionContext,
                        batchOperationHandler,
                        cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                batchOperationHandler?.Dispose();
                requestTimeoutCts.Dispose();
            }
        }
    }
}
