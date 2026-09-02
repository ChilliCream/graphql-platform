using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

public sealed class OpencodeHooksInstallerServiceTests : IDisposable
{
    private static readonly LaunchDescriptor Descriptor = new("nitro", []);

    private readonly DirectoryInfo _tempRoot;
    private readonly string _hooksPath;
    private readonly string _sidecarDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;

    public OpencodeHooksInstallerServiceTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-opencode-hooks-installer-tests");
        _hooksPath = Path.Combine(_tempRoot.FullName, "opencode", "plugins", "nitro-hooks.js");
        _sidecarDirectory = Path.Combine(_tempRoot.FullName, "app-data");
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task InstallAsync_MissingFile_CreatesShimAndSidecar()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();

        // act
        var report = await service.InstallAsync(HookInstallScopes.User, ct);

        // assert
        Assert.Equal(HookInstallOutcome.Installed, report.Outcome);
        Assert.True(File.Exists(_hooksPath));
        Assert.Contains(OpencodeHooksTemplate.OwnershipMarker, await File.ReadAllTextAsync(_hooksPath, ct));
        Assert.True(File.Exists(Path.Combine(_sidecarDirectory, "opencode-hooks-sidecar.json")));
    }

    [Fact]
    public async Task InstallAsync_SecondRun_IsUnchanged()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();
        await service.InstallAsync(HookInstallScopes.User, ct);
        var original = await File.ReadAllTextAsync(_hooksPath, ct);

        // act
        var report = await service.InstallAsync(HookInstallScopes.User, ct);

        // assert
        Assert.Equal(HookInstallOutcome.Unchanged, report.Outcome);
        Assert.Equal(original, await File.ReadAllTextAsync(_hooksPath, ct));
    }

    [Fact]
    public async Task StatusAsync_DriftedManagedShim_IsOutdated()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();
        await service.InstallAsync(HookInstallScopes.User, ct);
        await File.WriteAllTextAsync(_hooksPath, $"// {OpencodeHooksTemplate.OwnershipMarker}\n// edited", ct);

        // act
        var report = await service.StatusAsync(HookInstallScopes.User, ct);

        // assert
        Assert.Equal(HookStatusOutcome.Outdated, report.Outcome);
    }

    [Fact]
    public async Task UninstallAsync_DriftedManagedShim_RemovesByMarker()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();
        await service.InstallAsync(HookInstallScopes.User, ct);
        await File.WriteAllTextAsync(_hooksPath, $"// {OpencodeHooksTemplate.OwnershipMarker}\n// edited", ct);

        // act
        var report = await service.UninstallAsync(HookInstallScopes.User, ct);

        // assert
        Assert.Equal(HookUninstallOutcome.Removed, report.Outcome);
        Assert.False(File.Exists(_hooksPath));
    }

    [Fact]
    public async Task UninstallAsync_ForeignShim_LeavesFileUntouched()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();
        Directory.CreateDirectory(Path.GetDirectoryName(_hooksPath)!);
        const string foreignShim = "export default async function foreignPlugin() {}\n";
        await File.WriteAllTextAsync(_hooksPath, foreignShim, ct);

        // act
        var report = await service.UninstallAsync(HookInstallScopes.User, ct);

        // assert
        Assert.Equal(HookUninstallOutcome.NotPresent, report.Outcome);
        Assert.Equal(foreignShim, await File.ReadAllTextAsync(_hooksPath, ct));
    }

    [Fact]
    public void Build_DescriptorWithMuxerAndDll_EmbedsBothTokens()
    {
        // arrange
        var descriptor = new LaunchDescriptor("dotnet", ["/tools/nitro.dll"]);

        // act
        var template = OpencodeHooksTemplate.Build(descriptor);

        // assert
        Assert.Contains("[\"dotnet\",\"/tools/nitro.dll\"]", template, StringComparison.Ordinal);
        Assert.Contains("session.created", template, StringComparison.Ordinal);
        Assert.Contains("chat.message", template, StringComparison.Ordinal);
        Assert.Contains("session.deleted", template, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_Should_StripThePushedMarkerAndForwardTheStablePayload()
    {
        // arrange
        var descriptor = new LaunchDescriptor("nitro", []);

        // act
        var template = OpencodeHooksTemplate.Build(descriptor);

        // assert
        Assert.Contains($"const nitroPushedMarker = \"{OpencodeHookProtocol.PushedPromptMarker}\"", template);
        Assert.Contains("part.text = part.text.slice(nitroPushedMarker.length);", template);
        Assert.Contains("nitroPushed,", template);
    }

    private OpencodeHooksInstallerService CreateService() => new(
        _fileSystem,
        new FixedOpencodePathResolver(_hooksPath),
        new FixedLaunchDescriptorResolver(Descriptor),
        new OpencodeHooksSidecarStore(_fileSystem, new FixedSidecarDirectoryProvider(_sidecarDirectory)),
        _timeProvider);

    private sealed class FixedOpencodePathResolver(string path) : IOpencodePathResolver
    {
        public string Resolve(string scope) => path;
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
