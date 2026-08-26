namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Sends one machine-generated digest to a local Claude Code interactive
/// session through its advertised peer-protocol endpoint.
/// </summary>
internal interface IClaudePeerClient
{
    Task<ClaudePeerSendOutcome> SendAsync(
        int pid,
        string sessionId,
        string message,
        CancellationToken cancellationToken);
}
