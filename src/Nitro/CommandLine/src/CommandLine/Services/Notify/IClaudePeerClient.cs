namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Sends one machine-generated digest to a local Claude Code interactive
/// session through its advertised peer-protocol endpoint.
/// </summary>
internal interface IClaudePeerClient
{
    Task<ClaudePeerSendResult> SendAsync(
        int pid,
        string sessionId,
        string message,
        CancellationToken cancellationToken);
}
internal enum ClaudePeerSendResult
{
    // Claude Code's raw peer endpoint closes with EOF and no inline
    // delivery receipt. Ok means the complete protocol payload was written
    // to the validated local endpoint, which is the behavior live-verified
    // on 2.1.226 and 2.1.241.
    Ok,
    Unsupported,
    EndpointGone,
    Error
}
