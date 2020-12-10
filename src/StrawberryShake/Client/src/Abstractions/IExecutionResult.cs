using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IExecutionResult
    {

    }

    public interface IObservableExecutionResult<T>
        : IExecutionResult
        , IAsyncObservable<IOperationResult<T>>
        where T : class
    {
    }

    public interface IAsyncObservable<out T> : IObservable<T>
    {
        ValueTask<IAsyncDisposable> SubscribeAsync(
            IObserver<string> observer,
            CancellationToken cancellationToken = default);
    }

    public interface IAsyncObserver<in T> : IObserver<T>
    {
        ValueTask OnCompletedAsync(CancellationToken cancellationToken = default);
        ValueTask OnErrorAsync(Exception error, CancellationToken cancellationToken = default);
        ValueTask OnNextAsync(T value, CancellationToken cancellationToken = default);
    }
}
