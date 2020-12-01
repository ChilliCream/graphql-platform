using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationExecutor
    {
        Task<IOperationResult> ExecuteAsync(
            IOperation operation,
            CancellationToken cancellationToken);

        Task<IOperationResult<T>> ExecuteAsync<T>(
            IOperation<T> operation,
            CancellationToken cancellationToken)
            where T : class;
    }
}
