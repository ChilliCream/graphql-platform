using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;

namespace HotChocolate.Fetching
{
    public class SerialBatchScheduler
        : IBatchScheduler
        , IBatchDispatcher
    {
        private readonly ConcurrentQueue<Func<ValueTask>> _queue = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _serialMode;

        public bool HasTasks => _queue.Count > 0;

        public event EventHandler? TaskEnqueued;

        public void Schedule(Func<ValueTask> dispatch)
        {
            _queue.Enqueue(dispatch);
            TaskEnqueued?.Invoke(this, EventArgs.Empty);
        }

        public void Dispatch(Action<IExecutionTaskDefinition> enqueue)
        {
            _semaphore.Wait();

            try
            {
                if (_serialMode)
                {
                    return;
                }

                if (_queue.Count > 0)
                {
                    while (_queue.TryDequeue(out Func<ValueTask>? fetch))
                    {
                        enqueue(new SerialBatchExecutionTaskDefinition(fetch));
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DispatchAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_queue.Count > 0)
                {
                    while (_queue.TryDequeue(out Func<ValueTask>? fetch))
                    {
                        await fetch().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private class SerialBatchExecutionTaskDefinition : IExecutionTaskDefinition
        {
            private readonly Func<ValueTask> _fetch;

            public SerialBatchExecutionTaskDefinition(Func<ValueTask> fetch)
            {
                _fetch = fetch;
            }

            public IExecutionTask Create(IExecutionTaskContext context)
            {
                return new SerialBatchExecutionTask(context, _fetch);
            }
        }

        private class SerialBatchExecutionTask : SerialExecutionTask
        {
            private readonly Func<ValueTask> _fetch;

            public SerialBatchExecutionTask(IExecutionTaskContext context, Func<ValueTask> fetch)
            {
                Context = context;
                _fetch = fetch;
            }

            protected override IExecutionTaskContext Context { get; }

            protected override ValueTask ExecuteAsync(CancellationToken cancellationToken) =>
                _fetch();
        }
    }
}
