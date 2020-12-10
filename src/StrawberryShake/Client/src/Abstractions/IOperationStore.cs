using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationStore
    {
        ValueTask SetAsync<T>(
            IOperationRequest operationRequest,
            IOperationResult<T> operationResult,
            CancellationToken cancellationToken = default)
            where T : class;

        bool TryGet<T>(IOperationRequest operationRequest, [NotNullWhen(true)] out IOperationResult<T>? result) where T : class;

        IOperationObservable<T> Watch<T>(IOperationRequest operationRequest) where T : class;
    }
}
