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
    private List<Task>? _inFlightDispatches;
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

        // Guard against `default(BatchDispatcherOptions)` which zeroes all fields,
        // bypassing the struct's field initializers and silently disabling age-based
        // forced dispatch.
        if (options.MaxBatchWaitTimeUs == 0)
        {
            options.MaxBatchWaitTimeUs = 50_000;
        }

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
        _dispatchTasks ??= new List<Task>(4);
        _inFlightDispatches ??= new List<Task>(4);

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

                await EvaluateAndDispatchAsync(
                    backlog,
                    _dispatchTasks,
                    _inFlightDispatches,
                    stoppingToken);
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
        List<Task> inFlightDispatches,
        CancellationToken stoppingToken)
    {
        var idleCycles = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var completedDispatches = await CompleteInFlightDispatchesAsync(inFlightDispatches)
                .ConfigureAwait(false);

            if (completedDispatches > 0)
            {
                _diagnosticEvents.BatchDispatched(completedDispatches);
                Send(BatchDispatchEventType.Dispatched);
                idleCycles = 0;
            }

            var openBatches = Volatile.Read(ref _openBatches);
            long lastModified = 0;

            // If we have no open batches to evaluate and all in-flight dispatches
            // are completed, we can stop and wait for another signal.
            // If there are in-flight dispatches still running we also stop and
            // wait for their completion signal.
            if (openBatches == 0)
            {
                return;
            }

            // Clear the state for the next evaluation round.
            backlog.Clear();
            dispatchTasks.Clear();

            EvaluateOpenBatches(ref lastModified, backlog, dispatchTasks);

            // If the evaluation selected batches for dispatch, we register them
            // as in-flight and continue evaluation without waiting for completion.
            if (dispatchTasks.Count > 0)
            {
                RegisterInFlightDispatches(dispatchTasks, inFlightDispatches);
                idleCycles = 0;
                continue;
            }

            // Signal that we have evaluated all enqueued tasks without dispatching any.
            _diagnosticEvents.BatchEvaluated(openBatches);
            Send(BatchDispatchEventType.Evaluated);

            // Spin-wait briefly to give executing resolvers time to add more
            // data requirements to the open batches.
            await WaitForMoreBatchActivityAsync(lastModified);

            // After 10 cycles without dispatch, insert a delay to avoid busy-spinning.
            // The first 10 cycles run tight (only the conditional yield in
            // WaitForMoreBatchActivityAsync) to give resolvers time to fill batches.
            if (idleCycles++ >= 10)
            {
                await Task.Delay(10, stoppingToken);
                idleCycles = 0;
            }
        }
    }

    private void RegisterInFlightDispatches(
        List<Task> dispatchTasks,
        List<Task> inFlightDispatches)
    {
        foreach (var dispatchTask in dispatchTasks)
        {
            if (!dispatchTask.IsCompleted)
            {
                _ = dispatchTask.ContinueWith(
                    static (_, state) => ((AsyncAutoResetEvent)state!).Set(),
                    _signal,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            inFlightDispatches.Add(dispatchTask);
        }
    }

    private static async Task<int> CompleteInFlightDispatchesAsync(List<Task> inFlightDispatches)
    {
        var completedDispatches = 0;

        for (var i = inFlightDispatches.Count - 1; i >= 0; i--)
        {
            var dispatchTask = inFlightDispatches[i];

            if (!dispatchTask.IsCompleted)
            {
                continue;
            }

            await dispatchTask.ConfigureAwait(false);
            inFlightDispatches.RemoveAt(i);
            completedDispatches++;
        }

        return completedDispatches;
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
        // We stop evaluation when we have touched all batches.
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
