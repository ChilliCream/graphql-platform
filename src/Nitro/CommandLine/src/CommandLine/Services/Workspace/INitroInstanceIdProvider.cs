namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves this machine's Nitro instance id: the value stored in
/// <c>agent_sessions.host</c> so pid liveness checks and reaping can tell a
/// session spawned on this host apart from a row left behind by another
/// machine sharing the same workspace over a network drive or a synced home
/// directory. A naive `~/.nitro/instance-id` would roam with a cloned or
/// shared home and recreate the exact cross-host hazard this exists to
/// prevent, so the id is anchored to the machine, not the workspace or the
/// home directory.
/// </summary>
internal interface INitroInstanceIdProvider
{
    /// <summary>
    /// Returns the OS machine identifier, hashed, when one can be read.
    /// Otherwise falls back to a generated id persisted under
    /// <paramref name="globalConfigDirectory"/> (the platform application
    /// data directory Nitro already uses for global state), created with an
    /// atomic create-or-read-winner so concurrent first use across processes
    /// yields one id. Regenerating the fallback (a fresh machine image, a
    /// deleted app-data directory) strands old rows as remote; that is
    /// accepted, documented behavior, not a bug this method needs to guard
    /// against.
    /// </summary>
    Task<string> GetIdAsync(string globalConfigDirectory, CancellationToken cancellationToken);
}
