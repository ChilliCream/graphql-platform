using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IOperationObservable<out TResultData>
        : IObservable<IOperationResult<TResultData>>
        , IAsyncObservable<IOperationResult<TResultData>>
        where TResultData : class
    {
        void Subscribe(
            Action<IOperationResult<TResultData>> next,
            CancellationToken cancellationToken = default);

        void Subscribe(
            Func<IOperationResult<TResultData>, CancellationToken, ValueTask> nextAsync,
            CancellationToken cancellationToken = default);
    }
}
