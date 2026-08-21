using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The project memory store's directory provisioning and discovery. This is
/// a skeleton: the read and write verbs (save, log, update, forget, show,
/// search, recent, context, promote, tags, reindex, doctor, where) land in
/// later slices; this slice only owns the on-disk layout the workspace
/// needs before any of them can run.
/// </summary>
internal sealed class MemoryStore(IFileSystem fileSystem) : IMemoryStore
{
    public string? FindProjectWorkspaceDirectory()
        => AgentWorkspace.FindMemory(fileSystem, fileSystem.GetCurrentDirectory());

    public Task EnsureProjectWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken)
    {
        var memoryDirectory = AgentWorkspace.GetMemoryDirectory(workspaceDirectory);

        CreateIfMissing(AgentWorkspace.GetMemoryCuratedDirectory(memoryDirectory));
        CreateIfMissing(AgentWorkspace.GetMemoryJournalDirectory(memoryDirectory));
        CreateIfMissing(AgentWorkspace.GetMemoryLocalDirectory(memoryDirectory));

        return Task.CompletedTask;
    }

    private void CreateIfMissing(string directory)
    {
        if (!fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }
    }
}
