using HotChocolate.Fusion.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// Owns the registered <see cref="IRegoDataProvider"/> instances, keeps their last known good data
/// snapshot, and produces the merged data document <see cref="RegoPolicyProvider"/> compiles the
/// policy set with.
/// </summary>
/// <remarks>
/// Every provider is refreshed independently: a provider's own change token drives its own
/// refresh, concurrent refresh requests for the same provider are coalesced into at most one
/// follow-up run, a refresh that throws or times out keeps the provider's last good snapshot, and
/// nothing is published while the aggregator is disposed.
/// </remarks>
internal sealed class RegoDataAggregator : IAsyncDisposable
{
    internal static readonly TimeSpan DefaultRefreshTimeout = TimeSpan.FromSeconds(30);

    private const long SizeWarningThresholdBytes = 16 * 1024 * 1024;

    private readonly ProviderState[] _providers;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly ILogger _logger;
    private readonly TimeSpan _refreshTimeout;

    // Canceled (never disposed) on disposal so a refresh that is awaiting a slow provider is not
    // left running (and cannot dispose-race a factory-owned provider instance): every refresh's
    // timeout token is linked to this one. Leaving it undisposed means a refresh that is only just
    // starting on another thread can still safely link to and read its token concurrently with
    // DisposeAsync, instead of risking an ObjectDisposedException.
    private readonly CancellationTokenSource _disposalSource = new();
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private bool _disposed;

