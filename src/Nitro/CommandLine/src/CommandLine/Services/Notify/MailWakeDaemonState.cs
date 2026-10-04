namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The coordinator's observed leadership and shutdown states.
/// </summary>
internal enum MailWakeDaemonState
{
    Standby,
    Ready,
    Degraded,
    Stopping
}
