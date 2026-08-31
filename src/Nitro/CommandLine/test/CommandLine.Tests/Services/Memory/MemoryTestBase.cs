using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

/// <summary>
/// A per-test temp directory laid out as an agent workspace, with a real
/// file system rooted at it and a fake clock, shared by the memory
/// foundation tests.
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

    // private protected, not protected: TestFileSystem is internal, and a
    // protected member of this public base could otherwise be reached from
    // a derived class outside the assembly, which could not see that type.
    private protected TestFileSystem FileSystem { get; }

    protected FakeTimeProvider TimeProvider { get; }

    protected string WorkspaceDirectory => AgentWorkspace.GetDirectory(WorkingDirectory);

    // The markdown layout the v11 import still reads from, and the only
    // thing AgentWorkspace's memory path helpers are used for now.
    protected string MemoryDirectory => AgentWorkspace.GetMemoryDirectory(WorkspaceDirectory);

    protected string CuratedDirectory => AgentWorkspace.GetMemoryCuratedDirectory(MemoryDirectory);

    protected string JournalDirectory => AgentWorkspace.GetMemoryJournalDirectory(MemoryDirectory);

    protected string LocalDirectory => AgentWorkspace.GetMemoryLocalDirectory(MemoryDirectory);

    /// <summary>
    /// Creates the workspace database memory now lives in, the same way
    /// `agent init` would. Every memory read and write goes through it, so a
    /// test that touches the store has to call this first.
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
