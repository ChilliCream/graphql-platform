using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// Runs asynchronous effects off the TUI event-loop thread and reports their outcome
/// back onto it through <see cref="DrainCompletions"/>.
/// </summary>
/// <typeparam name="TResult">The value one effect produces on success.</typeparam>
internal sealed class TuiEffectQueue<TResult>
{
    private readonly ConcurrentQueue<TuiEffectCompletion<TResult>> _completions = new();
    private readonly ConcurrentDictionary<string, byte> _inFlightKeys = new();
    private readonly ConcurrentDictionary<TuiOperationId, Task> _inFlight = new();
    private readonly SemaphoreSlim _wakeSignal = new(0);
    private volatile bool _accepting = true;

    /// <summary>
    /// Assigns an operation ID and starts <paramref name="effect"/> on a background
    /// task, returning immediately without waiting for it to finish. Returns
    /// <see langword="false"/> without starting anything once <see cref="StopAccepting"/>
    /// has been called, or while an effect submitted under <paramref name="dedupeKey"/>
    /// is already in flight.
    /// </summary>
    /// <param name="dedupeKey">
    /// Identifies the logical submission slot (for example one form) that must not
    /// have two effects running at once; a resubmission under the same key while one
    /// is in flight is rejected rather than queued.
    /// </param>
    /// <param name="effect">
    /// The work to run, receiving its own assigned <see cref="TuiOperationId"/> and the
    /// cancellation token passed in <paramref name="cancellationToken"/>. The queue
    /// never cancels an effect on its own initiative.
    /// </param>
    /// <param name="cancellationToken">
    /// Passed through to <paramref name="effect"/> unchanged.
    /// </param>
    /// <param name="operationId">
    /// The ID assigned to this submission, or the default value when nothing started.
    /// </param>
    public bool TrySubmit(
        string dedupeKey,
        Func<TuiOperationId, CancellationToken, Task<TResult>> effect,
        CancellationToken cancellationToken,
        out TuiOperationId operationId)
    {
        ArgumentNullException.ThrowIfNull(dedupeKey);
        ArgumentNullException.ThrowIfNull(effect);

        if (!_accepting || !_inFlightKeys.TryAdd(dedupeKey, 0))
        {
            operationId = default;
            return false;
        }

        var assignedId = TuiOperationId.New();
        operationId = assignedId;
        var runTask = Task.Run(
            () => RunEffectAsync(assignedId, dedupeKey, effect, cancellationToken), CancellationToken.None);

        // Inserted before the continuation below is attached, removed only by that continuation.
        _inFlight[assignedId] = runTask;
        runTask.ContinueWith(
            delegate
            { _inFlight.TryRemove(assignedId, out _); },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return true;
    }

    private async Task RunEffectAsync(
        TuiOperationId operationId,
        string dedupeKey,
        Func<TuiOperationId, CancellationToken, Task<TResult>> effect,
        CancellationToken cancellationToken)
    {
        TuiEffectCompletion<TResult> completion;

        try
        {
            var result = await effect(operationId, cancellationToken).ConfigureAwait(false);
            completion = new TuiEffectCompletion<TResult>.Completed(operationId, result);
        }
        catch (OperationCanceledException)
        {
            completion = new TuiEffectCompletion<TResult>.Cancelled(operationId);
        }
        catch (Exception exception)
        {
            completion = new TuiEffectCompletion<TResult>.Faulted(operationId, exception);
        }

        // Persisted before the dedupe key is freed and the wake signal is released.
        _completions.Enqueue(completion);
        _inFlightKeys.TryRemove(dedupeKey, out _);
        _wakeSignal.Release();
    }

    /// <summary>
    /// Drains and returns every completion persisted since the last call. Safe to call
    /// from any event handler, not only in response to <see cref="TuiEvent.EffectCompletedEvent"/>.
    /// </summary>
    public IReadOnlyList<TuiEffectCompletion<TResult>> DrainCompletions()
    {
        if (_completions.IsEmpty)
        {
            return [];
        }

        var drained = new List<TuiEffectCompletion<TResult>>();

        while (_completions.TryDequeue(out var completion))
        {
            drained.Add(completion);
        }

        return drained;
    }

    /// <summary>
    /// Releases one pending wake signal without enqueuing a completion, causing
    /// <see cref="RunAsync"/> to relay one <see cref="TuiEvent.EffectCompletedEvent"/>.
    /// </summary>
    public void SignalWake() => _wakeSignal.Release();

    /// <summary>
    /// Relays one wake event per completed effect, and per <see cref="SignalWake"/>
    /// call, onto <paramref name="writer"/>.
    /// </summary>
    public async Task RunAsync(ChannelWriter<TuiEvent> writer, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await _wakeSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
                writer.TryWrite(new TuiEvent.EffectCompletedEvent());
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }

    /// <summary>
    /// Stops accepting new submissions; every later <see cref="TrySubmit"/> call
    /// returns <see langword="false"/>. Used by the pre-cancellation quit gate before
    /// it drains what is already in flight. Idempotent.
    /// </summary>
    public void StopAccepting() => _accepting = false;

    /// <summary>
    /// Reverses <see cref="StopAccepting"/> after a cancelled quit; idempotent.
    /// </summary>
    public void ResumeAccepting() => _accepting = true;

    /// <summary>
    /// The number of effects submitted but not yet completed.
    /// </summary>
    public int PendingCount => _inFlight.Count;

    /// <summary>
    /// The operation IDs of every effect submitted but not yet completed.
    /// </summary>
    public IReadOnlyList<TuiOperationId> PendingOperationIds => [.. _inFlight.Keys];

    /// <summary>
    /// Waits for every effect in flight at the time of the call to complete, bounded by
    /// <paramref name="bound"/>; <see cref="PendingCount"/> reports what is left
    /// afterward. Never cancels the effects themselves.
    /// </summary>
    public async Task DrainPendingAsync(TimeSpan bound, CancellationToken cancellationToken)
    {
        var pending = _inFlight.Values.ToArray();

        if (pending.Length == 0)
        {
            return;
        }

        try
        {
            await Task.WhenAll(pending).WaitAsync(bound, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // cancellationToken fired; PendingCount reports what is left.
        }
        catch (TimeoutException)
        {
            // bound elapsed; PendingCount reports what is left.
        }
    }
}
