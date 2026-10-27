namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The ping cooldown, transport slot lease duration, and maximum digest and transport duration.
/// </summary>
internal static class PingPolicy
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan HardTimeout = TimeSpan.FromSeconds(20);
}
