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
    /// Attempts to build the merged data document from the given FAR data document and every
    /// provider's best known data, without committing anything: a provider's still-pending
    /// candidate is only ever promoted to that provider's last-good snapshot by calling
    /// <see cref="RegoDataMergeAttempt.Commit"/> on the <see cref="RegoDataMergeAttempt"/> this
    /// returns on <see cref="RegoDataMergeStatus.Ready"/>. The caller commits only once it has
    /// successfully compiled the policy set with <see cref="RegoDataMergeAttempt.MergedData"/>;
    /// an attempt that is discarded instead leaves every provider's last-good snapshot exactly as
    /// it was.
    /// </summary>
    public RegoDataMergeStatus TryBuildMergedData(
        byte[] farData,
        out RegoDataMergeAttempt? attempt,
        out Exception? error)
    {
        attempt = null;
        error = null;

        List<ReadOnlyMemory<byte>> documents;
        List<(ProviderState State, RegoDataSnapshot Snapshot)> promotions;
        List<(ProviderState State, RegoDataSnapshot Snapshot)>? forcedCandidates = null;

        lock (_sync)
        {
            promotions = ComputePendingPromotions(farData);

            documents = new List<ReadOnlyMemory<byte>>(_providers.Length + 1) { farData };

            foreach (var state in _providers)
            {
                var snapshot = FindPromotion(promotions, state)?.Snapshot ?? state.Snapshot;

                if (snapshot is not null)
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
                // collided and it has no last-good to fall back to: its still-pending,
                // still-unvalidated candidate is used as the merge fallback below instead of
                // waiting forever. Tracked here as a forced candidate so that, on a clean merge, it
                // is promoted alongside the other promotions below (the data actually served is
                // then exactly the data that got committed, honoring invariant 4 of ruling 726
                // instead of serving it silently); on a collision it attributes the failure below
                // instead of surfacing as a bare, unattributed merge exception
                // (ComputePendingPromotions deliberately did not report it, to avoid reporting the
                // same collision twice).
                (forcedCandidates ??= []).Add((state, state.PendingSnapshot));
                documents.Add(state.PendingSnapshot.Data);
            }
        }

        byte[] merged;

        try
        {
            merged = RegoDataMerge.Merge(documents);
        }
        catch (RegoDataMergeException ex)
        {
            // Attribute the failure to the (first) provider that has no last-good snapshot to
            // fall back on: that provider's still-pending, still-unvalidated candidate is what
            // forced this attempt through, whether it is the sole cause of the collision or one
            // of several no-last-good providers caught in the same one. A failure that involves
            // only providers with an established last-good (a FAR change colliding with already
            // committed data) is not attributable to any single provider and stays a bare
            // RegoDataMergeException.
            error = forcedCandidates is { Count: > 0 }
                ? new RegoDataProviderException(forcedCandidates[0].State.Name, ex)
                : ex;
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

        if (forcedCandidates is not null)
        {
            // The merge succeeded using each forced candidate's still-pending data as-is, so the
            // data about to be served is exactly that candidate's data: promote it alongside the
            // other promotions so Commit advances it to last-good too, instead of leaving it
            // uncommitted and unreported while its data is served.
            promotions.AddRange(forcedCandidates);
        }

        attempt = new RegoDataMergeAttempt(this, merged, promotions);
        return RegoDataMergeStatus.Ready;
    }

    private static (ProviderState State, RegoDataSnapshot Snapshot)? FindPromotion(
        List<(ProviderState State, RegoDataSnapshot Snapshot)> promotions,
        ProviderState state)
    {
        foreach (var promotion in promotions)
        {
            if (ReferenceEquals(promotion.State, state))
            {
                return promotion;
            }
        }

        return null;
    }

    // Must be called while holding _sync. Computes, without mutating any provider's committed
    // Snapshot, which pending candidates would be safe to promote: a provider snapshot that
    // collides with the given FAR data or another provider's snapshot never becomes that
    // provider's last-good here - the caller commits the returned promotions only after
    // successfully compiling the policy set with the resulting merged data, so a candidate that
    // looked safe here but turns out to break compilation never displaces a still-valid last-good
    // snapshot. Other providers are validated against using their own best known value (last-good
    // if any, otherwise their own still-pending candidate, otherwise an already-computed promotion
    // from earlier in this same pass) rather than requiring them to already be promoted: two
    // providers whose very first snapshots both arrive before either is committed must still be
    // able to validate against each other in the same pass instead of deadlocking on one another.
    private List<(ProviderState State, RegoDataSnapshot Snapshot)> ComputePendingPromotions(
        byte[] farData)
    {
        var promotions = new List<(ProviderState State, RegoDataSnapshot Snapshot)>();

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
                    : FindPromotion(promotions, state)?.Snapshot ?? state.Snapshot ?? state.PendingSnapshot;

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
                if (candidate.Snapshot is not null)
                {
                    // This provider already has a last-good snapshot, so the caller's own merge
                    // attempt below will silently keep using it instead of re-discovering this
                    // same failure: report it here, once, or it would never surface at all.
                    _diagnosticEvents.PolicyUpdateError(new RegoDataProviderException(candidate.Name, ex));
                }

                // Else: this provider has no last-good yet, so it has no fallback value - the
                // caller's overall merge attempt is forced to fall through with this same
                // colliding candidate and will fail identically, reporting the failure once
                // there. Reporting here too would report the same collision twice (F3).
                continue;
            }

            promotions.Add((candidate, candidate.PendingSnapshot));
        }

        return promotions;
    }

    // Commits every promotion an accepted RegoDataMergeAttempt staged: a provider whose pending
    // candidate is still exactly the value validated when the attempt was built becomes that
    // provider's new last-good snapshot. A provider whose pending candidate has since moved on (a
    // newer refresh arrived while the caller was compiling) is left untouched - the newer
    // candidate stays pending for the next rebuild to retry.
    private void CommitPromotions(List<(ProviderState State, RegoDataSnapshot Snapshot)> promotions)
    {
        if (promotions.Count == 0)
        {
            return;
        }

        lock (_sync)
        {
            foreach (var (state, snapshot) in promotions)
            {
                if (state.PendingSnapshot?.Equals(snapshot) == true)
                {
                    state.Snapshot = state.PendingSnapshot;
                    state.PendingSnapshot = null;
                }
            }
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
                // provider's last-good once the caller confirms it merges cleanly with the
                // current FAR content and every other provider AND compiles, so a colliding or
                // otherwise broken candidate never displaces a still-valid last-good snapshot.
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

    /// <summary>
    /// A validated, not-yet-committed outcome of <see cref="RegoDataAggregator.TryBuildMergedData"/>.
    /// </summary>
    /// <remarks>
    /// Carries the merged data document together with the set of provider promotions that
    /// produced it. Nothing about the aggregator's state changes until <see cref="Commit"/> is
    /// called; an attempt that is simply discarded (because, for example, the caller failed to
    /// compile the policy set with <see cref="MergedData"/>) leaves every provider's last-good
    /// snapshot exactly as it was before the attempt was built.
    /// </remarks>
    internal sealed class RegoDataMergeAttempt
    {
        private readonly RegoDataAggregator _owner;
        private readonly List<(ProviderState State, RegoDataSnapshot Snapshot)> _promotions;

        internal RegoDataMergeAttempt(
            RegoDataAggregator owner,
            byte[] mergedData,
            List<(ProviderState State, RegoDataSnapshot Snapshot)> promotions)
        {
            _owner = owner;
            MergedData = mergedData;
            _promotions = promotions;
        }

        /// <summary>
        /// Gets the merged data document this attempt produced.
        /// </summary>
        public byte[] MergedData { get; }

        /// <summary>
        /// Commits every promotion this attempt staged. Call only after the policy set has been
        /// successfully compiled with <see cref="MergedData"/>; calling it any other time risks
        /// promoting provider data that was never actually used to produce a served policy set.
        /// </summary>
        public void Commit() => _owner.CommitPromotions(_promotions);
    }

    // internal (not private): RegoDataMergeAttempt's constructor - called from TryBuildMergedData,
    // a member of this containing type - takes a List<(ProviderState, RegoDataSnapshot)>, and a
    // private nested type is not accessible from the containing type's own members, only from
    // within its own body. internal is therefore the narrowest visibility this design supports;
    // every member below is internal rather than public for the same reason - nothing outside this
    // assembly ever needs to see a ProviderState.
    internal sealed class ProviderState(string name, IRegoDataProvider instance, bool ownsInstance)
    {
        internal string Name { get; } = name;

        internal IRegoDataProvider Instance { get; } = instance;

        internal bool OwnsInstance { get; } = ownsInstance;

        internal bool RefreshInFlight { get; set; }

        internal bool RefreshPending { get; set; }

        internal RegoDataSnapshot? Snapshot { get; set; }

        // A freshly fetched snapshot that has not yet been confirmed to merge cleanly with the
        // current FAR content and every other provider AND to compile. Set by every refresh that
        // returns a different value than whichever of Snapshot/PendingSnapshot is currently
        // newest, and cleared only once RegoDataMergeAttempt.Commit promotes it to Snapshot. It is
        // never overwritten with null on a failed validation or a discarded attempt: it stays here
        // for the next retry.
        internal RegoDataSnapshot? PendingSnapshot { get; set; }

        internal IDisposable? ChangeSubscription { get; set; }
    }
}
