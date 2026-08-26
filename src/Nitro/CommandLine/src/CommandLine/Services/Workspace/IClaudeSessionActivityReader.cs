namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Reads a live Claude Code harness session's idle/busy activity straight
/// from <c>~/.claude/sessions/&lt;pid&gt;.json</c> at display time. Matches
/// the plan's rule that Claude activity is a read-through, never stored:
/// there is no column for it in <c>agent_sessions</c>, and every call
/// re-reads the file fresh instead of caching. Used by <c>agent list</c> and
/// the TUI Agents tab, both of which only ask for activity on sessions this
/// Nitro instance can actually see the process for (an
/// <see cref="AgentSessionState.Online"/> claude-code row).
/// </summary>
internal interface IClaudeSessionActivityReader
{
    /// <summary>
    /// Returns the session file's <c>status</c> field ("idle" or "busy",
    /// whatever Claude Code currently writes) for <paramref name="pid"/>,
    /// but only when the file's own <c>sessionId</c> still matches
    /// <paramref name="sessionId"/> - a pid can be reused by an unrelated
    /// process, or the file can already belong to a newer generation than
    /// the one the caller is asking about. Returns null on any mismatch,
    /// missing file, or parse failure: this is a best-effort display
    /// enrichment, never a source of truth.
    /// </summary>
    string? GetStatus(int pid, string sessionId);
}
