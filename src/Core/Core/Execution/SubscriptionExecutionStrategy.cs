using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class SubscriptionExecutionStrategy
        : ExecutionStrategyBase
    {
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
                message => executionContext.Clone(
                    new Dictionary<string, object> {
                        { typeof(IEventMessage).FullName, message } },
                    cancellationToken),
                ExecuteSubscriptionQueryAsync);
        }

        private EventDescription CreateEvent(
            IExecutionContext executionContext)
        {
            IReadOnlyCollection<FieldSelection> selections = executionContext
                .CollectFields(
                    executionContext.OperationType,
                    executionContext.Operation.SelectionSet);

            if (selections.Count == 1)
            {
                FieldSelection selection = selections.Single();

                Dictionary<string, ArgumentValue> argumentValues =
                    selection.CoerceArgumentValues(
                        executionContext.Variables);

                List<ArgumentNode> arguments = new List<ArgumentNode>();

                foreach (var argumentValue in argumentValues)
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

        private async Task<IQueryExecutionResult> ExecuteSubscriptionQueryAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            TimeSpan executionTimeout =
                executionContext.Schema.Options.ExecutionTimeout;

            var requestTimeoutCts =
                new CancellationTokenSource(executionTimeout);

            var combinedCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    requestTimeoutCts.Token, cancellationToken);

            try
            {
                return await ExecuteQueryAsync(
                    executionContext,
                    combinedCts.Token);
            }
            finally
            {
                combinedCts.Dispose();
                requestTimeoutCts.Dispose();
            }
        }
    }
}
