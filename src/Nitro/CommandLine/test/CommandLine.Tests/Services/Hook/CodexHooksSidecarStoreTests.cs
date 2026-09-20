using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

public sealed class CodexHooksSidecarStoreTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _sidecarDirectory;
    private readonly string _sidecarPath;
    private readonly TestFileSystem _fileSystem;

    public CodexHooksSidecarStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-codex-hooks-sidecar-store-tests");
        _sidecarDirectory = Path.Combine(_tempRoot.FullName, "app-data");
        _sidecarPath = Path.Combine(_sidecarDirectory, "codex-hooks-sidecar.json");
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task WriteIfUnchangedAsync_UnchangedSnapshot_WritesSidecar()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var store = CreateStore();
        var (_, hashAtRead) = await store.ReadWithHashAsync(ct);
        var file = new CodexHooksSidecarFile(
            CodexHooksSidecarFile.CurrentVersion,
            new Dictionary<string, CodexNotifySidecarEntry>
            {
                ["/codex/config.toml"] = new(["nitro", "agent", "hook"], null, DateTimeOffset.UnixEpoch)
            });

        // act
        var written = await store.WriteIfUnchangedAsync(file, hashAtRead, ct);
        var (actual, _) = await store.ReadWithHashAsync(ct);

        // assert
        Assert.True(written);
        Assert.True(File.Exists(_sidecarPath));
        Assert.Equal(["nitro", "agent", "hook"], actual.NotifyFiles["/codex/config.toml"].OurArgv);
    }

    [Fact]
    public async Task WriteIfUnchangedAsync_StaleSnapshot_ReturnsFalseAndPreservesCurrentContent()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        const string originalContent = """{"version":2,"notifyFiles":{}}""";
        const string changedContent = """{"version":2,"notifyFiles":{},"external":true}""";
        Directory.CreateDirectory(_sidecarDirectory);
        await File.WriteAllTextAsync(_sidecarPath, originalContent, ct);
        var store = CreateStore();
        var (file, hashAtRead) = await store.ReadWithHashAsync(ct);
        await File.WriteAllTextAsync(_sidecarPath, changedContent, ct);

        // act
        var written = await store.WriteIfUnchangedAsync(file, hashAtRead, ct);

        // assert
        Assert.False(written);
        Assert.Equal(changedContent, await File.ReadAllTextAsync(_sidecarPath, ct));
    }

    private CodexHooksSidecarStore CreateStore()
        => new(_fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory));

    private sealed class FixedSidecarDirectoryProvider(string directory) : IGlobalConfigDirectoryProvider
    {
        public string GetDirectory() => directory;
    }
}
