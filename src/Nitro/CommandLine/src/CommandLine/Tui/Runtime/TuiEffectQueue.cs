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
    /// Starts the effect on a background task and returns its assigned operation id
    /// without waiting for completion. Returns false while submissions are stopped
    /// or an effect with the same deduplication key is in flight.
    /// </summary>
    /// <param name="dedupeKey">
    /// Identifies a submission slot that permits at most one running effect;
    /// a concurrent submission with the same key is rejected.
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

        // The completion is queued before the submission slot is freed and signaled.
        _completions.Enqueue(completion);
        _inFlightKeys.TryRemove(dedupeKey, out _);
        _wakeSignal.Release();
    }

    /// <summary>
    /// Removes and returns all currently queued completions.
    /// May be called independently of wake events.
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
    /// Signals the event source without adding a completion.
    /// </summary>
    public void SignalWake() => _wakeSignal.Release();

    /// <summary>
    /// Attempts to write a wake event for each completed effect and each
    /// <see cref="SignalWake"/> call until cancellation.
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
    /// Rejects new submissions until <see cref="ResumeAccepting"/> is called,
    /// without cancelling effects already in flight.
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
    /// Waits for the effects currently in flight until they finish, the bound expires,
    /// or the wait is cancelled. Does not cancel the effects themselves.
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
