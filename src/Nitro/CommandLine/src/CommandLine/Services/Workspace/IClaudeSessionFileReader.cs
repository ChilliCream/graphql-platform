namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Finds Claude session metadata by the session id supplied by a hook event.
/// </summary>
internal interface IClaudeSessionFileReader
{
    /// <summary>
    /// The session Claude Code recorded under <paramref name="sessionId"/>,
    /// or null when no session file carries it.
    /// </summary>
    ClaudeSessionFile? Find(string sessionId);
}
