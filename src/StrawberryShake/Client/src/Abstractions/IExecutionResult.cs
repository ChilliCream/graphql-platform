using System;
using System.Collections.Generic;
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

    public interface IOperationExecutor<T> where T : class
    {
        Task<IOperationResult<T>> ExecuteAsync(
            IOperationRequest request,
            CancellationToken cancellationToken = default);
    }

    public interface IConnection<out TData> // JsonElement
    {
        IAsyncEnumerable<TData> ExecuteAsync(
            IOperationRequest request,
            CancellationToken cancellationToken = default);
    }
}
