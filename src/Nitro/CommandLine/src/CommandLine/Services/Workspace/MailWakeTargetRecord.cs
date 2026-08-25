namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A row of <c>mail_wake_targets</c>: a durable record of one full session
/// generation a <see cref="MailWakeBatchRecord"/> dispatched to when
/// claimed. Cascades only with its owning batch, never with
/// <c>agent_sessions</c>, so it survives that session ending or being
/// reaped. See <see cref="IMailWakeBatchStore"/>.
/// </summary>
internal sealed record MailWakeTargetRecord
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the mail_wake_targets table.
    /// </summary>
    public const string Columns =
        "batch_id AS BatchId, harness AS Harness, session_id AS SessionId, host AS Host, "
        + "pid AS Pid, proc_start AS ProcStart, status AS Status, "
        + "offered_generation AS OfferedGeneration, accepted_generation AS AcceptedGeneration, "
        + "last_error AS LastError, updated_at AS UpdatedAt";

    public required string BatchId { get; init; }
    public required string Harness { get; init; }
    public required string SessionId { get; init; }
    public required string Host { get; init; }
    public required int Pid { get; init; }
    public required string ProcStart { get; init; }

    /// <summary>
    /// One of <c>"pending"</c>, <c>"delivered"</c>, <c>"satisfied"</c>,
    /// <c>"delegated"</c>, <c>"skipped"</c>, or <c>"failed"</c>.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// The generation durably offered to this exact target (for example, a
    /// dashboard delegation offer), or null when none has been offered.
    /// </summary>
    public required long? OfferedGeneration { get; init; }

    /// <summary>
    /// The generation this exact target has accepted responsibility for, or
    /// null when none has been accepted. Target-qualified: acceptance is
    /// scoped to this one target, never implied for the rest of the batch.
    /// </summary>
    public required long? AcceptedGeneration { get; init; }

    /// <summary>
    /// At most 200 characters, or null when no failure has been recorded.
    /// </summary>
    public required string? LastError { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
