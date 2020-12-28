using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Impl
{
    internal class AsyncDelegateObserver<T> : IAsyncObserver<IOperationResult<T>> where T : class
    {
        private readonly Func<IOperationResult<T>, CancellationToken, ValueTask> _nextAsync;

        public AsyncDelegateObserver(
            Func<IOperationResult<T>, CancellationToken, ValueTask> nextAsync)
        {
            _nextAsync = nextAsync;
        }

        public ValueTask OnNextAsync(
            IOperationResult<T> value,
            CancellationToken cancellationToken = default) =>
            _nextAsync(value, cancellationToken);

        public ValueTask OnErrorAsync(
            Exception error,
            CancellationToken cancellationToken = default) =>
            default;

        public ValueTask OnCompletedAsync() =>
            default;
    }
}
