namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A row of <c>mail_wake_daemons</c>: the one persistent leader row for a
/// Nitro instance. <see cref="Epoch"/> increments every time a new owner
/// steals an expired lease, so a stale owner's write is rejected by its own
/// no-longer-current epoch, not merely by wall-clock expiry. See
/// <see cref="IMailWakeDaemonLeaderStore"/>.
/// </summary>
internal sealed record MailWakeDaemonRecord
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the mail_wake_daemons table.
    /// </summary>
    public const string Columns =
        "nitro_instance_id AS NitroInstanceId, owner_id AS OwnerId, epoch AS Epoch, "
        + "leased_at AS LeasedAt, expires_at AS ExpiresAt, last_error AS LastError";

    public required string NitroInstanceId { get; init; }
    public required string OwnerId { get; init; }

    /// <summary>
    /// Monotonically increasing from 1; a fresh value every time leadership
    /// changes hands.
    /// </summary>
    public required long Epoch { get; init; }

    public required DateTimeOffset LeasedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// At most 200 characters, or null when no failure has been recorded.
    /// </summary>
    public required string? LastError { get; init; }
}
