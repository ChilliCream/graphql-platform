namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Finds the Claude Code session a hook event names. The event carries the
/// session id, so the session is identified exactly rather than inferred
/// from the process tree.
/// </summary>
internal interface IClaudeSessionFileReader
{
    /// <summary>
    /// The session Claude Code recorded under <paramref name="sessionId"/>,
    /// or null when no session file carries it.
    /// </summary>
    ClaudeSessionFile? Find(string sessionId);
}
