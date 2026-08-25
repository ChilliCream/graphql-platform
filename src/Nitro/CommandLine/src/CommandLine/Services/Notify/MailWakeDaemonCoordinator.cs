using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The persistent, cross-process mail-wake daemon: one leader per
/// <c>(workspace database, nitro_instance_id)</c>, elected and renewed over
/// <see cref="IMailWakeDaemonLeaderStore"/>, running two independent loops
/// while <see cref="MailWakeDaemonState.Ready"/>.
/// <list type="bullet">
/// <item>A heartbeat loop renews the held lease every
/// <see cref="MailWakeDaemonPolicy.HeartbeatInterval"/>; a lost or failed
/// renewal demotes to <see cref="MailWakeDaemonState.Standby"/> and cancels
/// the admission loop and every in-flight actor dispatch.</item>
/// <item>An admission loop polls <c>mail_wake_outbox</c> every
/// <see cref="MailWakeDaemonPolicy.AdmissionPollInterval"/> for actors whose
/// generation is due, and hands each newly due actor to its own concurrent
/// execution task (bounded by <see cref="MailWakeDaemonPolicy.MaxConcurrentActorExecutions"/>)
/// that calls the reused <see cref="IActorWakeDispatcher.DispatchAsync"/>.
/// Because discovery and per-actor dispatch run as independent tasks, a
/// newly due actor is admitted without waiting for another actor's own
/// in-flight transport to finish.</item>
/// </list>
/// A non-leader instance never writes a probe: it reads the lease row
/// read-only every <see cref="MailWakeDaemonPolicy.StandbyPollInterval"/> and
/// only calls <see cref="IMailWakeDaemonLeaderStore.TryAcquireAsync"/> once it
/// observes the row as expired.
///
/// If this coordinator's own dispatch of an actor is itself denied Claude
/// socket access, this instance can never serve that endpoint class: it
/// releases leadership immediately, demotes to
/// <see cref="MailWakeDaemonState.Degraded"/>, cancels every other in-flight
/// execution through the same leadership-scoped cancellation token, and
/// withholds its own re-acquisition attempts for
/// <see cref="MailWakeDaemonRetryPolicy.MaxDelay"/> so a differently
/// privileged standby can take over without contention. This backoff lives
/// only in this instance's own memory; it never suppresses another
/// coordinator instance.
///
/// Reuses <see cref="IMailWakeDaemonLeaderStore"/> and
/// <see cref="IActorWakeDispatcher"/> exactly as merged: every claim,
/// materialization, target resolution, and retry-timing decision below the
/// actor level belongs to <see cref="ActorWakeDispatcher"/> already. This
/// coordinator is only responsible for deciding, cross-process, who is
/// allowed to call it and for which actors, and for doing so quickly and
/// safely even while another actor's dispatch is still running.
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

    private readonly string _ownerId = $"daemon-{Guid.NewGuid():N}";
    private readonly object _statusLock = new();
    private readonly ConcurrentDictionaryBackoff _backoff = new();

    private MailWakeDaemonStatus _status = MailWakeDaemonStatus.Initial;
    private CancellationTokenSource? _lifetime;
    private Task? _runTask;
    private DateTimeOffset? _selfDeniedUntil;

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

    public Task StartAsync(string nitroInstanceId, CancellationToken cancellationToken)
    {
        if (_runTask is not null)
        {
            throw new InvalidOperationException("The mail-wake daemon coordinator has already been started.");
        }

        _backoff.Clear();
        _selfDeniedUntil = null;
        SetStatus(MailWakeDaemonStatus.Initial);

        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runTask = RunAsync(nitroInstanceId, _lifetime.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_lifetime is not { } lifetime || _runTask is not { } runTask)
        {
            return;
        }

        SetStatus(Status with { State = MailWakeDaemonState.Stopping });

        try
        {
            await lifetime.CancelAsync();
            await runTask.WaitAsync(policy.ShutdownWait, cancellationToken);
        }
        catch (TimeoutException)
        {
            // A noncooperative in-flight task outlived the shutdown budget:
            // this call still returns rather than blocking forever, and the
            // orphaned task is left to lose its lease/claims to expiry.
        }
        catch (OperationCanceledException)
        {
            // cancellationToken itself fired while waiting for the loop to
            // wind down.
        }

        lifetime.Dispose();
        _lifetime = null;
        _runTask = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None);

    private async Task RunAsync(string nitroInstanceId, CancellationToken stopToken)
    {
        try
        {
            while (!stopToken.IsCancellationRequested)
            {
                var epoch = await StandbyUntilLeaderAsync(nitroInstanceId, stopToken);

                if (epoch is null)
                {
                    break;
                }

                await RunAsLeaderAsync(nitroInstanceId, epoch.Value, stopToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }
    }

    private async Task<long?> StandbyUntilLeaderAsync(string nitroInstanceId, CancellationToken stopToken)
    {
        while (!stopToken.IsCancellationRequested)
        {
            var now = timeProvider.GetUtcNow();
            var selfDenied = _selfDeniedUntil is { } deniedUntil && now < deniedUntil;

            // Keep reporting Degraded, not Standby, for the whole
            // self-denial backoff window: this instance is not merely idly
            // waiting its turn, it deliberately withholds every acquisition
            // attempt below so a differently privileged standby can win
            // instead.
            SetStatus(new MailWakeDaemonStatus(
                selfDenied ? MailWakeDaemonState.Degraded : MailWakeDaemonState.Standby,
                null, null, null, Status.LastError));

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
        SetStatus(new MailWakeDaemonStatus(
            MailWakeDaemonState.Ready, _ownerId, epoch, now + policy.LeaderLeaseDuration, null));
        _backoff.Clear();

        using var degradedSource = new CancellationTokenSource();
        using var leaderSource = CancellationTokenSource.CreateLinkedTokenSource(stopToken, degradedSource.Token);

        var heartbeatTask = HeartbeatLoopAsync(nitroInstanceId, epoch, degradedSource, leaderSource.Token);
        var admissionTask = AdmissionLoopAsync(nitroInstanceId, epoch, degradedSource, leaderSource.Token);

        await Task.WhenAll(AwaitLoopAsync(heartbeatTask), AwaitLoopAsync(admissionTask));

        if (stopToken.IsCancellationRequested && Status.State != MailWakeDaemonState.Degraded)
        {
            // Graceful shutdown while still holding leadership: release
            // immediately rather than waiting out the lease, so a standby
            // can take over right away. A self-inflicted degradation already
            // released leadership itself before cancelling these loops.
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
            // Expected: leaderSource was cancelled, either by graceful stop,
            // heartbeat loss, or this instance's own degradation.
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
                // A renewal whose result is unknown (the store call itself
                // threw) is treated exactly like a lost renewal.
                SetStatus(Status with { State = MailWakeDaemonState.Standby, LastError = Bound(ex.Message) });
                await degradedSource.CancelAsync();
                return;
            }

            if (!renewed)
            {
                SetStatus(Status with { State = MailWakeDaemonState.Standby });
                await degradedSource.CancelAsync();
                return;
            }

            SetStatus(Status with { LeaseExpiresAt = now + policy.LeaderLeaseDuration });
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
                    // An unexpected admission-tick failure must not crash
                    // the whole loop; the next tick simply tries again.
                    SetStatus(Status with { LastError = Bound(ex.Message) });
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
                    // This instance's own dispatch was itself denied Claude
                    // socket access: it can never accept this endpoint class
                    // for itself, so it stops admission and degrades rather
                    // than retrying an offer it can never fulfil.
                    _selfDeniedUntil = timeProvider.GetUtcNow() + MailWakeDaemonRetryPolicy.MaxDelay;
                    SetStatus(Status with { State = MailWakeDaemonState.Degraded, LastError = "access-denied" });
                    await leaderStore.TryReleaseAsync(
                        nitroInstanceId, _ownerId, epoch, timeProvider.GetUtcNow(), CancellationToken.None);
                    await degradedSource.CancelAsync();
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
            // Leadership ended (graceful stop, heartbeat loss, or this
            // instance's own degradation) while this actor's dispatch was in
            // flight; ActorWakeDispatcher never asserts an outcome for a
            // target abandoned this way, so there is nothing further to
            // record here.
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
    /// Tracks, per actor and only in this coordinator instance's memory, how
    /// many consecutive dispatches in a row left durable offered (busy,
    /// capacity, or access-denied) work behind, and the earliest time this
    /// instance will attempt that actor again under
    /// <see cref="MailWakeDaemonRetryPolicy"/>. Thread-safe: written from
    /// concurrent per-actor execution tasks, read from the single admission
    /// loop.
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
