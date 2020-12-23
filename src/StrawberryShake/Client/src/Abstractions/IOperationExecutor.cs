using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationExecutor<TResultData> where TResultData : class
    {
        Task<IOperationResult<TResultData>> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default);

        IOperationObservable<TResultData> Watch(
            OperationRequest request,
            ExecutionStrategy? strategy = null);
    }
}
