using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using GreenDonut;
using HotChocolate.Utilities;

namespace HotChocolate.Fetching;

/// <summary>
/// The execution engine batch dispatcher.
/// </summary>
public sealed partial class BatchDispatcher : IBatchDispatcher
{
    private readonly AsyncAutoResetEvent _signal = new();
    private readonly object _sync = new();
    private readonly List<Batch> _batches = [];
    private readonly CancellationTokenSource _coordinatorCts = new();
    private int _enqueueVersion;
    private int _openBatches;
    private long _lastSubscribed;
    private long _lastEnqueued;
    private int _isCoordinatorRunning;
    private ImmutableArray<ExecutorSession> _sessions = [];
    private bool _disposed;

    public IDisposable Subscribe(IObserver<BatchDispatchEventArgs> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return new ExecutorSession(observer, this);
    }

    public void BeginDispatch(CancellationToken cancellationToken = default)
    {
        _signal.Set();
        EnsureCoordinatorIsRunning();
    }

    private void EnsureCoordinatorIsRunning()
    {
        if (Interlocked.CompareExchange(ref _isCoordinatorRunning, 1, 0) == 0)
        {
            CoordinatorAsync(_coordinatorCts.Token).FireAndForget();
        }
    }

    private async Task CoordinatorAsync(CancellationToken stoppingToken)
    {
        const int maxSpinUs = 12;
        const int maxParallelBatches = 4;
        var dispatchTasks = new List<Task>(maxParallelBatches);
        var queue = new PriorityQueue<Batch, long>();

        while (!stoppingToken.IsCancellationRequested)
        {
            await _signal;

            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var openBatches = Volatile.Read(ref _openBatches);
                long lastModified = 0;

                if (openBatches == 0)
                {
                    break;
                }

                dispatchTasks.Clear();
                queue.Clear();

                lock (_batches)
                {
                    foreach (var batch in _batches)
                    {
                        queue.Enqueue(batch, batch.ModifiedTimestamp);
                    }

                    while (queue.TryDequeue(out var batch, out _))
                    {
                        if (lastModified < batch.ModifiedTimestamp)
                        {
                            lastModified = batch.ModifiedTimestamp;
                        }

                        if (batch.Touch())
                        {
                            dispatchTasks.Add(batch.DispatchAsync());
                            Interlocked.Decrement(ref _openBatches);
                            _batches.Remove(batch);
                        }

                        if (dispatchTasks.Count == 4)
                        {
                            break;
                        }
                    }
                }

                if (dispatchTasks.Count > 0)
                {
                    await Task.WhenAll(dispatchTasks);
                    Send(BatchDispatchEventType.Dispatched);
                    break;
                }

                // we signal to the execution engine that we have evaluated the task.
                // while we are doing this we spin for a while and then reevaluate
                Send(BatchDispatchEventType.Evaluated);

                var lastSubscribed = Volatile.Read(ref _lastSubscribed);
                var lastEnqueued = Volatile.Read(ref _lastEnqueued);

                if (lastSubscribed > lastModified)
                {
                    lastModified = lastSubscribed;
                }

                if (lastEnqueued > lastModified)
                {
                    lastModified = lastEnqueued;
                }

                var ageUs = TicksToUs(Stopwatch.GetTimestamp() - lastModified);
                if (ageUs <= maxSpinUs && ThreadPoolHasHeadroom())
                {
                    var sw = new SpinWait();
                    var start = Stopwatch.GetTimestamp();

                    while (TicksToUs(Stopwatch.GetTimestamp() - start) < maxSpinUs)
                    {
                        sw.SpinOnce();
                        if (sw.Count >= 8)
                        {
                            Thread.Yield();
                        }
                    }
                }
            }
        }
    }

    private void Send(BatchDispatchEventType type)
    {
        var sessions = _sessions;
        var eventAgs = new BatchDispatchEventArgs(type);

        foreach (var session in sessions)
        {
            session.OnNext(eventAgs);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long TicksToUs(long ticks) =>
        ticks * 1_000_000 / Stopwatch.Frequency;

    private static bool ThreadPoolHasHeadroom()
    {
        ThreadPool.GetAvailableThreads(out var worker, out _);
        return worker >= 2;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _coordinatorCts.Cancel();
            _coordinatorCts.Dispose();
            _signal.Set();
            _disposed = true;
        }
    }

    private class ExecutorSession : IDisposable
    {
        private readonly IObserver<BatchDispatchEventArgs> _observer;
        private readonly BatchDispatcher _dispatcher;
        private bool _disposed;

        public ExecutorSession(
            IObserver<BatchDispatchEventArgs> observer,
            BatchDispatcher dispatcher)
        {
            _observer = observer;
            _dispatcher = dispatcher;

            lock (dispatcher._sync)
            {
                dispatcher._sessions = dispatcher._sessions.Add(this);
                dispatcher._lastSubscribed = Stopwatch.GetTimestamp();
                dispatcher._signal.Set();
            }
        }

        public void OnNext(BatchDispatchEventArgs e)
        {
            _observer.OnNext(e);
        }

        public void OnCompleted()
        {
            _observer.OnCompleted();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_dispatcher._sync)
                {
                    _dispatcher._sessions = _dispatcher._sessions.Remove(this);
                }

                _disposed = true;
            }
        }
    }
}
