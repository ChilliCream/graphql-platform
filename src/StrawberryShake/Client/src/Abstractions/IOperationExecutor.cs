using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationExecutor<T> where T : class
    {
        Task<IOperationResult<T>> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default);

        IOperationObservable<T> Watch(
            OperationRequest request);
    }
}
