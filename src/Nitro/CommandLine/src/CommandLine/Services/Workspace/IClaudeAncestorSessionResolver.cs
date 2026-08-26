namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A live Claude Code session found by walking this process's ancestors:
/// the pid of the ancestor process itself, and the identity
/// <c>~/.claude/sessions/&lt;pid&gt;.json</c> recorded for it.
/// </summary>
internal sealed record ClaudeAncestorSession(int Pid, string SessionId, string Cwd, string Name);

/// <summary>
/// Zero-config self-identification for Nitro commands running under Claude
/// Code on Linux: walks the current process's ancestors looking for one
/// Claude Code registered a live session file for.
/// </summary>
internal interface IClaudeAncestorSessionResolver
{
    /// <summary>
    /// Returns the nearest ancestor process that has a live Claude Code
    /// session file, or null when none is found (not running on Linux, no
    /// Claude Code ancestor, or the ancestor chain was exhausted).
    /// </summary>
    ClaudeAncestorSession? Resolve();
}
