using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using GreenDonut;
using HotChocolate.Utilities;

namespace HotChocolate.Fetching;

/// <summary>
/// <para>
/// The default batch dispatcher that coordinates efficient batch dispatching between DataLoader and
/// the Hot Chocolate execution engine. This dispatcher uses timing and prioritization strategies
/// to optimize batching efficiency while maintaining low latency.
/// </para>
/// <para>
/// The dispatcher processes batches based on their modification timestamps, prioritizing
/// batches that haven't received new keys for the longest time. This allows newer batches
/// to accumulate additional keys while older, "settled" batches are dispatched for
/// optimal throughput.
/// </para>
/// </summary>
public sealed partial class BatchDispatcher : IBatchDispatcher
{
    private const int MaxParallelBatches = 4;
    private readonly AsyncAutoResetEvent _signal = new();
    private readonly object _sync = new();
    private readonly HashSet<Batch> _enqueuedBatches = [];
    private readonly HashSet<Batch> _completedBatches = [];
    private readonly CancellationTokenSource _coordinatorCts = new();
    private int _openBatches;
    private long _lastSubscribed;
    private long _lastEnqueued;
    private int _isCoordinatorRunning;
    private ImmutableArray<ExecutorSession> _sessions = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchDispatcher"/> class.
    /// </summary>
    public BatchDispatcher()
    {
        _coordinatorCts.Token.Register(_signal.Set);
    }

    /// <summary>
    /// <para>Subscribe to the batch dispatcher events.</para>
    /// <para>
    /// Subscribers will be notified when the coordinator starts/completes and
    /// when batches are enqueued, evaluated and dispatched.
    /// </para>
    /// </summary>
    /// <param name="observer">
    /// The observer that will be notified. about dispatcher events.
    /// </param>
    /// <returns>
    /// Returns the subscription session that when being disposed will unsubscribe the observer.
    /// </returns>
    public IDisposable Subscribe(IObserver<BatchDispatchEventArgs> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return new ExecutorSession(observer, this);
    }

    /// <inheritdoc cref="IBatchDispatcher"/>
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
        var backlog = new PriorityQueue<Batch, long>();
        var dispatchTasks = new List<Task>(MaxParallelBatches);
        var completedBatches = new List<Batch>(MaxParallelBatches);

        Send(BatchDispatchEventType.CoordinatorStarted);

        try
        {
            while (!stoppingToken.IsCancellationRequested && !IsCompleted())
            {
                await _signal;

                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                await EvaluateAndDispatchAsync(backlog, dispatchTasks, completedBatches, stoppingToken);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isCoordinatorRunning, 0);
            Send(BatchDispatchEventType.CoordinatorCompleted);
        }

        bool IsCompleted()
        {
            var openBatches = Volatile.Read(ref _openBatches);
            return openBatches == 0;
        }
    }

    private async Task EvaluateAndDispatchAsync(
        PriorityQueue<Batch, long> backlog,
        List<Task> dispatchTasks,
        List<Batch> completedBatches,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var openBatches = Volatile.Read(ref _openBatches);
            long lastModified = 0;

            // If we have no open batches to evaluate, we can stop
            // and wait for another signal.
            if (openBatches == 0)
            {
                return;
            }

            // Clear the state for the next evaluation round.
            backlog.Clear();
            dispatchTasks.Clear();
            completedBatches.Clear();

            EvaluateOpenBatches(ref lastModified, backlog, dispatchTasks, completedBatches);

            // If the evaluation selected batches for dispatch.
            if (dispatchTasks.Count > 0)
            {
                // We wait for all dispatch tasks to be completed before we reset the signal
                // that lets us pause the evaluation. Only then will we send a message to
                // the subscribed executors to reevaluate if they can continue execution.
                await Task.WhenAll(dispatchTasks);
                _signal.TryResetToIdle();
                Send(BatchDispatchEventType.Dispatched);
                return;
            }

            // Signal that we have evaluated all enqueued tasks without dispatching any.
            Send(BatchDispatchEventType.Evaluated);

            // Spin-wait briefly to give executing resolvers time to add more
            // data requirements to the open batches.
            WaitForMoreBatchActivity(lastModified);
        }
    }

    private void EvaluateOpenBatches(
        ref long lastModified,
        PriorityQueue<Batch, long> backlog,
        List<Task> dispatchTasks,
        List<Batch> completedBatches)
    {
        // Fill the evaluation backlog with the current enqueued batches.
        // The backlog orders batches by ModifiedTimestamp, so batches that
        // haven't been touched by a DataLoader for the longest time are evaluated first,
        // giving them the highest likelihood of being dispatched.
        lock (_enqueuedBatches)
        {
            foreach (var batch in _enqueuedBatches)
            {
                backlog.Enqueue(batch, batch.ModifiedTimestamp);
            }
        }

        // In each evaluation round, we try to touch all batches in the backlog.
        // If a batch has had no interaction with a DataLoader since the last evaluation
        // (i.e., we can touch it twice without the DataLoader resetting its status),
        // we complete the batch by dispatching it.
        //
        // We stop evaluation once we've dispatched MaxParallelBatches or when we have touched all batches.
        while (backlog.TryDequeue(out var batch, out _))
        {
            if (lastModified < batch.ModifiedTimestamp)
            {
                lastModified = batch.ModifiedTimestamp;
            }

            if (batch.Touch())
            {
                completedBatches.Add(batch);
                dispatchTasks.Add(batch.DispatchAsync());
                Interlocked.Decrement(ref _openBatches);
            }

            if (dispatchTasks.Count == MaxParallelBatches)
            {
                break;
            }
        }

        // Remove batches that have been marked for dispatch from the enqueued list.
        // Note: Calling DispatchAsync() closes the batch, preventing DataLoaders from
        // enqueuing new keys. The _enqueuedBatches set only tracks batches available
        // for evaluation rounds; it doesn't control whether new keys can be enqueued.
        lock (_enqueuedBatches)
        {
            foreach (var completed in completedBatches)
            {
                _completedBatches.Add(completed);
                _enqueuedBatches.Remove(completed);
            }
        }
    }

    private void WaitForMoreBatchActivity(long lastModified)
    {
        const int maxSpinUs = 12;

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
}
