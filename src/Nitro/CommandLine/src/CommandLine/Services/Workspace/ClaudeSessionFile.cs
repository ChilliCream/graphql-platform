namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// One Claude Code session as its own session file describes it: the pid
/// running it, and the peer name other sessions address it by.
/// </summary>
internal sealed record ClaudeSessionFile(int Pid, string SessionId, string Cwd, string Name);
