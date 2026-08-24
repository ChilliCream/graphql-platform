using ChilliCream.Nitro.CommandLine.Commands.Agent;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="DoctorHooksCheck"/> against the real
/// <see cref="ClaudeHooksInstallerService"/> and
/// <see cref="CopilotHooksInstallerService"/> over a temp-directory file
/// system: never installed is not a finding, a fresh install is consistent
/// and current, and a sidecar that no longer agrees with what is on disk
/// (lost, or the entry hand-edited after install) is reported, distinct
/// from a merely outdated entry.
/// </summary>
public sealed class DoctorHooksCheckTests : IDisposable
{
    private static readonly LaunchDescriptor Descriptor = new("/home/agent/.dotnet/tools/nitro", []);
    private static readonly LaunchDescriptor OtherDescriptor = new("/home/agent/.dotnet/tools/nitro", ["--other"]);

    private readonly DirectoryInfo _tempRoot;
    private readonly string _settingsPath;
    private readonly string _hooksJsonPath;
    private readonly string _codexHooksJsonPath;
    private readonly string _codexConfigTomlPath;
    private readonly string _sidecarDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;

    public DoctorHooksCheckTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-doctor-hooks-check-tests");
        _settingsPath = Path.Combine(_tempRoot.FullName, "claude-home", ".claude", "settings.json");
        _hooksJsonPath = Path.Combine(_tempRoot.FullName, "copilot-home", "hooks", "nitro-mail.json");
        _codexHooksJsonPath = Path.Combine(_tempRoot.FullName, "codex-home", ".codex", "hooks.json");
        _codexConfigTomlPath = Path.Combine(_tempRoot.FullName, "codex-home", ".codex", "config.toml");
        _sidecarDirectory = Path.Combine(_tempRoot.FullName, "app-data");
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task CheckClaudeAsync_Should_ReturnNull_When_NeverInstalled()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var (installer, sidecarStore) = CreateClaudeServices();

        // act
        var result = await DoctorHooksCheck.CheckClaudeAsync(installer, sidecarStore, HookInstallScopes.User, ct);

        // assert: opting out is not a doctor finding.
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckClaudeAsync_Should_ReportConsistent_When_JustInstalled()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var (installer, sidecarStore) = CreateClaudeServices();
        await installer.InstallAsync(HookInstallScopes.User, ct);

        // act
        var result = await DoctorHooksCheck.CheckClaudeAsync(installer, sidecarStore, HookInstallScopes.User, ct);

