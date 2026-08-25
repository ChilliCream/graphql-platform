namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A row of <c>mail_wake_outbox</c>: the durable wake-intent queue head for
/// one actor on one Nitro instance. No command writes <c>requested_generation</c>
/// or <c>due_at</c> yet; the mail store that owns enqueueing lands in a later
/// bead. <see cref="IMailWakeBatchStore"/> reads and settles this row as part
/// of claiming and completing a batch.
/// </summary>
internal sealed record MailWakeOutboxRecord
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the mail_wake_outbox table.
    /// </summary>
    public const string Columns =
        "nitro_instance_id AS NitroInstanceId, actor AS Actor, "
        + "requested_generation AS RequestedGeneration, settled_generation AS SettledGeneration, "
        + "due_at AS DueAt, updated_at AS UpdatedAt";

    public required string NitroInstanceId { get; init; }
    public required string Actor { get; init; }

    /// <summary>
    /// Counts every distinct wake intent enqueued for this actor on this
    /// instance; monotonically non-decreasing.
    /// </summary>
    public required long RequestedGeneration { get; init; }

    /// <summary>
    /// The highest generation a completed batch has actually settled. Never
    /// ahead of <see cref="RequestedGeneration"/>, and never advanced past a
    /// generation whose claiming batch has not itself completed, so a
    /// generation requested after a batch was claimed is never silently
    /// settled by that batch's completion.
    /// </summary>
    public required long SettledGeneration { get; init; }

    /// <summary>
    /// The earliest time outstanding work for this actor should next be
    /// attempted.
    /// </summary>
    public required DateTimeOffset DueAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
