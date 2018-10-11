using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Subscriptions;

namespace HotChocolate.Execution
{
    internal class SubscriptionExecutionStrategy
        : ExecutionStrategyBase
    {
        public override Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private object CreateSourceEventStream(
            IExecutionContext executionContext)
        {
            throw new NotImplementedException();
        }
    }
}