        // assert
        Assert.NotNull(result);
        Assert.True(result.Consistent);
        Assert.Empty(result.Issues);
        Assert.All(result.Events, e => Assert.Equal(HookStatusOutcome.Installed.ToString(), e.Outcome));
    }

    [Fact]
    public async Task CheckClaudeAsync_Should_ReportOutdated_When_TheLaunchDescriptorChangedSinceInstall()
    {
        // arrange: installed once under one descriptor, then the resolver
        // that would run today reports a different one - the same drift
        // `hooks status` calls Outdated - without ever touching the sidecar.
        var ct = TestContext.Current.CancellationToken;
        var (installer, sidecarStore) = CreateClaudeServices(Descriptor);
        await installer.InstallAsync(HookInstallScopes.User, ct);
        var (installerToday, _) = CreateClaudeServices(OtherDescriptor);

        // act
        var result = await DoctorHooksCheck.CheckClaudeAsync(installerToday, sidecarStore, HookInstallScopes.User, ct);

        // assert: every managed event is outdated, but each is still fully
        // explained by the sidecar (no separate "no sidecar record" or
        // "hand-edited" issue alongside it), and the remediation names the
        // claude group, not the deprecated bare verb or another harness.
        Assert.NotNull(result);
        Assert.False(result.Consistent);
        Assert.All(
            result.Issues,
            issue => Assert.Contains("rerun `nitro agent hooks claude install` to refresh it", issue));
        Assert.Equal(result.Events.Count, result.Issues.Count);
    }

    [Fact]
    public async Task CheckClaudeAsync_Should_ReportInconsistent_When_TheSidecarIsLostAfterInstall()
    {
        // arrange: the entries are on disk, but the sidecar that recorded
        // them is gone - a lost or corrupted sidecar, degrading detection to
        // marker matching, per ClaudeHooksSidecarStore's own contract.
        var ct = TestContext.Current.CancellationToken;
        var (installer, sidecarStore) = CreateClaudeServices();
        await installer.InstallAsync(HookInstallScopes.User, ct);
        Directory.Delete(_sidecarDirectory, recursive: true);

        // act
        var result = await DoctorHooksCheck.CheckClaudeAsync(installer, sidecarStore, HookInstallScopes.User, ct);

        // assert
        Assert.NotNull(result);
        Assert.False(result.Consistent);
        Assert.NotEmpty(result.Issues);
        Assert.All(result.Issues, issue => Assert.Contains("no matching sidecar record", issue));
    }

    [Fact]
    public async Task CheckCodexAsync_Should_ReturnNull_When_NeverInstalled()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var (installer, sidecarStore) = CreateCodexServices();

        // act
        var result = await DoctorHooksCheck.CheckCodexAsync(installer, sidecarStore, ct);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckCodexAsync_Should_ReportConsistent_When_JustInstalled()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var (installer, sidecarStore) = CreateCodexServices();
        await installer.InstallAsync(ct);

        // act
        var result = await DoctorHooksCheck.CheckCodexAsync(installer, sidecarStore, ct);

        // assert
        Assert.NotNull(result);
        Assert.True(result.Consistent);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task CheckCopilotAsync_Should_ReturnNull_When_NeverInstalled()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var (installer, sidecarStore) = CreateCopilotServices();

        // act
        var result = await DoctorHooksCheck.CheckCopilotAsync(installer, sidecarStore, ct);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckCopilotAsync_Should_ReportConsistent_When_JustInstalled()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var (installer, sidecarStore) = CreateCopilotServices();
        await installer.InstallAsync(ct);

        // act
        var result = await DoctorHooksCheck.CheckCopilotAsync(installer, sidecarStore, ct);

        // assert
        Assert.NotNull(result);
        Assert.True(result.Consistent);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task CheckCopilotAsync_Should_ReportOutdated_When_TheLaunchDescriptorChangedSinceInstall()
    {
        // arrange: installed once under one descriptor, then the resolver
        // that would run today reports a different one, the same drift
        // `hooks copilot status` calls Outdated, without ever touching the
        // sidecar.
        var ct = TestContext.Current.CancellationToken;
        var (installer, sidecarStore) = CreateCopilotServices(Descriptor);
        await installer.InstallAsync(ct);
        var (installerToday, _) = CreateCopilotServices(OtherDescriptor);

        // act
        var result = await DoctorHooksCheck.CheckCopilotAsync(installerToday, sidecarStore, ct);

        // assert: every managed event is outdated, and the remediation names
        // the copilot group, not claude or the deprecated bare verb.
        Assert.NotNull(result);
        Assert.False(result.Consistent);
        Assert.All(
            result.Issues,
            issue => Assert.Contains("rerun `nitro agent hooks copilot install` to refresh it", issue));
        Assert.Equal(result.Events.Count, result.Issues.Count);
    }

    private (ClaudeHooksInstallerService Installer, IClaudeHooksSidecarStore SidecarStore) CreateClaudeServices(
        LaunchDescriptor? descriptor = null)
    {
        var sidecarStore = new ClaudeHooksSidecarStore(_fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory));
        var installer = new ClaudeHooksInstallerService(
            _fileSystem,
            new FixedClaudeSettingsPathResolver(_settingsPath),
            new FixedLaunchDescriptorResolver(descriptor ?? Descriptor),
            sidecarStore,
            _timeProvider);

        return (installer, sidecarStore);
    }

    private (CodexHooksInstallerService Installer, ICodexHooksSidecarStore SidecarStore) CreateCodexServices(
        LaunchDescriptor? descriptor = null)
    {
        var sidecarStore = new CodexHooksSidecarStore(_fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory));
        var installer = new CodexHooksInstallerService(
            _fileSystem,
            new FixedCodexPathResolver(_codexHooksJsonPath, _codexConfigTomlPath),
            new FixedLaunchDescriptorResolver(descriptor ?? Descriptor),
            sidecarStore,
            _timeProvider);

        return (installer, sidecarStore);
    }

    private (CopilotHooksInstallerService Installer, ICopilotHooksSidecarStore SidecarStore) CreateCopilotServices(
        LaunchDescriptor? descriptor = null)
    {
        var sidecarStore = new CopilotHooksSidecarStore(_fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory));
        var installer = new CopilotHooksInstallerService(
            _fileSystem,
            new FixedCopilotPathResolver(_hooksJsonPath),
            new FixedLaunchDescriptorResolver(descriptor ?? Descriptor),
            sidecarStore,
            _timeProvider);

        return (installer, sidecarStore);
    }

    private sealed class FixedClaudeSettingsPathResolver(string path) : IClaudeSettingsPathResolver
    {
        public string Resolve(string scope) => path;
    }

    private sealed class FixedCopilotPathResolver(string path) : ICopilotPathResolver
    {
        public string ResolveHooksFile() => path;
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
