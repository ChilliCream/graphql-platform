namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// One <see cref="IMailWakeDaemonCoordinator"/> instance's current, in-memory
/// view of its own leadership: <see cref="State"/>, the owner id and epoch it
/// currently holds (both null outside <see cref="MailWakeDaemonState.Ready"/>),
/// when its held lease is next due to expire absent a renewal, and a bounded
/// diagnostic for the most recent infrastructure or dispatch failure. Not a
/// durable record; a fresh coordinator observes the durable
/// <c>mail_wake_daemons</c> row directly rather than through this type.
/// </summary>
internal sealed record MailWakeDaemonStatus(
    MailWakeDaemonState State,
    string? OwnerId,
    long? Epoch,
    DateTimeOffset? LeaseExpiresAt,
    string? LastError)
{
    public static readonly MailWakeDaemonStatus Initial =
        new(MailWakeDaemonState.Standby, null, null, null, null);
}
