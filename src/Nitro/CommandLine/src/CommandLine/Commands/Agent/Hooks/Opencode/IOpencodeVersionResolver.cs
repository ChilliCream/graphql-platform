namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

/// <summary>
/// Resolves the installed Opencode version for compatibility guidance.
/// </summary>
internal interface IOpencodeVersionResolver
{
    /// <summary>
    /// Returns the installed version, or <see langword="null"/> when it cannot be determined.
    /// </summary>
    Task<Version?> ResolveAsync(CancellationToken cancellationToken);
}
