using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

internal interface IMemoryStore
{
    /// <summary>
    /// Finds the nearest project agent workspace directory at or above the
    /// current directory that has a project memory store, per
    /// <see cref="AgentWorkspace.FindMemory"/>. Returns null when none
    /// exists.
    /// </summary>
    string? FindProjectWorkspaceDirectory();

    /// <summary>
    /// Creates the curated, journal, and local index directories of the
    /// project memory store under the given agent workspace directory.
    /// Idempotent: directories that already exist are left as they are.
    /// </summary>
    Task EnsureProjectWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken);
}
