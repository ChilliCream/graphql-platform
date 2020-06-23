using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class SubscriptionExecutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly SubscriptionExecutor _subscriptionExecutor;

        public SubscriptionExecutionMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
            SubscriptionExecutor subscriptionExecutor)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _subscriptionExecutor = subscriptionExecutor ??
                throw new ArgumentNullException(nameof(subscriptionExecutor));
        }

        public async ValueTask InvokeAsync(
            IRequestContext context,
            IBatchDispatcher batchDispatcher)
        {
            if (context.Operation is { } && context.Variables is { })
            {
                if (context.Operation.Definition.Operation == OperationType.Subscription)
                {
                    context.Result = await _subscriptionExecutor
                        .ExecuteAsync(context)
                        .ConfigureAwait(false);
                }

                await _next(context).ConfigureAwait(false);
            }
            else
            {
                context.Result = ErrorHelper.StateInvalidForSubscriptionExecution();
            }
        }
    }
}
