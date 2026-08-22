namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A live Claude Code session found by walking this process's ancestors:
/// the pid of the ancestor process itself, and the identity
/// <c>~/.claude/sessions/&lt;pid&gt;.json</c> recorded for it.
/// </summary>
internal sealed record ClaudeAncestorSession(int Pid, string SessionId, string Cwd, string Name);

/// <summary>
/// Zero-config self-identification for <c>nitro agent session claim</c> on
/// Linux with Claude Code: walks the current process's ancestors looking for
/// one Claude Code registered a live session file for. Other platforms and
/// harnesses have no ancestor-walk path; binding for them happens at
/// SessionStart via the harness's launch environment instead (the hook
/// adapter bead).
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
