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
        public override async Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            Event @event = CreateEvent(executionContext);

            IEventStream eventStream = await SubscribeAsync(
                executionContext.Services, @event);

            return new SubscriptionResult(
                eventStream, executionContext.Clone(default),
                ExecuteSubscriptionQueryAsync);
        }

        private object CreateSourceEventStream(
            IExecutionContext executionContext)
        {
            throw new NotImplementedException();
        }

        private Event CreateEvent(IExecutionContext executionContext)
        {
            IReadOnlyCollection<FieldSelection> selections = executionContext
                .CollectFields(
                    executionContext.OperationType,
                    executionContext.Operation.SelectionSet);

            if (selections.Count == 1)
            {
                FieldSelection selection = selections.Single();

                var argumentResolver = new ArgumentResolver();
                Dictionary<string, ArgumentValue> argumentValues =
                    argumentResolver.CoerceArgumentValues(
                        selection,
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

                return new Event(selection.Field.Name, arguments);
            }
            else
            {
                // TODO : Error message
                throw new QueryException();
            }
        }

        private Task<IEventStream> SubscribeAsync(
            IServiceProvider services,
            Event @event)
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

            IExecutionContext internalContext =
                executionContext.Clone(combinedCts.Token);

            try
            {
                return await ExecuteQueryAsync(
                    internalContext, combinedCts.Token);
            }
            finally
            {
                executionContext.Dispose();
                combinedCts.Dispose();
                requestTimeoutCts.Dispose();
            }
        }
    }
}
