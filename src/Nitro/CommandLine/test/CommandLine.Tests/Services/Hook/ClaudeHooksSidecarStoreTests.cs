using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

public sealed class ClaudeHooksSidecarStoreTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot = Directory.CreateTempSubdirectory("nitro-claude-sidecar-store-tests");

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task WriteIfUnchangedAsync_ConcurrentWriters_RejectsTheContendingWriter()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var sidecarDirectory = Path.Combine(_tempRoot.FullName, "sidecars");
        var sidecarPath = Path.Combine(sidecarDirectory, "claude-hooks-sidecar.json");
        Directory.CreateDirectory(sidecarDirectory);
        await File.WriteAllTextAsync(sidecarPath, """{"version":1,"files":{}}""", ct);

        var firstFileSystem = new BlockFirstSidecarReplaceFileSystem(
            new TestFileSystem(_tempRoot.FullName),
            sidecarPath);
        var firstStore = CreateStore(firstFileSystem, sidecarDirectory);
        var secondStore = CreateStore(new TestFileSystem(_tempRoot.FullName), sidecarDirectory);
        var (firstFile, firstHash) = await firstStore.ReadWithHashAsync(ct);
        var (secondFile, secondHash) = await secondStore.ReadWithHashAsync(ct);
        firstFile.Files["first"] = [];
        secondFile.Files["second"] = [];

        // act
        var results = await ConcurrentTestHarness.RunAsync(2, async writer =>
        {
            if (writer == 1)
            {
                return new SidecarWriteResult(await firstStore.WriteIfUnchangedAsync(firstFile, firstHash, ct), null);
            }

            await firstFileSystem.WaitForReplaceAsync(ct);

            try
            {
                return new SidecarWriteResult(
                    await secondStore.WriteIfUnchangedAsync(secondFile, secondHash, ct),
                    null);
            }
            catch (ExitException exception)
            {
                return new SidecarWriteResult(false, exception.Message);
            }
            finally
            {
                firstFileSystem.ReleaseReplace();
            }
        });
        var persistedFile = await CreateStore(new TestFileSystem(_tempRoot.FullName), sidecarDirectory).ReadAsync(ct);

        // assert
        Assert.Equal(firstHash, secondHash);
        Assert.Equal(1, results.Count(result => result.Succeeded));
        Assert.Contains("locked by another nitro process", results[1].Failure, StringComparison.Ordinal);
        Assert.Equal(["first"], persistedFile.Files.Keys);
    }

    [Fact]
    public async Task WriteIfUnchangedAsync_DifferentSidecarPath_IsNotBlockedByAnotherSidecar()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var firstDirectory = Path.Combine(_tempRoot.FullName, "first-sidecars");
        var secondDirectory = Path.Combine(_tempRoot.FullName, "second-sidecars");
        var firstPath = Path.Combine(firstDirectory, "claude-hooks-sidecar.json");
        Directory.CreateDirectory(firstDirectory);
        Directory.CreateDirectory(secondDirectory);
        await File.WriteAllTextAsync(firstPath, """{"version":1,"files":{}}""", ct);

        var firstFileSystem = new BlockFirstSidecarReplaceFileSystem(
            new TestFileSystem(_tempRoot.FullName),
            firstPath);
        var firstStore = CreateStore(firstFileSystem, firstDirectory);
        var secondStore = CreateStore(new TestFileSystem(_tempRoot.FullName), secondDirectory);
        var (firstFile, firstHash) = await firstStore.ReadWithHashAsync(ct);
        var (secondFile, secondHash) = await secondStore.ReadWithHashAsync(ct);
        firstFile.Files["first"] = [];
        secondFile.Files["second"] = [];

        // act
        var firstWrite = firstStore.WriteIfUnchangedAsync(firstFile, firstHash, ct);
        await firstFileSystem.WaitForReplaceAsync(ct);
        var secondWrite = await secondStore.WriteIfUnchangedAsync(secondFile, secondHash, ct);
        firstFileSystem.ReleaseReplace();
        var firstWriteResult = await firstWrite;

        // assert
        Assert.True(firstWriteResult);
        Assert.True(secondWrite);
    }

    private static ClaudeHooksSidecarStore CreateStore(IFileSystem fileSystem, string sidecarDirectory)
        => new(fileSystem, new FixedSidecarDirectoryProvider(sidecarDirectory));

    private sealed record SidecarWriteResult(bool Succeeded, string? Failure);

    private sealed class FixedSidecarDirectoryProvider(string directory) : IGlobalConfigDirectoryProvider
    {
        public string GetDirectory() => directory;
    }

    /// <summary>
    /// Holds one sidecar replacement after its write guard has completed.
    /// </summary>
    private sealed class BlockFirstSidecarReplaceFileSystem(IFileSystem inner, string sidecarPath) : IFileSystem
    {
        private readonly TaskCompletionSource _replaceStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseReplace = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _hasBlocked;

        public bool FileExists(string path) => inner.FileExists(path);

        public Stream OpenReadStream(string path) => inner.OpenReadStream(path);

        public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
            => inner.ReadAllBytesAsync(path, ct);

        public Task<string> ReadAllTextAsync(string path, CancellationToken ct)
            => inner.ReadAllTextAsync(path, ct);

        public Stream CreateFile(string path) => inner.CreateFile(path);

        public Task WriteAllTextAsync(string path, string content, CancellationToken ct)
            => inner.WriteAllTextAsync(path, content, ct);

        public Task CreateFileAtomicAsync(string path, string content, CancellationToken ct)
            => inner.CreateFileAtomicAsync(path, content, ct);

        public async Task ReplaceFileAtomicAsync(string path, string content, CancellationToken ct)
        {
            if (string.Equals(path, sidecarPath, StringComparison.Ordinal)
                && Interlocked.CompareExchange(ref _hasBlocked, 1, 0) == 0)
            {
                _replaceStarted.TrySetResult();
                await _releaseReplace.Task.WaitAsync(ct);
            }

            await inner.ReplaceFileAtomicAsync(path, content, ct);
        }

        public void CleanupAbandonedTempFiles(string directory, TimeSpan olderThan)
            => inner.CleanupAbandonedTempFiles(directory, olderThan);

        public void DeleteFile(string path) => inner.DeleteFile(path);

        public bool DirectoryExists(string path) => inner.DirectoryExists(path);

        public void CreateDirectory(string path) => inner.CreateDirectory(path);

        public void MoveDirectory(string sourcePath, string targetPath)
            => inner.MoveDirectory(sourcePath, targetPath);

        public void DeleteDirectory(string path, bool recursive)
            => inner.DeleteDirectory(path, recursive);

        public string GetCurrentDirectory() => inner.GetCurrentDirectory();

        public IEnumerable<string> GetFiles(string directory, string pattern, SearchOption searchOption)
            => inner.GetFiles(directory, pattern, searchOption);

        public IEnumerable<string> GlobMatch(
            IEnumerable<string> patterns,
            IEnumerable<string>? excludes = null,
            string? workingDirectory = null)
            => inner.GlobMatch(patterns, excludes, workingDirectory);

        public Task WaitForReplaceAsync(CancellationToken cancellationToken)
            => _replaceStarted.Task.WaitAsync(cancellationToken);

        public void ReleaseReplace() => _releaseReplace.TrySetResult();
    }
}
