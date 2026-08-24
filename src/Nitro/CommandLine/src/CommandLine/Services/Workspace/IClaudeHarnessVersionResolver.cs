namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the exact running Claude Code version for a live session, from
/// the same <c>~/.claude/sessions/&lt;pid&gt;.json</c> file
/// <see cref="IClaudeAncestorSessionResolver"/> reads identity from.
/// </summary>
internal interface IClaudeHarnessVersionResolver
{
    /// <summary>
    /// Returns the version recorded for <paramref name="pid"/>'s session
    /// file, or empty when the file is unavailable, malformed, or its
    /// recorded process start no longer matches the pid's actual one (the
    /// OS reused the pid since the file was written).
    /// </summary>
    string Resolve(int pid);
}
