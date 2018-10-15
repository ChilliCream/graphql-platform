using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    public class InMemoryEventStream
        : IEventStream
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private TaskCompletionSource<object> _taskCompletionSource =
            new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public event EventHandler Disposed;

        public string SubscriptionId { get; } = Guid.NewGuid().ToString("N");

        public bool IsCompleted { get; private set; }

        public void Trigger()
        {
            _semaphore.Wait();

            try
            {
                _taskCompletionSource.TrySetResult(null);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task NextAsync(
            CancellationToken cancellationToken = default)
        {
            await _taskCompletionSource.Task;
            await _semaphore.WaitAsync();

            try
            {
                _taskCompletionSource = new TaskCompletionSource<object>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            IsCompleted = true;
            Disposed?.Invoke(this, EventArgs.Empty);
        }
    }
}

