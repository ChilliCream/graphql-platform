namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Sends prompts to an opencode session and checks server and session reachability.
/// </summary>
internal interface IOpencodeServerClient
{
    /// <summary>
    /// Sends <paramref name="text"/> as a user prompt for the supplied
    /// opencode session.
    /// </summary>
    Task<string> PushMessageAsync(
        string serverUrl,
        string sessionId,
        string text,
        string? secret,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks that the server and its supplied session are both reachable.
    /// </summary>
    Task<string> PingAsync(
        string serverUrl,
        string sessionId,
        string? secret,
        CancellationToken cancellationToken);
}
