namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

/// <summary>
/// Tests file creation without overwrite, replacement, and cleanup of
/// abandoned temporary files.
/// </summary>
public sealed class AtomicFileSystemTests : MemoryTestBase
{
    public AtomicFileSystemTests() : base("nitro-memory-atomic-fs-tests")
    {
    }

    [Fact]
    public async Task CreateFileAtomicAsync_Should_CreateFile_When_DestinationDoesNotExist()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(WorkingDirectory);
        var path = Path.Combine(WorkingDirectory, "note.md");

        // act
        await FileSystem.CreateFileAtomicAsync(path, "hello", cancellationToken);

        // assert
        Assert.True(File.Exists(path));
        Assert.Equal("hello", await File.ReadAllTextAsync(path, cancellationToken));
    }

    [Fact]
    public async Task CreateFileAtomicAsync_Should_Throw_When_DestinationAlreadyExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(WorkingDirectory);
        var path = Path.Combine(WorkingDirectory, "note.md");
        await FileSystem.CreateFileAtomicAsync(path, "original", cancellationToken);

        // act
        await Assert.ThrowsAsync<IOException>(
            () => FileSystem.CreateFileAtomicAsync(path, "conflicting", cancellationToken));

        // assert
        Assert.Equal("original", await File.ReadAllTextAsync(path, cancellationToken));
    }

    [Fact]
    public async Task CreateFileAtomicAsync_Should_NotLeaveTempFile_When_DestinationAlreadyExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(WorkingDirectory);
        var path = Path.Combine(WorkingDirectory, "note.md");
        await FileSystem.CreateFileAtomicAsync(path, "original", cancellationToken);

        // act
        await Assert.ThrowsAsync<IOException>(
            () => FileSystem.CreateFileAtomicAsync(path, "conflicting", cancellationToken));

        // assert
        Assert.Equal(["note.md"], Directory.GetFiles(WorkingDirectory).Select(Path.GetFileName));
    }

    [Fact]
    public async Task ReplaceFileAtomicAsync_Should_CreateFile_When_DestinationDoesNotExist()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(WorkingDirectory);
        var path = Path.Combine(WorkingDirectory, "note.md");

        // act
        await FileSystem.ReplaceFileAtomicAsync(path, "hello", cancellationToken);

        // assert
        Assert.Equal("hello", await File.ReadAllTextAsync(path, cancellationToken));
    }

    [Fact]
    public async Task ReplaceFileAtomicAsync_Should_OverwriteContent_When_DestinationExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(WorkingDirectory);
        var path = Path.Combine(WorkingDirectory, "note.md");
        await FileSystem.CreateFileAtomicAsync(path, "original", cancellationToken);

        // act
        await FileSystem.ReplaceFileAtomicAsync(path, "updated", cancellationToken);

        // assert
        Assert.Equal("updated", await File.ReadAllTextAsync(path, cancellationToken));
    }

    [Fact]
    public async Task ReplaceFileAtomicAsync_Should_NotLeaveTempFile_When_Called()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(WorkingDirectory);
        var path = Path.Combine(WorkingDirectory, "note.md");
        await FileSystem.CreateFileAtomicAsync(path, "original", cancellationToken);

        // act
        await FileSystem.ReplaceFileAtomicAsync(path, "updated", cancellationToken);

        // assert
        Assert.Equal(["note.md"], Directory.GetFiles(WorkingDirectory).Select(Path.GetFileName));
    }

    [Fact]
    public void CleanupAbandonedTempFiles_Should_RemoveOldTempFiles_When_OlderThanThreshold()
    {
        // arrange
        Directory.CreateDirectory(WorkingDirectory);
        var abandonedTempPath = Path.Combine(WorkingDirectory, ".note.md.nitro-tmp-abc123");
        File.WriteAllText(abandonedTempPath, "orphaned by a killed process");
        File.SetLastWriteTimeUtc(abandonedTempPath, DateTime.UtcNow - TimeSpan.FromHours(2));

        // act
        FileSystem.CleanupAbandonedTempFiles(WorkingDirectory, TimeSpan.FromHours(1));

        // assert
        Assert.False(File.Exists(abandonedTempPath));
    }

    [Fact]
    public void CleanupAbandonedTempFiles_Should_KeepRecentTempFiles_When_NotOlderThanThreshold()
    {
        // arrange
        Directory.CreateDirectory(WorkingDirectory);
        var inFlightTempPath = Path.Combine(WorkingDirectory, ".note.md.nitro-tmp-def456");
        File.WriteAllText(inFlightTempPath, "a write still in progress");

        // act
        FileSystem.CleanupAbandonedTempFiles(WorkingDirectory, TimeSpan.FromHours(1));

        // assert
        Assert.True(File.Exists(inFlightTempPath));
    }

    [Fact]
    public void CleanupAbandonedTempFiles_Should_KeepOtherFiles_When_TheyDoNotMatchTheTempPattern()
    {
        // arrange
        Directory.CreateDirectory(WorkingDirectory);
        var unrelatedPath = Path.Combine(WorkingDirectory, "note.md");
        File.WriteAllText(unrelatedPath, "not a temp file");
        File.SetLastWriteTimeUtc(unrelatedPath, DateTime.UtcNow - TimeSpan.FromDays(1));

        // act
        FileSystem.CleanupAbandonedTempFiles(WorkingDirectory, TimeSpan.FromHours(1));

        // assert
        Assert.True(File.Exists(unrelatedPath));
    }

    [Fact]
    public void CleanupAbandonedTempFiles_Should_DoNothing_When_DirectoryDoesNotExist()
    {
        // act
        var exception = Record.Exception(
            () => FileSystem.CleanupAbandonedTempFiles(
                Path.Combine(WorkingDirectory, "does-not-exist"), TimeSpan.FromHours(1)));

        // assert
        Assert.Null(exception);
    }
}
