namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the Nitro instance id used to identify the owner of session presence.
/// </summary>
internal interface INitroInstanceIdProvider
{
    /// <summary>
    /// Returns a hash of the platform machine identifier when available, otherwise a
    /// generated id persisted under <paramref name="globalConfigDirectory"/>.
    /// Concurrent first use shares the same persisted fallback id.
    /// </summary>
    Task<string> GetIdAsync(string globalConfigDirectory, CancellationToken cancellationToken);
}
