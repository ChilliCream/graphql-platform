using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class MemoryStoreTests : MemoryTestBase
{
    private readonly MemoryStore _store;

    public MemoryStoreTests() : base("nitro-memory-store-tests")
    {
        _store = new MemoryStore(FileSystem, TimeProvider);
    }

    [Fact]
    public async Task EnsureProjectWorkspaceAsync_Should_CreateCuratedJournalAndLocalDirectories()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);

        // assert
        Assert.True(Directory.Exists(CuratedDirectory));
        Assert.True(Directory.Exists(JournalDirectory));
        Assert.True(Directory.Exists(LocalDirectory));
    }

    [Fact]
    public async Task EnsureProjectWorkspaceAsync_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);
        var markerPath = Path.Combine(CuratedDirectory, "existing.md");
        File.WriteAllText(markerPath, "kept");

        // act
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);

        // assert
        Assert.Equal("kept", await File.ReadAllTextAsync(markerPath, cancellationToken));
    }

    [Fact]
    public void FindProjectWorkspaceDirectory_Should_ReturnNull_When_NoWorkspaceExists()
    {
        // arrange
        Directory.CreateDirectory(WorkingDirectory);

        // act
        var found = _store.FindProjectWorkspaceDirectory();

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task FindProjectWorkspaceDirectory_Should_ReturnDirectory_When_ProvisionedByEnsure()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, cancellationToken);

        // act
        var found = _store.FindProjectWorkspaceDirectory();

        // assert
        Assert.Equal(WorkspaceDirectory, found);
    }
}
