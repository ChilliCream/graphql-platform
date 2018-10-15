using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions
{
    public class InMemoryEventStream
        : IEventStream
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly IEventMessage _eventMessage;
        private TaskCompletionSource<IEventMessage> _taskCompletionSource =
            new TaskCompletionSource<IEventMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public event EventHandler Disposed;

        public InMemoryEventStream(IEventMessage eventMessage)
        {
            _eventMessage = eventMessage
                ?? throw new ArgumentNullException(nameof(eventMessage));
        }

        public bool IsCompleted { get; private set; }

        public void Trigger()
        {
            _semaphore.Wait();

            try
            {
                _taskCompletionSource.TrySetResult(_eventMessage);
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

        public void Dispose()
        {
            IsCompleted = true;
            Disposed?.Invoke(this, EventArgs.Empty);
        }
    }
}

