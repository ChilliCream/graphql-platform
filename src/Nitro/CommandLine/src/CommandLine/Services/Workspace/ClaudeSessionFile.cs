namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// One Claude Code session as its own session file describes it: the peer
/// name other sessions address it by, and the harness version running it.
/// </summary>
internal sealed record ClaudeSessionFile(string SessionId, string Cwd, string Name, string Version);
