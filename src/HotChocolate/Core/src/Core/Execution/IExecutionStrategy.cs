using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal interface IExecutionStrategy
    {
        Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken);
    }
}
