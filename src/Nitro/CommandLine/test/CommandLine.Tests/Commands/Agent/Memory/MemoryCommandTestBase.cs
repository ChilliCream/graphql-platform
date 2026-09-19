using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Memory;

/// <summary>
/// Runs memory commands against a real file system in a temporary workspace.
/// Deletes the temporary directory on disposal.
/// </summary>
public abstract class MemoryCommandTestBase : CommandTestBase
{
    private readonly DirectoryInfo _tempRoot;

    protected MemoryCommandTestBase(NitroCommandFixture fixture) : base(fixture)
    {
        SetupNoAuthentication();
        SetupActingActor("test-agent");
        DefaultActor = "test-agent";

        _tempRoot = Directory.CreateTempSubdirectory("nitro-memory-command-tests");
        WorkingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(WorkingDirectory);
        SetupFileSystem(new TestFileSystem(WorkingDirectory));
    }

    protected string WorkingDirectory { get; }

    protected string WorkspaceDirectory
        => AgentWorkspace.GetDirectory(WorkingDirectory);

    protected string MemoryDirectory
        => AgentWorkspace.GetMemoryDirectory(WorkspaceDirectory);

    protected string CuratedDirectory
        => AgentWorkspace.GetMemoryCuratedDirectory(MemoryDirectory);

    protected string JournalDirectory
        => AgentWorkspace.GetMemoryJournalDirectory(MemoryDirectory);

    protected string LocalDirectory
        => AgentWorkspace.GetMemoryLocalDirectory(MemoryDirectory);

    /// <summary>
    /// Returns all curated memories in the workspace, newest first.
    /// </summary>
    internal Task<IReadOnlyList<MemoryRecord>> ReadCuratedAsync()
        => CreateStore().GetRecentCuratedAsync(limit: null, TestContext.Current.CancellationToken);

    /// <summary>
    /// Returns all journal entries in the workspace, newest first.
    /// </summary>
    internal Task<IReadOnlyList<MemoryJournalEntry>> ReadJournalAsync()
        => CreateStore().GetRecentJournalAsync(limit: null, TestContext.Current.CancellationToken);

    protected async Task InitWorkspaceAsync()
    {
        var result = await ExecuteCommandAsync("agent", "init");
        Assert.Equal(0, result.ExitCode);
    }

    /// <summary>
    /// Creates an <see cref="IMemoryStore"/> bound to the test's workspace database and clock.
    /// </summary>
    internal MemoryStore CreateStore()
        => new(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase());

    /// <summary>
    /// Saves a curated memory directly against the store.
    /// </summary>
    internal Task<MemoryRecord> SeedMemoryAsync(
        string text,
        string type = "fact",
        IReadOnlyList<string>? tags = null,
        string actor = "test-agent")
        => CreateStore().SaveAsync(
            new MemoryRecordCreation
            {
                Text = text,
                Type = type,
                Tags = tags ?? [],
                Actor = actor
            },
            TestContext.Current.CancellationToken);

    /// <summary>
    /// Logs a journal entry directly against the store.
    /// </summary>
    internal Task<MemoryJournalEntry> SeedJournalEntryAsync(
        string text,
        string actor = "test-agent")
        => CreateStore().LogAsync(
            new MemoryJournalEntryCreation
            {
                Text = text,
                Actor = actor
            },
            TestContext.Current.CancellationToken);

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _tempRoot.Delete(recursive: true);
    }
}
