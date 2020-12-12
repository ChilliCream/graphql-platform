using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationObservable<T>
        : IObservable<IOperationResult<T>>
        , IAsyncObservable<IOperationResult<T>>
        where T : class
    {
        void Subscribe(
            Action<IOperationResult<T>> next,
            CancellationToken cancellationToken = default);

        void Subscribe(
            Func<IOperationResult<T>, CancellationToken, ValueTask> nextAsync,
            CancellationToken cancellationToken = default);
    }
}
