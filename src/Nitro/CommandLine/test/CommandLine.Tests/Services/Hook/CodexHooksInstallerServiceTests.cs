using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CodexHooksInstallerService"/> against a real
/// temp-directory file system (never <c>~/.codex</c>): the hooks.json AND
/// config.toml round trip together, the sidecar round trip covering both
/// files, and the install-flow's headline scenario - wrapping a foreign
/// <c>notify</c> program on install and restoring it verbatim on uninstall.
/// </summary>
public sealed class CodexHooksInstallerServiceTests : IDisposable
{
    private static readonly LaunchDescriptor Descriptor = new("/home/agent/.dotnet/tools/nitro", []);

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
        Assert.True(sidecar.HooksFiles.ContainsKey(_hooksJsonPath));
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
        Assert.False(sidecar.HooksFiles.ContainsKey(_hooksJsonPath));
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
        new FixedLaunchDescriptorResolver(Descriptor),
        new CodexHooksSidecarStore(fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory)),
        _timeProvider);

    private static async Task<CodexHooksSidecarFile> ReadSidecarAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);

        return JsonSerializer.Deserialize(json, CodexHooksSidecarJsonContext.Default.CodexHooksSidecarFile)!;
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
