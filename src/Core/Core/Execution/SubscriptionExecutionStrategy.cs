using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class SubscriptionExecutionStrategy
        : ExecutionStrategyBase
    {
        private IRequestTimeoutOptionsAccessor _options;

        public SubscriptionExecutionStrategy(
            IRequestTimeoutOptionsAccessor options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _options = options;
        }

        public override Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            return ExecuteInternalAsync(executionContext, cancellationToken);
        }

        private async Task<IExecutionResult> ExecuteInternalAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            EventDescription eventDescription = CreateEvent(executionContext);

            IEventStream eventStream = await SubscribeAsync(
                executionContext.Services, eventDescription);

            return new SubscriptionResult(
                eventStream,
                msg =>
                {
                    IExecutionContext cloned = executionContext.Clone();
                    cloned.ContextData[typeof(IEventMessage).FullName] = msg;
                    return cloned;
                },
                ExecuteSubscriptionQueryAsync,
                executionContext.ServiceScope);
        }

        private EventDescription CreateEvent(
            IExecutionContext executionContext)
        {
            IReadOnlyCollection<FieldSelection> selections = executionContext
                .FieldHelper.CollectFields(
                    executionContext.Operation.RootType,
                    executionContext.Operation.Definition.SelectionSet);

            if (selections.Count == 1)
            {
                FieldSelection selection = selections.Single();
                Dictionary<string, ArgumentValue> argumentValues =
                    selection.CoerceArgumentValues(
                        executionContext.Variables);
                var arguments = new List<ArgumentNode>();

                foreach (KeyValuePair<string, ArgumentValue> argumentValue in
                    argumentValues)
                {
                    IInputType argumentType = argumentValue.Value.Type;
                    object value = argumentValue.Value.Value;

                    arguments.Add(new ArgumentNode(
                        argumentValue.Key,
                        argumentType.ParseValue(value)));
                }

                return new EventDescription(selection.Field.Name, arguments);
            }
            else
            {
                // TODO : Error message
                throw new QueryException();
            }
        }

        private Task<IEventStream> SubscribeAsync(
            IServiceProvider services,
            EventDescription @event)
        {
            IEventRegistry eventRegistry =
                (IEventRegistry)services.GetService(typeof(IEventRegistry));

            if (eventRegistry == null)
            {
                throw new QueryException(new QueryError(
                    "Register a event registry as service in order " +
                    "to use subsciptions."));
            }

            return eventRegistry.SubscribeAsync(@event);
        }

        private async Task<IReadOnlyQueryResult> ExecuteSubscriptionQueryAsync(
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
                    .CreateLinkedTokenSource(requestTimeoutCts.Token,
                        cancellationToken))
                {
                    IQueryResult result = await ExecuteQueryAsync(
                        executionContext,
                        batchOperationHandler,
                        cancellationToken);
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
