namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

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
