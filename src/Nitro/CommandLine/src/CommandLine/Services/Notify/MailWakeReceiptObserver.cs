using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Reads a <see cref="MailWakeReceipt"/>'s durable dispatch state directly
/// from <c>mail_wake_batches</c> and <c>mail_wake_targets</c>.
/// <see cref="IMailWakeBatchStore"/> exposes no read primitive over those
/// tables, so this observer opens its own connection the same way
/// <c>MailWakeBatchStore</c> does (<see cref="AgentWorkspace.Find"/> then
/// <see cref="AgentDatabase.ConnectAsync"/>) rather than being added to that
/// store. Every <see cref="ObserveAsync"/> call runs its own fresh read
/// transaction and caches nothing across calls, so two calls for the same
/// receipt see whatever changed between them.
/// </summary>
internal sealed class MailWakeReceiptObserver(
    IFileSystem fileSystem,
    AgentDatabase database,
    INitroInstanceIdProvider instanceIdProvider,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : IMailWakeReceiptObserver
{
    public async Task<MailWakeObservation> ObserveAsync(
        MailWakeReceipt receipt, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        var nitroInstanceId = await instanceIdProvider.GetIdAsync(
            globalConfigDirectoryProvider.GetDirectory(), cancellationToken);

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var settledGeneration = await connection.QueryFirstOrDefaultAsync<long?>(
            """
            SELECT settled_generation
            FROM mail_wake_outbox
            WHERE nitro_instance_id = @nitroInstanceId AND actor = @actor
            """,
            new { nitroInstanceId, actor = receipt.Actor, cancellationToken },
            transaction) ?? 0;

        var batchId = await connection.QueryFirstOrDefaultAsync<string>(
            """
            SELECT batch_id
            FROM mail_wake_batches
            WHERE nitro_instance_id = @nitroInstanceId AND actor = @actor
              AND claimed_generation >= @generation
            ORDER BY claimed_at DESC
            LIMIT 1
            """,
            new { nitroInstanceId, actor = receipt.Actor, generation = receipt.Generation, cancellationToken },
            transaction);

        if (batchId is null)
        {
            await transaction.CommitAsync(cancellationToken);

            // No batch has claimed this generation yet: pending while the
            // outbox has not caught up to it, or failed (its batch's target
            // rows are gone) once the outbox has settled at or past it
            // without one ever showing up here.
            var status = settledGeneration < receipt.Generation
                ? MailWakeTargetStatus.Pending
                : MailWakeTargetStatus.Failed;

            return new MailWakeObservation(
                receipt.Actor, receipt.Generation, status, WakeReceiptAggregator.IsZero(status), []);
        }

        var targetRows = (await connection.QueryAsync<TargetRow>(
            """
            SELECT harness AS Harness, session_id AS SessionId, host AS Host, pid AS Pid, proc_start AS ProcStart,
                   status AS Status, offered_generation AS OfferedGeneration,
                   accepted_generation AS AcceptedGeneration, last_error AS LastError
            FROM mail_wake_targets
            WHERE batch_id = @batchId
            """,
            new { batchId, cancellationToken },
            transaction)).AsList();

        await transaction.CommitAsync(cancellationToken);

        var targets = targetRows
            .Select(row => new ActorWakeTargetReceipt(
                new AgentSessionGeneration(row.Harness, row.SessionId, row.Host, row.Pid, row.ProcStart),
                row.Status,
                row.OfferedGeneration,
                row.AcceptedGeneration,
                row.LastError))
            .ToList();

        var aggregate = WakeReceiptAggregator.Aggregate(targets.Select(t => t.Status).ToList());

        return new MailWakeObservation(
            receipt.Actor, receipt.Generation, aggregate, WakeReceiptAggregator.IsZero(aggregate), targets);
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }

    // Internal, not private: Dapper.AOT's generated interceptors live
    // outside this class and cannot reference a private nested type.
    internal sealed class TargetRow
    {
        public required string Harness { get; init; }
        public required string SessionId { get; init; }
        public required string Host { get; init; }
        public required int Pid { get; init; }
        public required string ProcStart { get; init; }
        public required string Status { get; init; }
        public long? OfferedGeneration { get; init; }
        public long? AcceptedGeneration { get; init; }
        public string? LastError { get; init; }
    }
}
