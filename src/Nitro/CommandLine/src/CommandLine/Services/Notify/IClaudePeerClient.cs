namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Sends one machine-generated digest to a local Claude Code or Venti interactive
/// session through its advertised peer-protocol endpoint.
/// </summary>
internal interface IClaudePeerClient
{
    /// <summary>
    /// Sends <paramref name="message"/> to the session <paramref name="sessionId"/> of
    /// <paramref name="harness"/>, returning <see cref="ClaudePeerSendOutcome.Unsupported"/>
    /// for a harness without peer endpoints.
    /// </summary>
    Task<ClaudePeerSendOutcome> SendAsync(
        string harness,
        string sessionId,
        string message,
        CancellationToken cancellationToken);
}
