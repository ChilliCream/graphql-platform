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
    private readonly AsyncAutoResetEvent _signal = new();
    private readonly object _sync = new();
    private readonly HashSet<Batch> _enqueuedBatches = [];
    private readonly CancellationTokenSource _coordinatorCts = new();
    private readonly IDataLoaderDiagnosticEvents _diagnosticEvents;
    private readonly BatchDispatcherOptions _options;
    private List<Task>? _dispatchTasks;
    private int _openBatches;
    private long _lastSubscribed;
    private long _lastEnqueued;
    private int _isCoordinatorRunning;
    private ImmutableArray<ExecutorSession> _sessions = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchDispatcher"/> class.
    /// </summary>
    public BatchDispatcher(IDataLoaderDiagnosticEvents diagnosticEvents, BatchDispatcherOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _diagnosticEvents = diagnosticEvents;
        _options = options;
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
        if (_isCoordinatorRunning == 1)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _isCoordinatorRunning, 1, 0) == 0)
        {
            CoordinatorAsync(_coordinatorCts.Token).FireAndForget();
        }
    }

    private async Task CoordinatorAsync(CancellationToken stoppingToken)
    {
        using var scope = _diagnosticEvents.RunBatchDispatchCoordinator();

        var backlog = new PriorityQueue<Batch, long>();
        _dispatchTasks ??= new List<Task>(_options.MaxParallelBatches);

        Send(BatchDispatchEventType.CoordinatorStarted);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _signal;

                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                await EvaluateAndDispatchAsync(backlog, _dispatchTasks, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _diagnosticEvents.BatchDispatchError(ex);
            throw;
        }
        finally
        {
            Interlocked.Exchange(ref _isCoordinatorRunning, 0);
            Send(BatchDispatchEventType.CoordinatorCompleted);
        }
    }

    private async Task EvaluateAndDispatchAsync(
        PriorityQueue<Batch, long> backlog,
        List<Task> dispatchTasks,
        CancellationToken stoppingToken)
    {
        var noDispatchCycles = 0;

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

            EvaluateOpenBatches(ref lastModified, backlog, dispatchTasks);

            // If the evaluation selected batches for dispatch.
            if (dispatchTasks.Count > 0)
            {
                // We wait for all dispatch tasks to be completed before we reset the signal
                // that lets us pause the evaluation. Only then will we send a message to
                // the subscribed executors to reevaluate if they can continue execution.
                if (dispatchTasks.Count == 1)
                {
                    await dispatchTasks[0];
                }
                else
                {
                    if (_options.EnableParallelBatches)
                    {
                        await Task.WhenAll(dispatchTasks);
                    }
                    else
                    {
                        foreach (var task in dispatchTasks)
                        {
                            await task.ConfigureAwait(false);
                        }
                    }
                }

                _diagnosticEvents.BatchDispatched(dispatchTasks.Count, _options.EnableParallelBatches);
                _signal.TryResetToIdle();
                Send(BatchDispatchEventType.Dispatched);
                return;
            }

            // Signal that we have evaluated all enqueued tasks without dispatching any.
            _diagnosticEvents.BatchEvaluated(openBatches);
            Send(BatchDispatchEventType.Evaluated);

            // Spin-wait briefly to give executing resolvers time to add more
            // data requirements to the open batches.
            await WaitForMoreBatchActivityAsync(lastModified);

            // After 10 cycles without dispatch, we add a small delay to provide backpressure.
            if (noDispatchCycles >= 10)
            {
                await Task.Delay(10, stoppingToken);
                noDispatchCycles = 0;
            }

            noDispatchCycles++;
        }
    }

    private void EvaluateOpenBatches(
        ref long lastModified,
        PriorityQueue<Batch, long> backlog,
        List<Task> dispatchTasks)
    {
        Batch? singleBatch = null;

        // Fill the evaluation backlog with the current enqueued batches.
        // The backlog orders batches by ModifiedTimestamp, so batches that
        // haven't been touched by a DataLoader for the longest time are evaluated first,
        // giving them the highest likelihood of being dispatched.
        lock (_enqueuedBatches)
        {
            if (_enqueuedBatches.Count == 1)
            {
                singleBatch = _enqueuedBatches.First();
            }
            else
            {
                foreach (var batch in _enqueuedBatches)
                {
                    backlog.Enqueue(batch, batch.ModifiedTimestamp);
                }
            }
        }

        // In each evaluation round, we try to touch all batches in the backlog.
        // If a batch has had no interaction with a DataLoader since the last evaluation
        // (i.e., we can touch it twice without the DataLoader resetting its status),
        // we complete the batch by dispatching it.
        //
        // Additionally, if a batch has been waiting longer than maxBatchAgeUs,
        // we force dispatch it regardless of its status to prevent starvation
        // under continuous high load.
        //
        // We stop evaluation once we've dispatched MaxParallelBatches or when we have touched all batches.
        if (singleBatch is not null)
        {
            // we have an optimized path if there is only a single batch to evaluate.
            EvaluateSingleOpenBatches(ref lastModified, singleBatch, dispatchTasks);
        }
        else
        {
            EvaluateMultipleOpenBatches(ref lastModified, backlog, dispatchTasks);
        }
    }

    private void EvaluateMultipleOpenBatches(
        ref long lastModified,
        PriorityQueue<Batch, long> backlog,
        List<Task> dispatchTasks)
    {
        var now = Stopwatch.GetTimestamp();
        var maxBatchAgeUs = _options.MaxBatchWaitTimeUs;
        var maxParallelBatches = _options.MaxParallelBatches;

        while (backlog.TryDequeue(out var batch, out _))
        {
            if (lastModified < batch.ModifiedTimestamp)
            {
                lastModified = batch.ModifiedTimestamp;
            }

            var shouldDispatch = batch.Touch();

            if (!shouldDispatch && maxBatchAgeUs != 0)
            {
                shouldDispatch = TicksToUs(now - batch.CreatedTimestamp) > maxBatchAgeUs;
            }

            if (shouldDispatch)
            {
                lock (_enqueuedBatches)
                {
                    _enqueuedBatches.Remove(batch);
                }

                Interlocked.Decrement(ref _openBatches);
                dispatchTasks.Add(batch.DispatchAsync());
            }

            if (dispatchTasks.Count == maxParallelBatches)
            {
                break;
            }
        }
    }

    private void EvaluateSingleOpenBatches(
        ref long lastModified,
        Batch batch,
        List<Task> dispatchTasks)
    {
        var now = Stopwatch.GetTimestamp();

        if (lastModified < batch.ModifiedTimestamp)
        {
            lastModified = batch.ModifiedTimestamp;
        }

        var maxBatchAgeUs = _options.MaxBatchWaitTimeUs;
        var shouldDispatch = batch.Touch();

        if (!shouldDispatch && maxBatchAgeUs != 0)
        {
            shouldDispatch = TicksToUs(now - batch.CreatedTimestamp) > maxBatchAgeUs;
        }

        if (shouldDispatch)
        {
            lock (_enqueuedBatches)
            {
                _enqueuedBatches.Remove(batch);
            }

            Interlocked.Decrement(ref _openBatches);
            dispatchTasks.Add(batch.DispatchAsync());
        }
    }

    private async Task WaitForMoreBatchActivityAsync(long lastModified)
    {
        const int maxSpinUs = 50;

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
        if (ageUs <= maxSpinUs)
        {
            await Task.Yield();
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
    private static long TicksToUs(long ticks)
        => ticks * 1_000_000 / Stopwatch.Frequency;

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
