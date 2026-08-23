namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The Copilot CLI ancestor process this hook invocation was launched under:
/// just the pid. Like <see cref="CodexAncestorSession"/> (and unlike
/// <see cref="ClaudeAncestorSession"/>), Copilot has no live per-pid session
/// registry file to read, and no ancestor "peer name" is needed either: the
/// Copilot endpoint is a sibling task's extension (perles-net-k3j.16, out of
/// this ticket's scope), not something derivable from the ancestor process.
/// </summary>
internal sealed record CopilotAncestorSession(int Pid);

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
