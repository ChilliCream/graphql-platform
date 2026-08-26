using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The cross-process mail-wake daemon leader for one
/// <c>(workspace database, nitro_instance_id)</c>, running a heartbeat, an
/// admission loop, and an execution loop while
/// <see cref="MailWakeDaemonState.Ready"/>. A lost or failed lease renewal
/// demotes to <see cref="MailWakeDaemonState.Standby"/> and cancels every
/// in-flight actor dispatch; a daemon-side Claude access denial on this
/// instance's own dispatch releases leadership and demotes to
/// <see cref="MailWakeDaemonState.Degraded"/> instead.
/// </summary>
internal sealed class MailWakeDaemonCoordinator(
    IMailWakeDaemonLeaderStore leaderStore,
    IActorWakeDispatcher dispatcher,
    IFileSystem fileSystem,
    AgentDatabase database,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider,
    TimeProvider timeProvider,
    MailWakeDaemonPolicy policy) : IMailWakeDaemonCoordinator
{
    private const int MaxTransientAttempts = 5;

    private readonly string _ownerId = $"daemon-{Guid.NewGuid():N}";
    private readonly object _statusLock = new();
    private readonly ConcurrentDictionaryBackoff _backoff = new();

    private MailWakeDaemonStatus _status = MailWakeDaemonStatus.Initial;
    private CancellationTokenSource? _lifetime;
    private Task? _runTask;
    private DateTimeOffset? _selfDeniedUntil;

    /// <summary>
    /// The end of this instance's own daemon-side access-denied cooldown,
    /// read and written under <see cref="_statusLock"/> so a concurrent
    /// election tick never observes a torn or stale value.
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

            // _lifetime/_runTask are cleared only once the run loop has
            // actually finished, so StartAsync's guard keeps throwing while
            // it is still alive.
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
        string? nitroInstanceId = null;

        while (!stopToken.IsCancellationRequested)
        {
            try
            {
                nitroInstanceId ??= await instanceIdProvider.GetIdAsync(
                    globalConfigDirectoryProvider.GetDirectory(), stopToken);

                var epoch = await StandbyUntilLeaderAsync(nitroInstanceId, stopToken);

                if (epoch is null)
                {
                    return;
                }

                await RunAsLeaderAsync(nitroInstanceId, epoch.Value, stopToken);
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

    private async Task<long?> StandbyUntilLeaderAsync(string nitroInstanceId, CancellationToken stopToken)
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
                    null, null, null, s.LastError));

            if (!selfDenied)
            {
                var lease = await ReadLeaseWithRetryAsync(nitroInstanceId, stopToken);

                if (lease is null || lease.ExpiresAt <= now)
                {
                    var epoch = await TryAcquireWithRetryAsync(nitroInstanceId, now, stopToken);

                    if (epoch is not null)
                    {
                        return epoch;
                    }
                }
            }

            try
            {
                await Task.Delay(policy.StandbyPollInterval, timeProvider, stopToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        return null;
    }

    private async Task RunAsLeaderAsync(string nitroInstanceId, long epoch, CancellationToken stopToken)
    {
        var now = timeProvider.GetUtcNow();

        // Never overwrite a concurrent StopAsync's Stopping status.
        UpdateStatus(s => s.State == MailWakeDaemonState.Stopping
            ? s
            : new MailWakeDaemonStatus(
                MailWakeDaemonState.Ready, _ownerId, epoch, now + policy.LeaderLeaseDuration, null));
        _backoff.Clear();

        using var degradedSource = new CancellationTokenSource();
        using var leaderSource = CancellationTokenSource.CreateLinkedTokenSource(stopToken, degradedSource.Token);

        var heartbeatTask = HeartbeatLoopAsync(nitroInstanceId, epoch, degradedSource, leaderSource.Token);
        var admissionTask = AdmissionLoopAsync(nitroInstanceId, epoch, degradedSource, leaderSource.Token);

        await Task.WhenAll(AwaitLoopAsync(heartbeatTask), AwaitLoopAsync(admissionTask));

        if (stopToken.IsCancellationRequested && Status.State != MailWakeDaemonState.Degraded)
        {
            await leaderStore.TryReleaseAsync(
                nitroInstanceId, _ownerId, epoch, timeProvider.GetUtcNow(), CancellationToken.None);
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

    private async Task HeartbeatLoopAsync(
        string nitroInstanceId, long epoch, CancellationTokenSource degradedSource, CancellationToken loopToken)
    {
        while (true)
        {
            await Task.Delay(policy.HeartbeatInterval, timeProvider, loopToken);

            var now = timeProvider.GetUtcNow();
            bool renewed;

            try
            {
                renewed = await leaderStore.TryRenewAsync(
                    nitroInstanceId, _ownerId, epoch, now, policy.LeaderLeaseDuration, Status.LastError, loopToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                UpdateStatus(_ => new MailWakeDaemonStatus(
                    MailWakeDaemonState.Standby, null, null, null, Bound(ex.Message)));
                await degradedSource.CancelAsync();
                return;
            }

            if (!renewed)
            {
                UpdateStatus(s => new MailWakeDaemonStatus(
                    MailWakeDaemonState.Standby, null, null, null, s.LastError));
                await degradedSource.CancelAsync();
                return;
            }

            UpdateStatus(s => s.State == MailWakeDaemonState.Ready
                ? s with { LeaseExpiresAt = now + policy.LeaderLeaseDuration }
                : s);
        }
    }

    private async Task AdmissionLoopAsync(
        string nitroInstanceId, long epoch, CancellationTokenSource degradedSource, CancellationToken loopToken)
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
                    var due = await FindDueActorsWithRetryAsync(nitroInstanceId, now, loopToken) ?? [];

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
                            nitroInstanceId, actor, epoch, executionGate, inFlight, inFlightLock, degradedSource,
                            loopToken));
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
        string nitroInstanceId,
        string actor,
        long epoch,
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
                var receipt = await dispatcher.DispatchAsync(actor, deadline, loopToken);

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

                    // Cancel siblings first so they unwind before the lease
                    // they were fenced under disappears, then release the
                    // lease in a finally (busy-retried) so a SQLITE_BUSY here
                    // can never leave this instance wedged as leader while
                    // reporting Degraded.
                    try
                    {
                        await degradedSource.CancelAsync();
                    }
                    finally
                    {
                        await ReleaseWithRetryAsync(nitroInstanceId, epoch, CancellationToken.None);
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

    private async Task<long?> TryAcquireWithRetryAsync(
        string nitroInstanceId, DateTimeOffset now, CancellationToken cancellationToken)
        => await RunWithBusyRetryAsync(
            ct => leaderStore.TryAcquireAsync(nitroInstanceId, _ownerId, now, policy.LeaderLeaseDuration, ct),
            cancellationToken);

    private async Task<LeaseSnapshot?> ReadLeaseWithRetryAsync(string nitroInstanceId, CancellationToken cancellationToken)
        => await RunWithBusyRetryAsync(ct => ReadLeaseAsync(nitroInstanceId, ct), cancellationToken);

    private async Task<IReadOnlyList<string>?> FindDueActorsWithRetryAsync(
        string nitroInstanceId, DateTimeOffset now, CancellationToken cancellationToken)
        => await RunWithBusyRetryAsync<IReadOnlyList<string>>(
            async ct => await FindDueActorsAsync(nitroInstanceId, now, ct), cancellationToken);

    private async Task ReleaseWithRetryAsync(string nitroInstanceId, long epoch, CancellationToken cancellationToken)
        => await RunWithBusyRetryAsync<bool>(
            async ct => await leaderStore.TryReleaseAsync(
                nitroInstanceId, _ownerId, epoch, timeProvider.GetUtcNow(), ct),
            cancellationToken);

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

    private async Task<LeaseSnapshot?> ReadLeaseAsync(string nitroInstanceId, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<LeaseRow>(
            """
            SELECT owner_id AS OwnerId, epoch AS Epoch, expires_at AS ExpiresAt
            FROM mail_wake_daemons
            WHERE nitro_instance_id = @nitroInstanceId
            """,
            new { nitroInstanceId, cancellationToken });

        return row is null
            ? null
            : new LeaseSnapshot(row.OwnerId, row.Epoch, DateTimeOffset.Parse(row.ExpiresAt, CultureInfo.InvariantCulture));
    }

    private async Task<IReadOnlyList<string>> FindDueActorsAsync(
        string nitroInstanceId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var actors = await connection.QueryAsync<string>(
            """
            SELECT actor FROM mail_wake_outbox
            WHERE nitro_instance_id = @nitroInstanceId
              AND settled_generation < requested_generation
              AND due_at <= @now
            """,
            new { nitroInstanceId, now, cancellationToken });

        return actors.AsList();
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }

    private sealed record LeaseSnapshot(string OwnerId, long Epoch, DateTimeOffset ExpiresAt);

    // Internal, not private: Dapper.AOT's generated interceptors live
    // outside this class and cannot reference a private nested type.
    internal sealed class LeaseRow
    {
        public required string OwnerId { get; init; }
        public required long Epoch { get; init; }
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
