using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

/// <summary>
/// Provides a temporary workspace directory, a real file system, and a fake clock
/// for memory tests. Deletes the temporary directory on disposal.
/// </summary>
public abstract class MemoryTestBase : IDisposable
{
    private readonly DirectoryInfo _tempRoot;

    protected MemoryTestBase(string tempDirectoryPrefix)
    {
        _tempRoot = Directory.CreateTempSubdirectory(tempDirectoryPrefix);
        WorkingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(WorkingDirectory);

        FileSystem = new TestFileSystem(WorkingDirectory);
        TimeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
    }

    protected string WorkingDirectory { get; }

    private protected TestFileSystem FileSystem { get; }

    protected FakeTimeProvider TimeProvider { get; }

    protected string WorkspaceDirectory => AgentWorkspace.GetDirectory(WorkingDirectory);

    protected string MemoryDirectory => AgentWorkspace.GetMemoryDirectory(WorkspaceDirectory);

    protected string CuratedDirectory => AgentWorkspace.GetMemoryCuratedDirectory(MemoryDirectory);

    protected string JournalDirectory => AgentWorkspace.GetMemoryJournalDirectory(MemoryDirectory);

    protected string LocalDirectory => AgentWorkspace.GetMemoryLocalDirectory(MemoryDirectory);

    /// <summary>
    /// Creates the workspace directory and initializes its database.
    /// </summary>
    protected void InitializeWorkspace()
    {
        Directory.CreateDirectory(WorkspaceDirectory);

        using var connection = new AgentDatabase()
            .InitializeAsync(WorkspaceDirectory, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);
}
