namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A row of <c>mail_wake_batches</c>: one immutable claim against a
/// <see cref="MailWakeOutboxRecord"/>, identified by <see cref="BatchId"/>
/// for its whole lifetime. <see cref="ClaimedGeneration"/> is fixed at claim
/// time; <see cref="OwnerId"/>, <see cref="AttemptId"/>, and
/// <see cref="ExpiresAt"/> fence every renewal and completion against a
/// stale or superseded claimant. See <see cref="IMailWakeBatchStore"/>.
/// </summary>
internal sealed record MailWakeBatchRecord
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the mail_wake_batches table.
    /// </summary>
    public const string Columns =
        "batch_id AS BatchId, nitro_instance_id AS NitroInstanceId, actor AS Actor, "
        + "claimed_generation AS ClaimedGeneration, owner_id AS OwnerId, attempt_id AS AttemptId, "
        + "status AS Status, claimed_at AS ClaimedAt, expires_at AS ExpiresAt, "
        + "completed_at AS CompletedAt, last_error AS LastError";

    public required string BatchId { get; init; }
    public required string NitroInstanceId { get; init; }
    public required string Actor { get; init; }

    /// <summary>
    /// The outbox's <c>requested_generation</c> snapshotted at the moment
    /// this batch was claimed.
    /// </summary>
    public required long ClaimedGeneration { get; init; }

    public required string OwnerId { get; init; }
    public required string AttemptId { get; init; }

    /// <summary>
    /// One of <c>"active"</c>, <c>"completed"</c>, or <c>"released"</c>. At
    /// most one <c>active</c> batch exists per (instance, actor) at a time.
    /// </summary>
    public required string Status { get; init; }

    public required DateTimeOffset ClaimedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// At most 200 characters, or null when no failure has been recorded.
    /// </summary>
    public required string? LastError { get; init; }
}
