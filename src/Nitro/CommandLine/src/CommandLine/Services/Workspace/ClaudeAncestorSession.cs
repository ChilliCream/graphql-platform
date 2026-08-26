namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A live Claude Code session found by walking this process's ancestors:
/// the pid of the ancestor process itself, and the identity
/// <c>~/.claude/sessions/&lt;pid&gt;.json</c> recorded for it.
/// </summary>
internal sealed record ClaudeAncestorSession(int Pid, string SessionId, string Cwd, string Name);
