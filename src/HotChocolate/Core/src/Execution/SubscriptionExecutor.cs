using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal sealed class SubscriptionExecutor
        : IOperationExecutor
    {
        public Task<IExecutionResult> ExecuteAsync(
            IOperationContext executionContext,
            CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
