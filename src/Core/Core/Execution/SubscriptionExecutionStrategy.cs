using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;

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
                new Dictionary<string, object>());

            SubscribeResolverDelegate subscribeResolver =
                fieldSelection.Field.SubscribeResolver
                ?? DefaultSubscribeResolverAsync;

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

        private static async ValueTask<IAsyncEnumerable<object>> DefaultSubscribeResolverAsync(
            IResolverContext resolverContext)
        {
            EventDescription eventDescription = CreateEvent(resolverContext);
            IServiceProvider services = resolverContext.Service<IServiceProvider>();
            IEventRegistry eventRegistry = services.GetService<IEventRegistry>();

            if (eventRegistry == null)
            {
                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(CoreResources.SubscriptionExecutionStrategy_NoEventRegistry)
                        .Build());
            }

            return await eventRegistry.SubscribeAsync(eventDescription);
        }

        private static EventDescription CreateEvent(
            IResolverContext executionContext)
        {
            IReadOnlyList<IFieldSelection> selections =
                executionContext.CollectFields(
                    executionContext.RootType,
                    executionContext.Operation.SelectionSet);

            if (selections.Count == 1)
            {
                IFieldSelection selection = selections[0];
                var arguments = new List<ArgumentNode>();
                IVariableValueCollection variables = executionContext.Variables;

                foreach (ArgumentNode argument in selection.Selection.Arguments)
                {
                    if (argument.Value is VariableNode v)
                    {
                        IValueNode value = variables.GetVariable<IValueNode>(v.Name.Value);
                        arguments.Add(argument.WithValue(value));
                    }
                    else
                    {
                        arguments.Add(argument);
                    }
                }

                return new EventDescription(selection.Field.Name, arguments);
            }
            else
            {
                throw new QueryException(CoreResources.Subscriptions_SingleRootField);
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
                    IQueryResult result = await ExecuteQueryAsync(
                        executionContext,
                        batchOperationHandler,
                        cancellationToken)
                        .ConfigureAwait(false);

                    return result.AsReadOnly();
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