    public RegoDataAggregator(
        IReadOnlyList<RegoDataProviderRegistration> registrations,
        IServiceProvider services,
        IFusionExecutionDiagnosticEvents diagnosticEvents,
        ILogger logger,
        TimeSpan? refreshTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);
        ArgumentNullException.ThrowIfNull(logger);

        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var registration in registrations)
        {
            if (!seenNames.Add(registration.Name))
            {
                throw new InvalidOperationException(
                    $"A Rego data provider named '{registration.Name}' is already registered. "
                    + "Provider names must be unique on a gateway.");
            }
        }

        _diagnosticEvents = diagnosticEvents;
        _logger = logger;
        _refreshTimeout = refreshTimeout ?? DefaultRefreshTimeout;

        _providers = new ProviderState[registrations.Count];

        for (var i = 0; i < registrations.Count; i++)
        {
            var registration = registrations[i];
            _providers[i] = new ProviderState(
                registration.Name,
                registration.Factory(services),
                registration.OwnsInstance);
        }
    }

    /// <summary>
    /// Raised whenever a provider publishes a data snapshot that differs from the one it last
    /// published, once every provider has loaded at least once.
    /// </summary>
    public event Action? DataChanged;

    /// <summary>
    /// Subscribes to every provider's change token and kicks off its initial load. Never blocks:
    /// every refresh runs on the thread pool.
    /// </summary>
    public void Start()
    {
        foreach (var state in _providers)
        {
            state.ChangeSubscription = ChangeToken.OnChange(
                state.Instance.GetChangeToken,
                () => ScheduleRefresh(state));
            ScheduleRefresh(state);
        }
    }

    /// <summary>
    /// Attempts to build the merged data document from the FAR data document and every provider's
    /// last known good snapshot. Before doing so, retries promoting every provider's pending
    /// candidate (a freshly fetched snapshot held back because it previously collided) against the
    /// given FAR data and the other providers' current snapshots, so a provider-side fix or a new
    /// FAR publish resolves a collision without the provider needing to refresh again.
    /// </summary>
    public RegoDataMergeStatus TryBuildMergedData(
        byte[] farData,
        out byte[]? merged,
        out Exception? error)
    {
        merged = null;
        error = null;

        List<ReadOnlyMemory<byte>> documents;

        lock (_sync)
        {
            ReconcilePendingSnapshots(farData);

            documents = new List<ReadOnlyMemory<byte>>(_providers.Length + 1) { farData };

            foreach (var state in _providers)
            {
                if (state.Snapshot is { } snapshot)
                {
                    documents.Add(snapshot.Data);
                    continue;
                }

                if (state.PendingSnapshot is null)
                {
                    // Genuinely still waiting for this provider's first response.
                    return RegoDataMergeStatus.NotReady;
                }

                // The provider has responded at least once, but every attempt so far has
                // collided: there is no last-good to fall back to yet, so fail instead of
                // waiting forever.
                documents.Add(state.PendingSnapshot.Data);
            }
        }

        try
        {
            merged = RegoDataMerge.Merge(documents);
        }
        catch (RegoDataMergeException ex)
        {
            error = ex;
            return RegoDataMergeStatus.Failed;
        }

        if (merged.Length > SizeWarningThresholdBytes)
        {
            _logger.LogWarning(
                "The merged Rego data document is {SizeInBytes} bytes, which exceeds the "
                + "{ThresholdInBytes} byte warning threshold.",
                merged.Length,
                SizeWarningThresholdBytes);
        }

        return RegoDataMergeStatus.Ready;
    }

    // Must be called while holding _sync. A provider snapshot that collides with the current FAR
    // content or another provider's snapshot is never committed as that provider's last-good: the
    // candidate is kept as PendingSnapshot and retried here (on every subsequent provider refresh
    // and every FAR publish) rather than replacing a value that is still valid, so a rejected
    // candidate never displaces a provider's last-good snapshot. Other providers are validated
    // against using their own best known value (last-good if promoted, otherwise their own still-
    // pending candidate) rather than requiring them to already be promoted: two providers whose
    // very first snapshots both arrive before either is validated must still be able to promote
    // each other in the same pass instead of deadlocking on one another.
    private void ReconcilePendingSnapshots(byte[] farData)
    {
        foreach (var candidate in _providers)
        {
            if (candidate.PendingSnapshot is null)
            {
                continue;
            }

            var documents = new List<ReadOnlyMemory<byte>>(_providers.Length + 1) { farData };
            var readyToValidate = true;

            foreach (var state in _providers)
            {
                var snapshot = ReferenceEquals(state, candidate)
                    ? state.PendingSnapshot
                    : state.Snapshot ?? state.PendingSnapshot;

                if (snapshot is null)
                {
                    // Another provider has not produced any snapshot yet: nothing to validate
                    // this candidate against until it does.
                    readyToValidate = false;
                    break;
                }

                documents.Add(snapshot.Data);
            }

            if (!readyToValidate)
            {
                continue;
            }

            try
            {
                RegoDataMerge.Merge(documents);
            }
            catch (RegoDataMergeException ex)
            {
                _diagnosticEvents.PolicyUpdateError(new RegoDataProviderException(candidate.Name, ex));
                continue;
            }

            candidate.Snapshot = candidate.PendingSnapshot;
            candidate.PendingSnapshot = null;
        }
    }

    private void ScheduleRefresh(ProviderState state)
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            if (state.RefreshInFlight)
            {
                state.RefreshPending = true;
                return;
            }

            state.RefreshInFlight = true;
        }

        _ = RunRefreshLoopAsync(state);
    }

    private async Task RunRefreshLoopAsync(ProviderState state)
    {
        while (true)
        {
            await RefreshOnceAsync(state).ConfigureAwait(false);

            lock (_sync)
            {
                if (state.RefreshPending && !_disposed)
                {
                    state.RefreshPending = false;
                    continue;
                }

                state.RefreshInFlight = false;
                return;
            }
        }
    }

    private async Task RefreshOnceAsync(ProviderState state)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(_disposalSource.Token);
        timeoutSource.CancelAfter(_refreshTimeout);
        var changed = false;

        try
        {
            var snapshot = await state.Instance.GetDataAsync(timeoutSource.Token).ConfigureAwait(false);

            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                // Compare against whatever is currently being considered for this provider - a
                // still-pending candidate takes priority over the last-good snapshot - so an
                // unchanged re-fetch of an already-pending value is not reported as a new change.
                var candidate = state.PendingSnapshot ?? state.Snapshot;

                if (candidate?.Equals(snapshot) == true)
                {
                    return;
                }

                // The fetched snapshot is not committed directly: it only becomes this
                // provider's last-good once TryBuildMergedData confirms it merges cleanly with
                // the current FAR content and every other provider, so a colliding snapshot never
                // displaces a still-valid last-good snapshot.
                state.PendingSnapshot = snapshot;
                changed = true;
            }
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            // Refresh timed out, or the aggregator was disposed while the refresh was in flight:
            // keep the last good snapshot, no diagnostics event either way.
        }
        catch (Exception ex)
        {
            bool disposed;

            lock (_sync)
            {
                disposed = _disposed;
            }

            if (!disposed)
            {
                _diagnosticEvents.PolicyUpdateError(new RegoDataProviderException(state.Name, ex));
            }
        }

        if (changed)
        {
            DataChanged?.Invoke();
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        // Cancels the token every in-flight refresh is awaiting on, so a well-behaved provider
        // observes cancellation right away instead of the aggregator waiting out its timeout. The
        // source itself is never disposed: a refresh that is only just starting on another thread
        // may still be reading its token, and a canceled-but-undisposed
        // CancellationTokenSource is safe to keep and to read from concurrently.
        await _disposalSource.CancelAsync().ConfigureAwait(false);

        foreach (var state in _providers)
        {
            state.ChangeSubscription?.Dispose();

            if (!state.OwnsInstance)
            {
                continue;
            }

            switch (state.Instance)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }

    private sealed class ProviderState(string name, IRegoDataProvider instance, bool ownsInstance)
    {
        public string Name { get; } = name;

        public IRegoDataProvider Instance { get; } = instance;

        public bool OwnsInstance { get; } = ownsInstance;

        public bool RefreshInFlight;

        public bool RefreshPending;

        public RegoDataSnapshot? Snapshot;

        // A freshly fetched snapshot that has not yet been confirmed to merge cleanly with the
        // current FAR content and every other provider. Set by every refresh that returns a
        // different value than whichever of Snapshot/PendingSnapshot is currently newest, and
        // cleared once ReconcilePendingSnapshots promotes it to Snapshot. It is never overwritten
        // with null on a failed reconciliation: it stays here for the next retry.
        public RegoDataSnapshot? PendingSnapshot;

        public IDisposable? ChangeSubscription;
    }
}
