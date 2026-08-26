namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

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
