using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

/// <summary>
/// The self-healing index coherence contract at the index layer: a fresh
/// clone (curated markdown present, no index yet), a manual edit outside the
/// CLI (stale fingerprint), and an interrupted write (a corrupt index file)
/// must all rebuild transparently, and a rebuild that fails partway must
/// leave the previous valid index untouched.
/// </summary>
public sealed class MemoryFtsIndexTests : MemoryTestBase
{
    public MemoryFtsIndexTests() : base("nitro-memory-fts-index-tests")
    {
    }

    private string IndexPath => Services.Workspace.AgentWorkspace.GetMemoryIndexDatabasePath(LocalDirectory);

    private async Task SeedCuratedFileAsync(string id, string body, string tags = "[]")
    {
        Directory.CreateDirectory(CuratedDirectory);

        var content = $"""
            ---
            schema: 1
            id: {id}
            type: fact
            tags: {tags}
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: test-agent
            ---
            {body}
            """;

        await File.WriteAllTextAsync(
            Path.Combine(CuratedDirectory, id + ".md"), content, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SearchAsync_Should_BuildIndex_When_NoIndexExistsYet()
    {
        // arrange: a fresh clone, curated markdown present, no index.db.
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedCuratedFileAsync("01hqzxk8xdtd3fk3f0z7c5g9aa", "the quick brown fox");
        Assert.False(File.Exists(IndexPath));

        // act
        var ids = await MemoryFtsIndex.SearchAsync(
            FileSystem, CuratedDirectory, LocalDirectory, "\"quick\"", null, [], null, cancellationToken);

        // assert
        Assert.True(File.Exists(IndexPath));
        Assert.Equal(["01hqzxk8xdtd3fk3f0z7c5g9aa"], ids);
    }

    [Fact]
    public async Task SearchAsync_Should_RebuildStaleIndex_When_ACuratedFileWasEditedOutsideTheCli()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        const string id = "01hqzxk8xdtd3fk3f0z7c5g9aa";
        await SeedCuratedFileAsync(id, "the quick brown fox");
        await MemoryFtsIndex.SearchAsync(
            FileSystem, CuratedDirectory, LocalDirectory, "\"quick\"", null, [], null, cancellationToken);
        var builtAt = File.GetLastWriteTimeUtc(IndexPath);

        // A manual edit outside the CLI: mtime moves forward, which must
        // invalidate the stored fingerprint.
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        await SeedCuratedFileAsync(id, "the slow red fox");

        // act
        var ids = await MemoryFtsIndex.SearchAsync(
            FileSystem, CuratedDirectory, LocalDirectory, "\"slow\"", null, [], null, cancellationToken);

        // assert
        Assert.Equal([id], ids);
        Assert.True(File.GetLastWriteTimeUtc(IndexPath) >= builtAt);
    }

    [Fact]
    public async Task SearchAsync_Should_RebuildCorruptIndex_When_APriorWriteWasInterrupted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        const string id = "01hqzxk8xdtd3fk3f0z7c5g9aa";
        await SeedCuratedFileAsync(id, "the quick brown fox");
        await MemoryFtsIndex.SearchAsync(
            FileSystem, CuratedDirectory, LocalDirectory, "\"quick\"", null, [], null, cancellationToken);

        // Simulate a process killed mid-rebuild: present but not a valid
        // SQLite database.
        await File.WriteAllBytesAsync(IndexPath, "not a sqlite database"u8.ToArray(), cancellationToken);

        // act
        var ids = await MemoryFtsIndex.SearchAsync(
            FileSystem, CuratedDirectory, LocalDirectory, "\"quick\"", null, [], null, cancellationToken);

        // assert
        Assert.Equal([id], ids);
    }

    [Fact]
    public async Task SearchAsync_Should_LeaveLastValidIndexUntouched_When_ARebuildFindsAMalformedFile()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        const string id = "01hqzxk8xdtd3fk3f0z7c5g9aa";
        await SeedCuratedFileAsync(id, "the quick brown fox");
        await MemoryFtsIndex.SearchAsync(
            FileSystem, CuratedDirectory, LocalDirectory, "\"quick\"", null, [], null, cancellationToken);
        var validIndexBytes = await File.ReadAllBytesAsync(IndexPath, cancellationToken);

        // A second file with corrupt frontmatter forces the next rebuild to
        // fail; mtime moves forward so the fingerprint check even attempts
        // the rebuild.
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(CuratedDirectory, "mem-09876543210987654321098.md"),
            "not frontmatter at all",
            cancellationToken);

        // act
        var exception = await Record.ExceptionAsync(() => MemoryFtsIndex.SearchAsync(
            FileSystem, CuratedDirectory, LocalDirectory, "\"quick\"", null, [], null, cancellationToken));

        // assert: the failure surfaces loudly rather than being swallowed,
        // and the previously valid index file is untouched, not replaced by
        // a partial rebuild.
        Assert.IsType<ExitException>(exception);
        Assert.Equal(validIndexBytes, await File.ReadAllBytesAsync(IndexPath, cancellationToken));
    }
}
