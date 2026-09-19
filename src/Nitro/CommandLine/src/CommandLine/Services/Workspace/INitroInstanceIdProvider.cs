namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves this machine's Nitro instance id: the value stored in
/// <c>agent_sessions.host</c> so pid liveness checks and reaping can tell a
/// session spawned on this host apart from a row left behind by another
/// machine sharing the same workspace over a network drive or a synced home
/// directory.
/// </summary>
internal interface INitroInstanceIdProvider
{
    /// <summary>
    /// Returns the OS machine identifier, hashed, when one can be read.
    /// Otherwise falls back to a generated id persisted under
    /// <paramref name="globalConfigDirectory"/>, created with an atomic
    /// create-or-read-winner so concurrent first use across processes
    /// yields one id.
    /// </summary>
    Task<string> GetIdAsync(string globalConfigDirectory, CancellationToken cancellationToken);
}
