using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    public class InMemoryEventStream
        : IEventStream
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private TaskCompletionSource<IEventMessage> _taskCompletionSource =
            new TaskCompletionSource<IEventMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public event EventHandler Completed;

        public bool IsCompleted { get; private set; }

        public void Trigger(IEventMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _semaphore.Wait();

            try
            {
                _taskCompletionSource.TrySetResult(message);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<IEventMessage> ReadAsync()
        {
            return ReadAsync(CancellationToken.None);
        }

        public async Task<IEventMessage> ReadAsync(
            CancellationToken cancellationToken)
        {
            IEventMessage message = await _taskCompletionSource.Task;
            await _semaphore.WaitAsync();

            try
            {
                _taskCompletionSource = new TaskCompletionSource<IEventMessage>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }
            finally
            {
                _semaphore.Release();
            }

            return message;
        }

        public Task CompleteAsync()
        {
            IsCompleted = true;
            Completed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            IsCompleted = true;
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }
}

