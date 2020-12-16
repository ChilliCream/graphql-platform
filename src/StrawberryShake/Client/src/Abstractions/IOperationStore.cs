using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationStore
    {
        ValueTask SetAsync<T>(
            OperationRequest operationRequest,
            IOperationResult<T> operationResult,
            CancellationToken cancellationToken = default)
            where T : class;

        bool TryGet<T>(OperationRequest operationRequest, [NotNullWhen(true)] out IOperationResult<T>? result) where T : class;

        IOperationObservable<T> Watch<T>(OperationRequest operationRequest) where T : class;
    }
}
