using System.Runtime.InteropServices.ComTypes;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Properties;
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
            EventDescription eventDescription = CreateEvent(executionContext);

            IEventStream eventStream = await SubscribeAsync(
                executionContext.Services, eventDescription)
                    .ConfigureAwait(false);

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

        private static EventDescription CreateEvent(
            IExecutionContext executionContext)
        {
            IReadOnlyCollection<FieldSelection> selections = executionContext
                .CollectFields(
                    executionContext.Operation.RootType,
                    executionContext.Operation.Definition.SelectionSet,
                    null);

            if (selections.Count == 1)
            {
                FieldSelection selection = selections.Single();
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

        private static Task<IEventStream> SubscribeAsync(
            IServiceProvider services,
            IEventDescription @event)
        {
            var eventRegistry = services.GetService<IEventRegistry>();

            if (eventRegistry == null)
            {
                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(CoreResources.SubscriptionExecutionStrategy_NoEventRegistry)
                        .Build());
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
