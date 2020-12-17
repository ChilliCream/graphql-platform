using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    // Transport
    // Execute => Create Connection => Execute Request => Response = Stream Results => foreach Result Builder

    // Result Builder (kennt stores)


    public interface IExecutionResult
    {
    }

    public interface IAsyncObservable<out T>
    {
        ValueTask<IAsyncDisposable> SubscribeAsync(
            IAsyncObserver<T> observer,
            CancellationToken cancellationToken = default);
    }

    public interface IAsyncObserver<in T>
    {
        ValueTask OnCompletedAsync();
        ValueTask OnErrorAsync(Exception error, CancellationToken cancellationToken = default);
        ValueTask OnNextAsync(T value, CancellationToken cancellationToken = default);
    }

    public interface IOperationExecutor<T> where T : class
    {
        Task<IOperationResult<T>> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default);
    }
}
