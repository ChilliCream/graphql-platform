using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationStreamExecutor
    {
        Task<IResponseStream> ExecuteAsync<T>(
            IOperation operation,
            CancellationToken cancellationToken)
            where T : class;

        Task<IResponseStream<T>> ExecuteAsync<T>(
            IOperation<T> operation,
            CancellationToken cancellationToken)
            where T : class;
    }
}
