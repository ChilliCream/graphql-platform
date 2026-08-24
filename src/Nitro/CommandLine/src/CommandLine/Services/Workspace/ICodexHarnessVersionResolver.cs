namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the exact running Codex version for a live session, from its
/// rollout file when one exists, falling back to the live ancestor
/// process's executable path.
/// </summary>
internal interface ICodexHarnessVersionResolver
{
    /// <summary>
    /// Returns the version for the session identified by <paramref
    /// name="sessionId"/>, or empty when neither the rollout file nor
    /// <paramref name="ancestorPid"/>'s executable path resolves one.
    /// </summary>
    string Resolve(string sessionId, int ancestorPid);
}
