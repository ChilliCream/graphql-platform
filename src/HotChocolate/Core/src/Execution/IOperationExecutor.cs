using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal interface IOperationExecutor
    {
        Task<IExecutionResult> ExecuteAsync(
            IOperationContext executionContext,
            CancellationToken cancellationToken);
    }
}
