namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Reads Claude session activity from its session file without caching it.
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
