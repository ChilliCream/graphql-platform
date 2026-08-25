using System.Text.Json.Nodes;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="ClaudeHooksInstallerService"/> against a real
/// temp-directory file system (never <c>~/.claude</c>): the settings file
/// round-trip, the sidecar round-trip, and the concurrency guard that
/// re-reads and hash-compares immediately before every write.
/// </summary>
public sealed class ClaudeHooksInstallerServiceTests : IDisposable
{
    private static readonly LaunchDescriptor Descriptor = new("/home/agent/.dotnet/tools/nitro", []);

    private readonly DirectoryInfo _tempRoot;
    private readonly string _settingsPath;
    private readonly string _sidecarDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;

    public ClaudeHooksInstallerServiceTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-claude-hooks-installer-tests");
        _settingsPath = Path.Combine(_tempRoot.FullName, "claude-home", ".claude", "settings.json");
        _sidecarDirectory = Path.Combine(_tempRoot.FullName, "app-data");
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task InstallAsync_MissingFile_CreatesSettingsFileAndSidecar()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);

        var report = await service.InstallAsync(HookInstallScopes.User, ct);

        Assert.All(report.Events, e => Assert.Equal(HookInstallOutcome.Installed, e.Outcome));
        Assert.True(File.Exists(_settingsPath));

        var sidecarPath = Path.Combine(_sidecarDirectory, "claude-hooks-sidecar.json");
        Assert.True(File.Exists(sidecarPath));

        var sidecarJson = await File.ReadAllTextAsync(sidecarPath, ct);
        var sidecarRoot = JsonNode.Parse(sidecarJson)!.AsObject();
        var files = (JsonObject)sidecarRoot["files"]!;
        Assert.True(files.ContainsKey(_settingsPath));
    }

    [Fact]
    public async Task InstallAsync_SecondRun_IsANoOpWrite()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);

        await service.InstallAsync(HookInstallScopes.User, ct);
        var textAfterFirst = await File.ReadAllTextAsync(_settingsPath, ct);
        var writeTimeAfterFirst = File.GetLastWriteTimeUtc(_settingsPath);

        // Ensure the file system's write-time resolution would catch an
        // unwanted second write.
        await Task.Delay(50, ct);

        var report = await service.InstallAsync(HookInstallScopes.User, ct);

        Assert.All(report.Events, e => Assert.Equal(HookInstallOutcome.Unchanged, e.Outcome));
        Assert.Equal(textAfterFirst, await File.ReadAllTextAsync(_settingsPath, ct));
        Assert.Equal(writeTimeAfterFirst, File.GetLastWriteTimeUtc(_settingsPath));
    }

    [Fact]
    public async Task StatusAsync_DoesNotWriteAnything()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);

        var report = await service.StatusAsync(HookInstallScopes.User, ct);

        Assert.All(report.Events, e => Assert.Equal(HookStatusOutcome.Missing, e.Outcome));
        Assert.False(File.Exists(_settingsPath));
    }

    [Fact]
    public async Task UninstallAsync_RemovesOwnEntriesAndSidecarRecord()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService(_fileSystem);
        await service.InstallAsync(HookInstallScopes.User, ct);

        var report = await service.UninstallAsync(HookInstallScopes.User, ct);

        Assert.All(report.Events, e => Assert.Equal(HookUninstallOutcome.Removed, e.Outcome));

        // Every event's group was Nitro's only group, so the whole "hooks"
        // key, and with it the file's only content, collapses to `{}`.
        var text = await File.ReadAllTextAsync(_settingsPath, ct);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{}"), JsonNode.Parse(text)));

        var sidecarPath = Path.Combine(_sidecarDirectory, "claude-hooks-sidecar.json");
        var sidecarRoot = JsonNode.Parse(await File.ReadAllTextAsync(sidecarPath, ct))!.AsObject();
        var files = (JsonObject)sidecarRoot["files"]!;
        Assert.False(files.ContainsKey(_settingsPath));
    }

    [Fact]
    public async Task InstallAsync_ConcurrentForeignEdit_AbortsWithoutWriting()
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        await File.WriteAllTextAsync(_settingsPath, """{"hooks":{}}""", ct);

        var injectingFileSystem = new InjectEditOnSecondReadFileSystem(
            _fileSystem, _settingsPath, """{"hooks":{"SessionStart":[{"hooks":[{"type":"command","command":"/usr/local/bin/herdr hook session-start","timeout":5}]}]}}""");

        var service = CreateService(injectingFileSystem);

        var exception = await Assert.ThrowsAsync<ExitException>(
            () => service.InstallAsync(HookInstallScopes.User, ct));

        Assert.Contains("changed since it was read", exception.Message, StringComparison.Ordinal);

        // The concurrent (foreign) edit must survive untouched: the aborted
        // install never wrote anything.
        var text = await File.ReadAllTextAsync(_settingsPath, ct);
        Assert.Contains("herdr", text, StringComparison.Ordinal);
        Assert.DoesNotContain("agent hook claude", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InstallAsync_TwoInterleavedInstalls_KeepsBothSidecarEntries()
    {
        var ct = TestContext.Current.CancellationToken;
        var settingsPathA = Path.Combine(_tempRoot.FullName, "claude-home-a", ".claude", "settings.json");
        var settingsPathB = Path.Combine(_tempRoot.FullName, "claude-home-b", ".claude", "settings.json");
        var sidecarPath = Path.Combine(_sidecarDirectory, "claude-hooks-sidecar.json");

        Directory.CreateDirectory(_sidecarDirectory);
        await File.WriteAllTextAsync(sidecarPath, """{"version":1,"files":{}}""", ct);

        var serviceB = new ClaudeHooksInstallerService(
            _fileSystem,
            new FixedClaudeSettingsPathResolver(settingsPathB),
            new FixedLaunchDescriptorResolver(Descriptor),
            new ClaudeHooksSidecarStore(_fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory)),
            _timeProvider);

        // Simulates a second, fully concurrent 'nitro agent hooks claude
        // install' (project scope) landing in the window between this
        // install's own sidecar read and its pre-write re-check: the SECOND
        // ReadAllTextAsync call for the sidecar runs B's install to
        // completion first, then reads the sidecar it left behind - exactly
        // what this install's own re-read would observe. B's install only
        // ever triggers on the first attempt's re-check read, so the retry
        // then observes a stable sidecar and succeeds.
        var injectingFileSystem = new RunOnSecondReadFileSystem(
            _fileSystem, sidecarPath, () => serviceB.InstallAsync(HookInstallScopes.User, ct));

        var serviceA = new ClaudeHooksInstallerService(
            injectingFileSystem,
            new FixedClaudeSettingsPathResolver(settingsPathA),
            new FixedLaunchDescriptorResolver(Descriptor),
            new ClaudeHooksSidecarStore(injectingFileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory)),
            _timeProvider);

        await serviceA.InstallAsync(HookInstallScopes.User, ct);

        var filesAfter = SidecarFiles(await File.ReadAllTextAsync(sidecarPath, ct));
        Assert.True(filesAfter.ContainsKey(settingsPathA));
        Assert.True(filesAfter.ContainsKey(settingsPathB));
        Assert.True(File.Exists(settingsPathA));
    }

    [Fact]
    public async Task UninstallAsync_InterleavedInstall_KeepsConcurrentSidecarEntry()
    {
        var ct = TestContext.Current.CancellationToken;
        var settingsPathA = Path.Combine(_tempRoot.FullName, "claude-home-a", ".claude", "settings.json");
        var settingsPathB = Path.Combine(_tempRoot.FullName, "claude-home-b", ".claude", "settings.json");
        var sidecarPath = Path.Combine(_sidecarDirectory, "claude-hooks-sidecar.json");

        var preInstallService = new ClaudeHooksInstallerService(
            _fileSystem,
            new FixedClaudeSettingsPathResolver(settingsPathA),
            new FixedLaunchDescriptorResolver(Descriptor),
            new ClaudeHooksSidecarStore(_fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory)),
            _timeProvider);
        await preInstallService.InstallAsync(HookInstallScopes.User, ct);

        var serviceB = new ClaudeHooksInstallerService(
            _fileSystem,
            new FixedClaudeSettingsPathResolver(settingsPathB),
            new FixedLaunchDescriptorResolver(Descriptor),
            new ClaudeHooksSidecarStore(_fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory)),
            _timeProvider);

        // Simulates a second, fully concurrent install landing in the window
        // between this uninstall's own sidecar read and its pre-write
        // re-check: the SECOND ReadAllTextAsync call for the sidecar runs
        // B's install to completion first, then reads the sidecar it left
        // behind - exactly what this uninstall's own re-read would observe.
        var injectingFileSystem = new RunOnSecondReadFileSystem(
            _fileSystem, sidecarPath, () => serviceB.InstallAsync(HookInstallScopes.User, ct));

        var serviceA = new ClaudeHooksInstallerService(
            injectingFileSystem,
            new FixedClaudeSettingsPathResolver(settingsPathA),
            new FixedLaunchDescriptorResolver(Descriptor),
            new ClaudeHooksSidecarStore(injectingFileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory)),
            _timeProvider);

        await serviceA.UninstallAsync(HookInstallScopes.User, ct);

        var filesAfter = SidecarFiles(await File.ReadAllTextAsync(sidecarPath, ct));
        Assert.False(filesAfter.ContainsKey(settingsPathA));
        Assert.True(filesAfter.ContainsKey(settingsPathB));
    }

    private static JsonObject SidecarFiles(string sidecarJson)
        => (JsonObject)JsonNode.Parse(sidecarJson)!.AsObject()["files"]!;

    private ClaudeHooksInstallerService CreateService(IFileSystem fileSystem) => new(
        fileSystem,
        new FixedClaudeSettingsPathResolver(_settingsPath),
        new FixedLaunchDescriptorResolver(Descriptor),
        new ClaudeHooksSidecarStore(fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory)),
        _timeProvider);

    private sealed class FixedClaudeSettingsPathResolver(string path) : IClaudeSettingsPathResolver
    {
        public string Resolve(string scope) => path;
    }

    private sealed class FixedLaunchDescriptorResolver(LaunchDescriptor descriptor) : ILaunchDescriptorResolver
    {
        public LaunchDescriptor Resolve() => descriptor;
    }

    private sealed class FixedSidecarDirectoryProvider(string directory)
        : Services.Workspace.IGlobalConfigDirectoryProvider
    {
        public string GetDirectory() => directory;
    }

    /// <summary>
    /// Simulates a foreign process editing the settings file in the window
    /// between the installer's initial read and its pre-write re-check: the
    /// SECOND <see cref="ReadAllTextAsync"/> call for the watched path
    /// writes <paramref name="editedContent"/> first, then reads it back -
    /// exactly what the installer's own re-read would observe.
    /// </summary>
    private sealed class InjectEditOnSecondReadFileSystem(
        IFileSystem inner, string watchedPath, string editedContent) : IFileSystem
    {
        private int _readCount;

        public bool FileExists(string path) => inner.FileExists(path);

        public Stream OpenReadStream(string path) => inner.OpenReadStream(path);

        public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct) => inner.ReadAllBytesAsync(path, ct);

        public async Task<string> ReadAllTextAsync(string path, CancellationToken ct)
        {
            if (string.Equals(path, watchedPath, StringComparison.Ordinal) && Interlocked.Increment(ref _readCount) == 2)
            {
                await inner.ReplaceFileAtomicAsync(path, editedContent, ct);
            }

            return await inner.ReadAllTextAsync(path, ct);
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

    /// <summary>
    /// Simulates a second, fully concurrent install landing in the window
    /// between an in-flight install's own read of <paramref name="watchedPath"/>
    /// and its pre-write re-check: the SECOND <see cref="ReadAllTextAsync"/>
    /// call for that path runs <paramref name="onSecondRead"/> to completion
    /// first, then reads whatever it left behind.
    /// </summary>
    private sealed class RunOnSecondReadFileSystem(
        IFileSystem inner, string watchedPath, Func<Task> onSecondRead) : IFileSystem
    {
        private int _readCount;

        public bool FileExists(string path) => inner.FileExists(path);

        public Stream OpenReadStream(string path) => inner.OpenReadStream(path);

        public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct) => inner.ReadAllBytesAsync(path, ct);

        public async Task<string> ReadAllTextAsync(string path, CancellationToken ct)
        {
            if (string.Equals(path, watchedPath, StringComparison.Ordinal) && Interlocked.Increment(ref _readCount) == 2)
            {
                await onSecondRead();
            }

            return await inner.ReadAllTextAsync(path, ct);
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
