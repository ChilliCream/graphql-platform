using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

/// <summary>
/// Runs memory commands against a real file system workspace in a per-test
/// temp directory named "acme", mirroring <c>MailCommandTestBase</c>.
/// </summary>
public abstract class MemoryCommandTestBase : CommandTestBase
{
    private readonly DirectoryInfo _tempRoot;

    protected MemoryCommandTestBase(NitroCommandFixture fixture) : base(fixture)
    {
        SetupNoAuthentication();
        SetupEnvironmentVariable("MEMORY_ACTOR", "test-agent");

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

    protected async Task InitWorkspaceAsync()
    {
        var result = await ExecuteCommandAsync("agent", "init");
        Assert.Equal(0, result.ExitCode);
    }

    /// <summary>
    /// Creates an <see cref="IMemoryStore"/> bound to this test's workspace
    /// and clock, for seeding data without going through the CLI.
    /// </summary>
    internal MemoryStore CreateStore()
        => new(new TestFileSystem(WorkingDirectory), FakeTime);

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

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _tempRoot.Delete(recursive: true);
    }
}
