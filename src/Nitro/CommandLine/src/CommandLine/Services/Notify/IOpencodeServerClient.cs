namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Sends prompts to, and checks the health of, an opencode server registered
/// for a live session.
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
