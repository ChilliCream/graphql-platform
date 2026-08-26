namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Zero-config process identity for Copilot hook adapters: walks the current
/// process's ancestors (Linux-first, mirrors
/// <see cref="ICodexAncestorSessionResolver"/>) looking for the Copilot CLI
/// process itself. A hooks-dir command runs through a shell, so the immediate
/// parent is the shell, not Copilot - the walk has to look past it.
/// </summary>
internal interface ICopilotAncestorSessionResolver
{
    /// <summary>
    /// Returns the nearest ancestor process identified as the Copilot CLI, or
    /// null when none is found (not running on Linux, or the ancestor chain
    /// was exhausted without a match).
    /// </summary>
    CopilotAncestorSession? Resolve();
}
