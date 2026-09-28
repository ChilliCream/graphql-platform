using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Tests <see cref="CodexHooksInstallerService"/> configuration and sidecar
/// updates in temporary directories, including foreign notify restoration.
/// </summary>
public sealed class CodexHooksInstallerServiceTests : IDisposable
{
    private static readonly LaunchDescriptor s_descriptor = new("/home/agent/.dotnet/tools/nitro", []);

    private readonly DirectoryInfo _tempRoot;
    private readonly string _hooksJsonPath;
    private readonly string _configTomlPath;
    private readonly string _sidecarDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;

    public CodexHooksInstallerServiceTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-codex-hooks-installer-tests");
        _hooksJsonPath = Path.Combine(_tempRoot.FullName, "codex-home", "hooks.json");
        _configTomlPath = Path.Combine(_tempRoot.FullName, "codex-home", "config.toml");
        _sidecarDirectory = Path.Combine(_tempRoot.FullName, "app-data");
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task InstallAsync_MissingFiles_CreatesBothFilesAndTheSidecar()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);

        var report = await service.InstallAsync(ct);

        Assert.All(report.HooksEvents, e => Assert.Equal(HookInstallOutcome.Installed, e.Outcome));
        Assert.Equal(HookInstallOutcome.Installed, report.NotifyOutcome);
        Assert.False(report.NotifyWrapsForeign);
        Assert.True(File.Exists(_hooksJsonPath));
        Assert.True(File.Exists(_configTomlPath));

        var sidecarPath = Path.Combine(_sidecarDirectory, "codex-hooks-sidecar.json");
        Assert.True(File.Exists(sidecarPath));

        var sidecar = await ReadSidecarAsync(sidecarPath);
        Assert.True(sidecar.NotifyFiles.ContainsKey(_configTomlPath));
        Assert.Null(sidecar.NotifyFiles[_configTomlPath].PriorForeign);
    }

    [Fact]
    public async Task InstallAsync_SecondRun_IsANoOpWrite()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);

        await service.InstallAsync(ct);
        var hooksTextAfterFirst = await File.ReadAllTextAsync(_hooksJsonPath, ct);
        var tomlTextAfterFirst = await File.ReadAllTextAsync(_configTomlPath, ct);
        var hooksWriteTimeAfterFirst = File.GetLastWriteTimeUtc(_hooksJsonPath);
        var tomlWriteTimeAfterFirst = File.GetLastWriteTimeUtc(_configTomlPath);

        await Task.Delay(50, ct);

        var report = await service.InstallAsync(ct);

        Assert.All(report.HooksEvents, e => Assert.Equal(HookInstallOutcome.Unchanged, e.Outcome));
        Assert.Equal(HookInstallOutcome.Unchanged, report.NotifyOutcome);
        Assert.Equal(hooksTextAfterFirst, await File.ReadAllTextAsync(_hooksJsonPath, ct));
        Assert.Equal(tomlTextAfterFirst, await File.ReadAllTextAsync(_configTomlPath, ct));
        Assert.Equal(hooksWriteTimeAfterFirst, File.GetLastWriteTimeUtc(_hooksJsonPath));
        Assert.Equal(tomlWriteTimeAfterFirst, File.GetLastWriteTimeUtc(_configTomlPath));
    }

    [Fact]
    public async Task InstallAsync_ForeignNotifyAlreadyConfigured_WrapsItAndRecordsItInTheSidecar()
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.GetDirectoryName(_configTomlPath)!);
        await File.WriteAllTextAsync(_configTomlPath, "notify = [\"/usr/local/bin/herdr-notify\", \"--flag\"]\n", ct);
        var service = CreateService(_fileSystem);

        var report = await service.InstallAsync(ct);

        Assert.Equal(HookInstallOutcome.Updated, report.NotifyOutcome);
        Assert.True(report.NotifyWrapsForeign);

        var tomlText = await File.ReadAllTextAsync(_configTomlPath, ct);
        Assert.DoesNotContain("herdr-notify", tomlText);
        Assert.Contains("agent\", \"hook\", \"codex\", \"notify\"", tomlText);

        var sidecar = await ReadSidecarAsync(Path.Combine(_sidecarDirectory, "codex-hooks-sidecar.json"));
        Assert.Equal(["/usr/local/bin/herdr-notify", "--flag"], sidecar.NotifyFiles[_configTomlPath].PriorForeign);
    }

    [Fact]
    public async Task UninstallAsync_RestoresTheWrappedForeignNotifyProgramAndRemovesHooksJsonEntries()
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.GetDirectoryName(_configTomlPath)!);
        await File.WriteAllTextAsync(_configTomlPath, "notify = [\"/usr/local/bin/herdr-notify\", \"--flag\"]\n", ct);
        var service = CreateService(_fileSystem);
        await service.InstallAsync(ct);

        var report = await service.UninstallAsync(ct);

        Assert.All(report.HooksEvents, e => Assert.Equal(HookUninstallOutcome.Removed, e.Outcome));
        Assert.Equal(HookUninstallOutcome.Removed, report.NotifyOutcome);
        Assert.True(report.NotifyForeignRestored);

        var tomlText = await File.ReadAllTextAsync(_configTomlPath, ct);
        Assert.Contains("notify = [\"/usr/local/bin/herdr-notify\", \"--flag\"]", tomlText);
        Assert.DoesNotContain("agent hook codex notify", tomlText);

        var hooksJsonText = await File.ReadAllTextAsync(_hooksJsonPath, ct);
        Assert.True(JsonSerializer.Deserialize<JsonElement>(hooksJsonText).ValueKind == JsonValueKind.Object);
        Assert.DoesNotContain("agent hook codex", hooksJsonText);

        var sidecar = await ReadSidecarAsync(Path.Combine(_sidecarDirectory, "codex-hooks-sidecar.json"));
        Assert.False(sidecar.NotifyFiles.ContainsKey(_configTomlPath));
    }

    [Fact]
    public async Task UninstallAsync_NoPriorForeign_RemovesTheNotifyKeyEntirely()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);
        await service.InstallAsync(ct);

        var report = await service.UninstallAsync(ct);

        Assert.Equal(HookUninstallOutcome.Removed, report.NotifyOutcome);
        Assert.False(report.NotifyForeignRestored);

        var tomlText = await File.ReadAllTextAsync(_configTomlPath, ct);
        Assert.DoesNotContain("notify", tomlText);
    }

    [Fact]
    public async Task InstallAsync_ConcurrentSidecarChanges_ReportsFailureWithoutOverwriting()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var sidecarPath = Path.Combine(_sidecarDirectory, "codex-hooks-sidecar.json");
        Directory.CreateDirectory(_sidecarDirectory);
        await File.WriteAllTextAsync(sidecarPath, """{"version":2,"notifyFiles":{}}""", ct);
        var injectingFileSystem = new InjectSidecarChangesOnReadFileSystem(_fileSystem, sidecarPath);
        var service = CreateService(injectingFileSystem);

        // act
        var exception = await Assert.ThrowsAsync<ExitException>(() => service.InstallAsync(ct));

        // assert
        Assert.Contains("codex-hooks-sidecar.json", exception.Message, StringComparison.Ordinal);
        Assert.Equal(injectingFileSystem.LastInjectedContent, await File.ReadAllTextAsync(sidecarPath, ct));
    }

    [Fact]
    public async Task StatusAsync_DoesNotWriteAnything()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);

        var report = await service.StatusAsync(ct);

        Assert.All(report.HooksEvents, e => Assert.Equal(HookStatusOutcome.Missing, e.Outcome));
        Assert.Equal(HookStatusOutcome.Missing, report.NotifyOutcome);
        Assert.False(File.Exists(_hooksJsonPath));
        Assert.False(File.Exists(_configTomlPath));
    }

    private CodexHooksInstallerService CreateService(IFileSystem fileSystem) => new(
        fileSystem,
        new FixedCodexPathResolver(_hooksJsonPath, _configTomlPath),
        new FixedLaunchDescriptorResolver(s_descriptor),
        new CodexHooksSidecarStore(fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory)),
        _timeProvider);

    private static async Task<CodexHooksSidecarFile> ReadSidecarAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);

        return JsonSerializer.Deserialize(json, CodexHooksSidecarJsonContext.Default.CodexHooksSidecarFile)!;
    }

    /// <summary>
    /// Replaces the watched sidecar after each read returns its prior content.
    /// </summary>
    private sealed class InjectSidecarChangesOnReadFileSystem(IFileSystem inner, string watchedPath) : IFileSystem
    {
        private int _readCount;

        public string LastInjectedContent { get; private set; } = string.Empty;

        public bool FileExists(string path) => inner.FileExists(path);

        public Stream OpenReadStream(string path) => inner.OpenReadStream(path);

        public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct) => inner.ReadAllBytesAsync(path, ct);

        public async Task<string> ReadAllTextAsync(string path, CancellationToken ct)
        {
            var content = await inner.ReadAllTextAsync(path, ct);

            if (string.Equals(path, watchedPath, StringComparison.Ordinal))
            {
                LastInjectedContent = $$"""{"version":2,"notifyFiles":{},"external":{{Interlocked.Increment(ref _readCount)}}}""";
                await inner.ReplaceFileAtomicAsync(path, LastInjectedContent, ct);
            }

            return content;
        }

        public Stream CreateFile(string path) => inner.CreateFile(path);

        public Task WriteAllTextAsync(string path, string content, CancellationToken ct)
            => inner.WriteAllTextAsync(path, content, ct);

        public Task CreateFileAtomicAsync(string path, string content, CancellationToken ct)
            => inner.CreateFileAtomicAsync(path, content, ct);

        public Task ReplaceFileAtomicAsync(string path, string content, CancellationToken ct)
            => inner.ReplaceFileAtomicAsync(path, content, ct);

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
            IEnumerable<string> patterns, IEnumerable<string>? excludes = null, string? workingDirectory = null)
            => inner.GlobMatch(patterns, excludes, workingDirectory);
    }

    private sealed class FixedCodexPathResolver(string hooksJsonPath, string configTomlPath) : ICodexPathResolver
    {
        public string ResolveHooksJson() => hooksJsonPath;

        public string ResolveConfigToml() => configTomlPath;
    }

    private sealed class FixedLaunchDescriptorResolver(LaunchDescriptor descriptor) : ILaunchDescriptorResolver
    {
        public LaunchDescriptor Resolve() => descriptor;
    }

    private sealed class FixedSidecarDirectoryProvider(string directory) : IGlobalConfigDirectoryProvider
    {
        public string GetDirectory() => directory;
    }
}
