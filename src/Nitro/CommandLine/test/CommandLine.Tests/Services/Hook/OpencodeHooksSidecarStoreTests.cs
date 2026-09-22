using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

public sealed class OpencodeHooksSidecarStoreTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _sidecarDirectory;
    private readonly string _sidecarPath;
    private readonly TestFileSystem _fileSystem;

    public OpencodeHooksSidecarStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-opencode-hooks-sidecar-store-tests");
        _sidecarDirectory = Path.Combine(_tempRoot.FullName, "app-data");
        _sidecarPath = Path.Combine(_sidecarDirectory, "opencode-hooks-sidecar.json");
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task WriteIfUnchangedAsync_Should_RejectContendingWriter_When_WriteLockIsHeld()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(_sidecarDirectory);
        await File.WriteAllTextAsync(_sidecarPath, """{"version":1,"files":{}}""", ct);

        var firstFileSystem = new ControlledSidecarReplaceFileSystem(
            new TestFileSystem(_tempRoot.FullName),
            _sidecarPath,
            blockFirstReplacement: true);
        var firstStore = CreateStore(firstFileSystem);
        var secondStore = CreateStore(new TestFileSystem(_tempRoot.FullName));
        var (firstFile, firstHash) = await firstStore.ReadWithHashAsync(ct);
        var (secondFile, secondHash) = await secondStore.ReadWithHashAsync(ct);
        firstFile.Files["first"] = CreateEntry();
        secondFile.Files["second"] = CreateEntry();

        // act
        var results = await ConcurrentTestHarness.RunAsync<(bool Succeeded, string? Failure)>(2, async writer =>
        {
            if (writer == 1)
            {
                return (Succeeded: await firstStore.WriteIfUnchangedAsync(firstFile, firstHash, ct), Failure: null);
            }

            await firstFileSystem.WaitForReplaceAsync(ct);

            try
            {
                return (Succeeded: await secondStore.WriteIfUnchangedAsync(secondFile, secondHash, ct), Failure: null);
            }
            catch (ExitException exception)
            {
                return (Succeeded: false, Failure: exception.Message);
            }
            finally
            {
                firstFileSystem.ReleaseReplace();
            }
        });
        var (persistedFile, _) = await CreateStore().ReadWithHashAsync(ct);

        // assert
        Assert.Equal(firstHash, secondHash);
        Assert.Equal(1, results.Count(result => result.Succeeded));
        Assert.False(results[1].Succeeded);
        Assert.StartsWith(
            $"Could not acquire write lock '{_sidecarPath}.lock' for sidecar '{_sidecarPath}' after 5 attempts: ",
            results[1].Failure);
        Assert.Equal(["first"], persistedFile.Files.Keys);
    }

    [Fact]
    public async Task WriteIfUnchangedAsync_Should_Cancel_When_WriteLockIsHeld()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        const string content = """{"version":1,"files":{}}""";
        Directory.CreateDirectory(_sidecarDirectory);
        await File.WriteAllTextAsync(_sidecarPath, content, ct);
        var store = CreateStore();
        var (file, hashAtRead) = await store.ReadWithHashAsync(ct);
        await using var heldLock = File.Open(_sidecarPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // act
        var write = store.WriteIfUnchangedAsync(file, hashAtRead, cancellation.Token);
        cancellation.Cancel();
        var exception = await Record.ExceptionAsync(() => write);

        // assert
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        Assert.Equal(content, await File.ReadAllTextAsync(_sidecarPath, ct));
    }

    [Fact]
    public async Task WriteIfUnchangedAsync_Should_ReleaseWriteLock_When_SnapshotIsStale()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        const string originalContent = """{"version":1,"files":{}}""";
        const string changedContent = """{"version":1,"files":{},"external":true}""";
        Directory.CreateDirectory(_sidecarDirectory);
        await File.WriteAllTextAsync(_sidecarPath, originalContent, ct);
        var store = CreateStore();
        var (file, hashAtRead) = await store.ReadWithHashAsync(ct);
        await File.WriteAllTextAsync(_sidecarPath, changedContent, ct);

        // act
        var rejected = await store.WriteIfUnchangedAsync(file, hashAtRead, ct);
        var contentAfterRejectedWrite = await File.ReadAllTextAsync(_sidecarPath, ct);
        var (_, currentHash) = await store.ReadWithHashAsync(ct);
        var recovered = await store.WriteIfUnchangedAsync(file, currentHash, ct);
        var (_, recoveryHash) = await store.ReadWithHashAsync(ct);
        var rewritten = await store.WriteIfUnchangedAsync(file, recoveryHash, ct);

        // assert
        Assert.False(rejected);
        Assert.Equal(changedContent, contentAfterRejectedWrite);
        Assert.True(recovered);
        Assert.True(rewritten);
    }

    [Fact]
    public async Task WriteIfUnchangedAsync_Should_IncludeLockOpenFailure_When_LockParentIsMissing()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(_sidecarDirectory);
        var store = CreateStore(
            new ControlledSidecarReplaceFileSystem(
                new TestFileSystem(_tempRoot.FullName),
                _sidecarPath,
                deleteSidecarDirectoryBeforeLock: true));
        var (file, hashAtRead) = await store.ReadWithHashAsync(ct);
        file.Files["first"] = CreateEntry();

        // act
        var exception = await Record.ExceptionAsync(() => store.WriteIfUnchangedAsync(file, hashAtRead, ct));

        // assert
        var exitException = Assert.IsType<ExitException>(exception);
        Assert.StartsWith(
            $"Could not acquire write lock '{_sidecarPath}.lock' for sidecar '{_sidecarPath}' after 5 attempts: ",
            exitException.Message);
        Assert.Contains("Could not find a part of the path", exitException.Message, StringComparison.Ordinal);
    }

    private OpencodeHooksSidecarStore CreateStore(IFileSystem? fileSystem = null)
        => new(fileSystem ?? _fileSystem, new SidecarTestDirectoryProvider(_sidecarDirectory));

    private static OpencodeHooksSidecarEntry CreateEntry()
        => new("nitro agent hook", "content-hash", DateTimeOffset.UnixEpoch);
}
