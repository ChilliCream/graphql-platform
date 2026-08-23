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

/// <summary>
/// Zero-config process identity for Codex hook and notify adapters: walks
/// the current process's ancestors (Linux-first, mirrors
/// <see cref="IClaudeAncestorSessionResolver"/>) looking for the Codex CLI
/// process itself. A hooks.json/notify command runs through a shell
/// (<c>sh -c "..."</c>), so the immediate parent is the shell, not Codex -
/// the walk has to look past it.
/// </summary>
internal interface ICodexAncestorSessionResolver
{
    /// <summary>
    /// Returns the nearest ancestor process identified as the Codex CLI, or
    /// null when none is found (not running on Linux, or the ancestor chain
    /// was exhausted without a match).
    /// </summary>
    CodexAncestorSession? Resolve();
}
