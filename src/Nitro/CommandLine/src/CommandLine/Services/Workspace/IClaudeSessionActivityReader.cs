namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Reads a live Claude Code harness session's idle/busy activity straight
/// from its session file at display time. Matches the plan's rule that
/// Claude activity is a read-through, never stored: there is no column for
/// it in <c>agent_sessions</c>, and every call re-reads the file fresh
/// instead of caching. Used by <c>agent list</c> and the TUI Agents tab,
/// both of which only ask for activity on an
/// <see cref="AgentSessionState.Online"/> claude-code row.
/// </summary>
internal interface IClaudeSessionActivityReader
{
    /// <summary>
    /// Returns the <c>status</c> field ("idle" or "busy", whatever Claude
    /// Code currently writes) from the session file carrying <paramref
    /// name="sessionId"/>. Returns null when no file carries it, or on any
    /// parse failure: this is a best-effort display enrichment, never a
    /// source of truth.
    /// </summary>
    string? GetStatus(string sessionId);
}
