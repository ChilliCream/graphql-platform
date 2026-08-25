using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Read-only diagnosis of the mail-wake daemon's persisted state for one
/// Nitro instance: the <c>mail_wake_daemons</c> leader row's epoch and lease,
/// that instance's own <c>mail_wake_outbox</c> generation counts, and any
/// target durably stuck on a Claude access-denied handoff. Reads the schema
/// and store state directly by SQL, never through
/// <see cref="IMailWakeDaemonLeaderStore"/> or <see cref="IMailWakeBatchStore"/>,
/// so it never acquires, renews, or releases anything. Every query is scoped
/// to the caller's own Nitro instance id: a different instance's rows are
/// never read or counted. Never reports an owner id, socket path, key, or
/// raw stderr; the reported last error carries only the daemon's own
/// already-bounded (200 char) failure message.
/// </summary>
internal static class DoctorMailWakeCheck
{
    /// <summary>
    /// The result for a workspace whose schema predates the mail-wake tables
    /// (see <see cref="AgentDatabase.CurrentVersion"/>): never queries a
    /// table that may not exist, and never reports such a workspace healthy,
    /// so an old dashboard process still open across the schema upgrade is
    /// never mistaken for a current one.
    /// </summary>
    public static DoctorAgentCommand.MailWakeDoctorResult ForSchemaNotCurrent(long version) => new(
        SchemaCurrent: false,
        LeaderState: "unknown",
        Epoch: null,
        LeaseExpiresInSeconds: null,
        LastError: null,
        AccessDeniedPendingTargets: 0,
        PendingActorCount: 0,
        AcceptedActorCount: 0,
        DeferredActorCount: 0,
        OldestPendingAgeSeconds: null,
        Healthy: false,
        Remediation:
        [
            $"Mail-wake diagnostics require the current schema (v{AgentDatabase.CurrentVersion}); "
            + $"this workspace is v{version}. Run `nitro agent init` to migrate. A dashboard "
            + "reporting healthy against this schema predates mail-wake support and cannot be "
            + "trusted."
        ]);

    public static async Task<DoctorAgentCommand.MailWakeDoctorResult> CheckAsync(
        SqliteConnection connection,
        string nitroInstanceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var lease = await connection.QueryFirstOrDefaultAsync<LeaderRow>(
            """
            SELECT epoch AS Epoch, expires_at AS ExpiresAt, last_error AS LastError
            FROM mail_wake_daemons
            WHERE nitro_instance_id = @nitroInstanceId
            """,
            new { nitroInstanceId, cancellationToken });

        string leaderState;
        long? epoch = null;
        double? leaseExpiresInSeconds = null;
        string? lastError = null;

        if (lease is null)
        {
            leaderState = "none";
        }
        else
        {
            epoch = lease.Epoch;
            lastError = lease.LastError;

            var expiresAt = DateTimeOffset.Parse(lease.ExpiresAt, CultureInfo.InvariantCulture);
            leaseExpiresInSeconds = (expiresAt - now).TotalSeconds;
            leaderState = leaseExpiresInSeconds > 0 ? "ready" : "expired";
        }

        var outbox = (await connection.QueryAsync<OutboxRow>(
            """
            SELECT actor AS Actor, due_at AS DueAt
            FROM mail_wake_outbox
            WHERE nitro_instance_id = @nitroInstanceId AND settled_generation < requested_generation
            """,
            new { nitroInstanceId, cancellationToken }))
            .ToArray();

        var activeActors = (await connection.QueryAsync<string>(
            """
            SELECT actor FROM mail_wake_batches
            WHERE nitro_instance_id = @nitroInstanceId AND status = 'active' AND expires_at > @now
            """,
            new { nitroInstanceId, now, cancellationToken }))
            .ToHashSet(StringComparer.Ordinal);

        var pendingCount = 0;
        var acceptedCount = 0;
        var deferredCount = 0;
        DateTimeOffset? oldestDue = null;

        foreach (var row in outbox)
        {
            var dueAt = DateTimeOffset.Parse(row.DueAt, CultureInfo.InvariantCulture);

            if (activeActors.Contains(row.Actor))
            {
                acceptedCount++;
            }
            else if (dueAt <= now)
            {
                pendingCount++;
            }
            else
            {
                deferredCount++;
                continue;
            }

            if (oldestDue is null || dueAt < oldestDue)
            {
                oldestDue = dueAt;
            }
        }

        var accessDeniedPendingTargets = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*) FROM mail_wake_targets t
            JOIN mail_wake_batches b ON b.batch_id = t.batch_id
            WHERE b.nitro_instance_id = @nitroInstanceId AND t.status = 'pending'
              AND t.last_error = 'access-denied'
            """,
            new { nitroInstanceId, cancellationToken });

        var remediation = new List<string>();
        var healthy = true;

        if (accessDeniedPendingTargets > 0)
        {
            healthy = false;
            remediation.Add(
                $"{accessDeniedPendingTargets} target(s) are stuck pending on a Claude "
                + "access-denied handoff; the dashboard daemon degraded and released leadership. "
                + "Verify the dashboard's Claude access, then it will re-elect and retry.");
        }

        if (pendingCount > 0 && leaderState != "ready")
        {
            healthy = false;
            remediation.Add(leaderState == "none"
                ? $"{pendingCount} actor(s) have pending mail-wake work, but no dashboard leader "
                    + "is currently running for this Nitro instance. Start the dashboard to "
                    + "process it."
                : $"{pendingCount} actor(s) have pending mail-wake work, but the dashboard "
                    + "leader's lease has expired and nobody has re-acquired it. Start (or "
                    + "restart) the dashboard.");
        }

        return new DoctorAgentCommand.MailWakeDoctorResult(
            SchemaCurrent: true,
            leaderState,
            epoch,
            leaseExpiresInSeconds,
            lastError,
            (int)accessDeniedPendingTargets,
            pendingCount,
            acceptedCount,
            deferredCount,
            oldestDue is null ? null : (now - oldestDue.Value).TotalSeconds,
            healthy,
            remediation);
    }

    // Internal, not private: Dapper.AOT's generated interceptors live
    // outside this class and cannot reference a private nested type,
    // mirroring MailWakeDaemonCoordinator.LeaseRow.
    internal sealed class LeaderRow
    {
        public required long Epoch { get; init; }
        public required string ExpiresAt { get; init; }
        public string? LastError { get; init; }
    }

    internal sealed class OutboxRow
    {
        public required string Actor { get; init; }
        public required string DueAt { get; init; }
    }
}
