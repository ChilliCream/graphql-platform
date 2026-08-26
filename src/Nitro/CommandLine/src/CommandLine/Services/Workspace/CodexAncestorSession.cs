namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The Codex ancestor process this hook or notify invocation was launched
/// under: just the pid. Unlike <see cref="ClaudeAncestorSession"/>, Codex has
/// no live per-pid session registry file to read (no
/// <c>~/.codex/sessions/&lt;pid&gt;.json</c> equivalent), and no ancestor
/// "peer name" is needed either - the Codex endpoint address is the thread
/// id itself, already present on every event's payload.
/// </summary>
internal sealed record CodexAncestorSession(int Pid);
