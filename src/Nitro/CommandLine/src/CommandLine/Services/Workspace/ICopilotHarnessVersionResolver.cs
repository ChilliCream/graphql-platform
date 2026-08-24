namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the exact running Copilot CLI version for a live session, from
/// its session-state events file when one exists, falling back to the live
/// ancestor process's own <c>--version</c> output.
/// </summary>
internal interface ICopilotHarnessVersionResolver
{
    /// <summary>
    /// Returns the version for the session identified by <paramref
    /// name="sessionId"/>, or empty when neither the session-state file nor
    /// <paramref name="ancestorPid"/>'s own version output resolves one.
    /// </summary>
    string Resolve(string sessionId, int ancestorPid);
}
