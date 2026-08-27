namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the exact running Codex version for a live session from its
/// rollout file.
/// </summary>
internal interface ICodexHarnessVersionResolver
{
    /// <summary>
    /// Returns the version for the session identified by <paramref
    /// name="sessionId"/>, or empty when no rollout file resolves one.
    /// </summary>
    string Resolve(string sessionId);
}
