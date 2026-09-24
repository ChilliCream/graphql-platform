using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Coordinates background mail wakes for this workspace.
/// Lease loss returns it to standby; a Claude access denial temporarily degrades it.
/// </summary>
internal sealed class MailWakeDaemonCoordinator(
    IMailWakeDaemonLeaderStore leaderStore,
    IActorWakeDispatcher dispatcher,
    IFileSystem fileSystem,
    AgentDatabase database,
    TimeProvider timeProvider,
    MailWakeDaemonPolicy policy) : IMailWakeDaemonCoordinator
{
    private const int MaxTransientAttempts = 5;

    private readonly string _ownerToken = $"daemon-{Guid.NewGuid():N}";
    private readonly object _statusLock = new();
    private readonly ConcurrentDictionaryBackoff _backoff = new();

    private MailWakeDaemonStatus _status = MailWakeDaemonStatus.Initial;
    private CancellationTokenSource? _lifetime;
    private Task? _runTask;
    private DateTimeOffset? _selfDeniedUntil;

    /// <summary>
    /// The end of the access-denial cooldown, or null when no cooldown has been set.
    /// </summary>
    private DateTimeOffset? SelfDeniedUntil
    {
        get
        {
            lock (_statusLock)
            {
                return _selfDeniedUntil;
            }
        }
        set
        {
            lock (_statusLock)
            {
                _selfDeniedUntil = value;
            }
        }
    }

    public MailWakeDaemonStatus Status
    {
        get
        {
            lock (_statusLock)
            {
                if (_status.State == MailWakeDaemonState.Ready
                    && timeProvider.GetUtcNow() >= _status.LeaseExpiresAt)
                {
                    return _status with { State = MailWakeDaemonState.Standby, OwnerToken = null, LeaseExpiresAt = null };
                }

                return _status;
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_runTask is not null)
        {
            throw new InvalidOperationException("The mail-wake daemon coordinator has already been started.");
        }

        _backoff.Clear();
        SelfDeniedUntil = null;
        SetStatus(MailWakeDaemonStatus.Initial);

        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runTask = RunAsync(_lifetime.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_lifetime is not { } lifetime || _runTask is not { } runTask)
        {
            return;
        }

        UpdateStatus(s => s with { State = MailWakeDaemonState.Stopping });

        try
        {
            await lifetime.CancelAsync();
            await runTask.WaitAsync(policy.ShutdownWait, cancellationToken);
        }
        catch (TimeoutException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            UpdateStatus(s => s with { LastError = Bound(ex.Message) });
        }
        finally
        {
            lifetime.Dispose();

            // Restart remains unavailable while the previous run loop is unfinished.
            if (runTask.IsCompleted)
            {
                _lifetime = null;
                _runTask = null;
            }
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None);

    private async Task RunAsync(CancellationToken stopToken)
    {
        while (!stopToken.IsCancellationRequested)
        {
            try
            {
                var acquired = await StandbyUntilLeaderAsync(stopToken);

                if (!acquired)
                {
                    return;
                }

                await RunAsLeaderAsync(stopToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                UpdateStatus(s => s with { State = MailWakeDaemonState.Standby, LastError = Bound(ex.Message) });

                try
                {
                    await Task.Delay(policy.StandbyPollInterval, timeProvider, stopToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task<bool> StandbyUntilLeaderAsync(CancellationToken stopToken)
    {
        while (!stopToken.IsCancellationRequested)
        {
            var now = timeProvider.GetUtcNow();
            var selfDenied = SelfDeniedUntil is { } deniedUntil && now < deniedUntil;

            // Never overwrite a concurrent StopAsync's Stopping status.
            UpdateStatus(s => s.State == MailWakeDaemonState.Stopping
                ? s
                : new MailWakeDaemonStatus(
                    selfDenied ? MailWakeDaemonState.Degraded : MailWakeDaemonState.Standby,
                    null, null, s.LastError));

            if (!selfDenied)
            {
                var lease = await ReadLeaseWithRetryAsync(stopToken);

                if (lease is null || lease.ExpiresAt <= now)
                {
                    var acquired = await TryAcquireWithRetryAsync(now, stopToken);

                    if (acquired)
                    {
                        return true;
                    }
                }
            }

            try
            {
                await Task.Delay(policy.StandbyPollInterval, timeProvider, stopToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return false;
    }

    private async Task RunAsLeaderAsync(CancellationToken stopToken)
    {
        var now = timeProvider.GetUtcNow();

        // Never overwrite a concurrent StopAsync's Stopping status.
        UpdateStatus(s => s.State == MailWakeDaemonState.Stopping
            ? s
            : new MailWakeDaemonStatus(
                MailWakeDaemonState.Ready, _ownerToken, now + policy.LeaderLeaseDuration, null));
        _backoff.Clear();

        using var degradedSource = new CancellationTokenSource();
        using var leaderSource = CancellationTokenSource.CreateLinkedTokenSource(stopToken, degradedSource.Token);

        var heartbeatTask = HeartbeatLoopAsync(degradedSource, leaderSource.Token);
        var admissionTask = AdmissionLoopAsync(degradedSource, leaderSource.Token);

        await Task.WhenAll(AwaitLoopAsync(heartbeatTask), AwaitLoopAsync(admissionTask));

        if (stopToken.IsCancellationRequested && Status.State != MailWakeDaemonState.Degraded)
        {
            await leaderStore.TryReleaseAsync(_ownerToken, timeProvider.GetUtcNow(), CancellationToken.None);
        }
    }

    private static async Task AwaitLoopAsync(Task loop)
    {
        try
        {
            await loop;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task HeartbeatLoopAsync(CancellationTokenSource degradedSource, CancellationToken loopToken)
    {
        while (true)
        {
            await Task.Delay(policy.HeartbeatInterval, timeProvider, loopToken);

            var now = timeProvider.GetUtcNow();
            bool renewed;

            try
            {
                renewed = await leaderStore.TryRenewAsync(
                    _ownerToken, now, policy.LeaderLeaseDuration, loopToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                UpdateStatus(_ => new MailWakeDaemonStatus(
                    MailWakeDaemonState.Standby, null, null, Bound(ex.Message)));
                await degradedSource.CancelAsync();
                return;
            }

            if (!renewed)
            {
                UpdateStatus(s => new MailWakeDaemonStatus(
                    MailWakeDaemonState.Standby, null, null, s.LastError));
                await degradedSource.CancelAsync();
                return;
            }

            UpdateStatus(s => s.State == MailWakeDaemonState.Ready
                ? s with { LeaseExpiresAt = now + policy.LeaderLeaseDuration }
                : s);
        }
    }

    private async Task AdmissionLoopAsync(CancellationTokenSource degradedSource, CancellationToken loopToken)
    {
        using var executionGate = new SemaphoreSlim(policy.MaxConcurrentActorExecutions);
        var inFlight = new HashSet<string>(StringComparer.Ordinal);
        var inFlightLock = new object();
        var executionTasks = new List<Task>();

        try
        {
            while (true)
            {
                try
                {
                    var now = timeProvider.GetUtcNow();

                    if (Status.State == MailWakeDaemonState.Ready)
                    {
                        var due = await FindDueActorsWithRetryAsync(now, loopToken) ?? [];

                        foreach (var actor in due)
                        {
                            lock (inFlightLock)
                            {
                                if (inFlight.Contains(actor) || !_backoff.IsEligible(actor, now))
                                {
                                    continue;
                                }

                                inFlight.Add(actor);
                            }

                            executionTasks.Add(ExecuteActorAsync(
                                actor, executionGate, inFlight, inFlightLock, degradedSource, loopToken));
                        }
                    }

                    executionTasks.RemoveAll(t => t.IsCompleted);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    UpdateStatus(s => s with { LastError = Bound(ex.Message) });
                }

                await Task.Delay(policy.AdmissionPollInterval, timeProvider, loopToken);
            }
        }
        finally
        {
            try
            {
                await Task.WhenAll(executionTasks);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task ExecuteActorAsync(
        string actor,
        SemaphoreSlim gate,
        HashSet<string> inFlight,
        object inFlightLock,
        CancellationTokenSource degradedSource,
        CancellationToken loopToken)
    {
        try
        {
            await gate.WaitAsync(loopToken);

            try
            {
                var deadline = timeProvider.GetUtcNow() + WakeDispatchPolicy.BatchDeadline;
                var receipt = await dispatcher.DispatchAsync(actor, _ownerToken, deadline, loopToken);

                if (receipt is null)
                {
                    return;
                }

                var deniedByThisAttempt = receipt.Targets.Any(t =>
                    t.Status == MailWakeTargetStatus.Pending && t.LastError == "access-denied");

                if (deniedByThisAttempt)
                {
                    SelfDeniedUntil = timeProvider.GetUtcNow() + MailWakeDaemonRetryPolicy.MaxDelay;
                    UpdateStatus(s => s with { State = MailWakeDaemonState.Degraded, LastError = "access-denied" });

                    // Signals sibling dispatches to cancel before releasing leadership.
                    try
                    {
                        await degradedSource.CancelAsync();
                    }
                    finally
                    {
                        await ReleaseWithRetryAsync(CancellationToken.None);
                    }

                    return;
                }

                var offeredReason = receipt.Targets
                    .FirstOrDefault(t => t.Status == MailWakeTargetStatus.Pending)?.LastError;

                if (MailWakeDaemonRetryPolicy.IsTransientOffer(offeredReason))
                {
                    _backoff.RecordFailure(actor, timeProvider.GetUtcNow());
                }
                else
                {
                    _backoff.RecordSuccess(actor);
                }
            }
            finally
            {
                gate.Release();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            UpdateStatus(s => s with { LastError = Bound(ex.Message) });
        }
        finally
        {
            lock (inFlightLock)
            {
                inFlight.Remove(actor);
            }
        }
    }

    private void SetStatus(MailWakeDaemonStatus status)
    {
        lock (_statusLock)
        {
            _status = status;
        }
    }

    private void UpdateStatus(Func<MailWakeDaemonStatus, MailWakeDaemonStatus> update)
    {
        lock (_statusLock)
        {
            _status = update(_status);
        }
    }

    private static string? Bound(string? value) => value is null || value.Length <= 200 ? value : value[..200];

    private static bool IsBusy(SqliteException ex) => ex.SqliteErrorCode is 5 or 6; // SQLITE_BUSY / SQLITE_LOCKED

    private async Task<bool> TryAcquireWithRetryAsync(DateTimeOffset now, CancellationToken cancellationToken)
        => await RunBoolWithBusyRetryAsync(
            ct => leaderStore.TryAcquireAsync(_ownerToken, now, policy.LeaderLeaseDuration, ct),
            cancellationToken);

    private async Task<LeaseSnapshot?> ReadLeaseWithRetryAsync(CancellationToken cancellationToken)
        => await RunWithBusyRetryAsync(ReadLeaseAsync, cancellationToken);

    private async Task<IReadOnlyList<string>?> FindDueActorsWithRetryAsync(
        DateTimeOffset now, CancellationToken cancellationToken)
        => await RunWithBusyRetryAsync(async ct => await FindDueActorsAsync(now, ct), cancellationToken);

    private async Task ReleaseWithRetryAsync(CancellationToken cancellationToken)
        => await RunBoolWithBusyRetryAsync(
            ct => leaderStore.TryReleaseAsync(_ownerToken, timeProvider.GetUtcNow(), ct),
            cancellationToken);

    /// <summary>
    /// Retries <paramref name="operation"/> under the same busy-retry policy as
    /// <see cref="RunWithBusyRetryAsync{TResult}"/>, returning false once retries are exhausted.
    /// </summary>
    private async Task<bool> RunBoolWithBusyRetryAsync(
        Func<CancellationToken, Task<bool>> operation, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxTransientAttempts; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (SqliteException ex) when (IsBusy(ex))
            {
                if (attempt == MaxTransientAttempts)
                {
                    return false;
                }

                await Task.Delay(MailWakeDaemonRetryPolicy.ComputeDelay(attempt), timeProvider, cancellationToken);
            }
        }

        return false;
    }

    private async Task<TResult?> RunWithBusyRetryAsync<TResult>(
        Func<CancellationToken, Task<TResult?>> operation, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxTransientAttempts; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (SqliteException ex) when (IsBusy(ex))
            {
                if (attempt == MaxTransientAttempts)
                {
                    return default;
                }

                await Task.Delay(MailWakeDaemonRetryPolicy.ComputeDelay(attempt), timeProvider, cancellationToken);
            }
        }

        return default;
    }

    private async Task<LeaseSnapshot?> ReadLeaseAsync(CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<LeaseRow>(
            new CommandDefinition(
                """
                SELECT owner_token AS OwnerToken, expires_at AS ExpiresAt
                FROM mail_wake_daemons
                WHERE id = 1
                """,
                cancellationToken: cancellationToken));

        return row is null
            ? null
            : new LeaseSnapshot(row.OwnerToken, DateTimeOffset.Parse(row.ExpiresAt, CultureInfo.InvariantCulture));
    }

    private async Task<IReadOnlyList<string>> FindDueActorsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var actors = await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT actor FROM mail_wake_outbox
                WHERE settled_generation < requested_generation AND due_at <= @now
                """,
                new { now },
                cancellationToken: cancellationToken));

        return actors.AsList();
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }

    private sealed record LeaseSnapshot(string OwnerToken, DateTimeOffset ExpiresAt);

    internal sealed class LeaseRow
    {
        public required string OwnerToken { get; init; }
        public required string ExpiresAt { get; init; }
    }

    /// <summary>
    /// Per-actor, in-memory retry eligibility under
    /// <see cref="MailWakeDaemonRetryPolicy"/>, thread-safe across concurrent
    /// callers.
    /// </summary>
    private sealed class ConcurrentDictionaryBackoff
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Failures, DateTimeOffset NextEligibleAt)>
            _state = new(StringComparer.Ordinal);

        public bool IsEligible(string actor, DateTimeOffset now)
            => !_state.TryGetValue(actor, out var entry) || now >= entry.NextEligibleAt;

        public void RecordFailure(string actor, DateTimeOffset now)
        {
            _state.AddOrUpdate(
                actor,
                _ => (1, now + MailWakeDaemonRetryPolicy.ComputeDelay(1)),
                (_, existing) =>
                {
                    var failures = existing.Failures + 1;
                    return (failures, now + MailWakeDaemonRetryPolicy.ComputeDelay(failures));
                });
        }

        public void RecordSuccess(string actor) => _state.TryRemove(actor, out _);

        public void Clear() => _state.Clear();
    }
}
