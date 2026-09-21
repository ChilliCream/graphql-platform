namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The coordinator's latest leadership snapshot, with owner and epoch values that
/// may remain set during stopping or degradation until the next standby observation.
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
