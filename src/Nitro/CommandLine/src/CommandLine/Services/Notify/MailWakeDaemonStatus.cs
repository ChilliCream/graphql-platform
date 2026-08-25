namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// One <see cref="IMailWakeDaemonCoordinator"/> instance's current, in-memory
/// view of its own leadership. <see cref="OwnerId"/> and <see cref="Epoch"/>
/// are both null outside <see cref="MailWakeDaemonState.Ready"/>.
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
