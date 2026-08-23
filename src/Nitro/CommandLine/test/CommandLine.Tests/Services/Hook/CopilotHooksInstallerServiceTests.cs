using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CopilotHooksInstallerService"/> against a real
/// temp-directory file system (never <c>~/.copilot</c>): the hooks file
/// round trip, the sidecar round trip, and the concurrency guard.
/// </summary>
public sealed class CopilotHooksInstallerServiceTests : IDisposable
{
    private static readonly LaunchDescriptor Descriptor = new("/home/agent/.dotnet/tools/nitro", []);

    private readonly DirectoryInfo _tempRoot;
    private readonly string _hooksJsonPath;
    private readonly string _sidecarDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;

    public CopilotHooksInstallerServiceTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-copilot-hooks-installer-tests");
        _hooksJsonPath = Path.Combine(_tempRoot.FullName, "copilot-home", "hooks", "nitro-mail.json");
        _sidecarDirectory = Path.Combine(_tempRoot.FullName, "app-data");
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task InstallAsync_MissingFile_CreatesTheFileAndTheSidecar()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);

        var report = await service.InstallAsync(ct);

        Assert.All(report.HooksEvents, e => Assert.Equal(HookInstallOutcome.Installed, e.Outcome));
        Assert.True(File.Exists(_hooksJsonPath));

        var sidecarPath = Path.Combine(_sidecarDirectory, "copilot-hooks-sidecar.json");
        Assert.True(File.Exists(sidecarPath));

        var sidecar = await ReadSidecarAsync(sidecarPath);
        Assert.True(sidecar.HooksFiles.ContainsKey(_hooksJsonPath));
        Assert.Equal(3, sidecar.HooksFiles[_hooksJsonPath].Count);

        var text = await File.ReadAllTextAsync(_hooksJsonPath, ct);
        Assert.True(JsonSerializer.Deserialize<JsonElement>(text).TryGetProperty("hooks", out _));
    }

    [Fact]
    public async Task InstallAsync_SecondRun_IsANoOpWrite()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);

        await service.InstallAsync(ct);
        var textAfterFirst = await File.ReadAllTextAsync(_hooksJsonPath, ct);
        var writeTimeAfterFirst = File.GetLastWriteTimeUtc(_hooksJsonPath);

        await Task.Delay(50, ct);

        var report = await service.InstallAsync(ct);

        Assert.All(report.HooksEvents, e => Assert.Equal(HookInstallOutcome.Unchanged, e.Outcome));
        Assert.Equal(textAfterFirst, await File.ReadAllTextAsync(_hooksJsonPath, ct));
        Assert.Equal(writeTimeAfterFirst, File.GetLastWriteTimeUtc(_hooksJsonPath));
    }

    [Fact]
    public async Task UninstallAsync_RemovesTheHooksFileEntriesAndTheSidecar()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);
        await service.InstallAsync(ct);

        var report = await service.UninstallAsync(ct);

        Assert.All(report.HooksEvents, e => Assert.Equal(HookUninstallOutcome.Removed, e.Outcome));

        var text = await File.ReadAllTextAsync(_hooksJsonPath, ct);
        Assert.DoesNotContain("agent hook copilot", text);

        var sidecar = await ReadSidecarAsync(Path.Combine(_sidecarDirectory, "copilot-hooks-sidecar.json"));
        Assert.False(sidecar.HooksFiles.ContainsKey(_hooksJsonPath));
    }

    [Fact]
    public async Task UninstallAsync_MissingFile_AllNotPresent()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);

        var report = await service.UninstallAsync(ct);

        Assert.All(report.HooksEvents, e => Assert.Equal(HookUninstallOutcome.NotPresent, e.Outcome));
        Assert.False(File.Exists(_hooksJsonPath));
    }

    [Fact]
    public async Task StatusAsync_DoesNotWriteAnything()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);

        var report = await service.StatusAsync(ct);

        Assert.All(report.HooksEvents, e => Assert.Equal(HookStatusOutcome.Missing, e.Outcome));
        Assert.False(File.Exists(_hooksJsonPath));
    }

    [Fact]
    public async Task InstallAsync_ConcurrentForeignEdit_AbortsWithoutClobbering()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);
        await service.InstallAsync(ct);

        // Simulate a foreign edit landing between the service's read and its
        // write by mutating the file underneath a stale in-memory read: the
        // simplest reliable way to do this against the real file system is
        // to change the file after obtaining the installer's normal read, so
        // route through a file system wrapper whose ReadAllTextAsync mutates
        // the file once, right after returning the value it read.
        var tamperingFileSystem = new TamperOnFirstReadFileSystem(_fileSystem, _hooksJsonPath);
        var tamperingService = CreateService(tamperingFileSystem);

        await Assert.ThrowsAsync<ExitException>(() => tamperingService.InstallAsync(ct));
    }

    private CopilotHooksInstallerService CreateService(IFileSystem fileSystem) => new(
        fileSystem,
        new FixedCopilotPathResolver(_hooksJsonPath),
        new FixedLaunchDescriptorResolver(Descriptor),
        new CopilotHooksSidecarStore(fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory)),
        _timeProvider);

    private static async Task<CopilotHooksSidecarFile> ReadSidecarAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);

        return JsonSerializer.Deserialize(json, CopilotHooksSidecarJsonContext.Default.CopilotHooksSidecarFile)!;
    }

    private sealed class FixedCopilotPathResolver(string hooksJsonPath) : ICopilotPathResolver
    {
        public string ResolveHooksFile() => hooksJsonPath;
    }

    private sealed class FixedLaunchDescriptorResolver(LaunchDescriptor descriptor) : ILaunchDescriptorResolver
    {
        public LaunchDescriptor Resolve() => descriptor;
    }

    private sealed class FixedSidecarDirectoryProvider(string directory) : IGlobalConfigDirectoryProvider
    {
        public string GetDirectory() => directory;
    }

    /// <summary>
    /// Wraps a real <see cref="IFileSystem"/> so that reading
    /// <paramref name="targetPath"/> tampers with the file, once, right
    /// after returning the value it read - simulating a foreign edit landing
    /// between the installer's own read-before-write and its write.
    /// </summary>
    private sealed class TamperOnFirstReadFileSystem(IFileSystem inner, string targetPath) : IFileSystem
    {
        private bool _tampered;

        public bool FileExists(string path) => inner.FileExists(path);

        public Stream OpenReadStream(string path) => inner.OpenReadStream(path);

        public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
            => inner.ReadAllBytesAsync(path, ct);

        public async Task<string> ReadAllTextAsync(string path, CancellationToken ct)
        {
            var text = await inner.ReadAllTextAsync(path, ct);

            if (path == targetPath && !_tampered)
            {
                _tampered = true;
                await inner.ReplaceFileAtomicAsync(path, text + " ", ct);
            }

            return text;
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

        public string GetCurrentDirectory() => inner.GetCurrentDirectory();

        public IEnumerable<string> GetFiles(string directory, string pattern, SearchOption searchOption)
            => inner.GetFiles(directory, pattern, searchOption);

        public IEnumerable<string> GlobMatch(
            IEnumerable<string> patterns, IEnumerable<string>? excludes = null, string? workingDirectory = null)
            => inner.GlobMatch(patterns, excludes, workingDirectory);
    }
}
