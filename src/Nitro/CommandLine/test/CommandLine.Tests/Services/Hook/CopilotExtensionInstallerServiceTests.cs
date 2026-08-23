using System.Runtime.CompilerServices;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CopilotExtensionInstallerService"/> against a real
/// temp-directory file system: the extension asset round trip, the config
/// round trip, and the overwrite-refusal-on-unknown-hash rule (perles-net-k3j.16).
/// </summary>
public sealed class CopilotExtensionInstallerServiceTests : IDisposable
{
    private static readonly LaunchDescriptor Descriptor = new("/home/agent/.dotnet/tools/nitro", []);

    private readonly DirectoryInfo _tempRoot;
    private readonly string _extensionPath;
    private readonly string _configPath;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;

    public CopilotExtensionInstallerServiceTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-copilot-extension-installer-tests");
        var extensionDirectory = Path.Combine(_tempRoot.FullName, ".github", "extensions", "nitro-mail");
        _extensionPath = Path.Combine(extensionDirectory, "extension.mjs");
        _configPath = Path.Combine(extensionDirectory, "nitro-mail.config.json");
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task InstallAsync_MissingFile_CreatesTheAssetAndTheConfig()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();

        var report = await service.InstallAsync(force: false, ct);

        Assert.Equal(CopilotExtensionInstallOutcome.Installed, report.Outcome);
        Assert.True(File.Exists(_extensionPath));
        Assert.True(File.Exists(_configPath));

        var text = await File.ReadAllTextAsync(_extensionPath, ct);
        Assert.Equal(CopilotExtensionAsset.Content, text);

        var configJson = await File.ReadAllTextAsync(_configPath, ct);
        Assert.Contains("\"executable\":\"/home/agent/.dotnet/tools/nitro\"", configJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InstallAsync_SecondRun_IsUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();

        await service.InstallAsync(force: false, ct);
        var writeTimeAfterFirst = File.GetLastWriteTimeUtc(_extensionPath);

        await Task.Delay(50, ct);

        var report = await service.InstallAsync(force: false, ct);

        Assert.Equal(CopilotExtensionInstallOutcome.Unchanged, report.Outcome);
        Assert.Equal(writeTimeAfterFirst, File.GetLastWriteTimeUtc(_extensionPath));
    }

    [Fact]
    public async Task InstallAsync_UnrecognizedOnDiskContent_RefusesWithoutForce()
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.GetDirectoryName(_extensionPath)!);
        await File.WriteAllTextAsync(_extensionPath, "// a hand-edited file, not ours", ct);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ExitException>(
            () => service.InstallAsync(force: false, ct));

        Assert.Contains("does not match any known", exception.Message, StringComparison.Ordinal);
        Assert.Equal("// a hand-edited file, not ours", await File.ReadAllTextAsync(_extensionPath, ct));
    }

    [Fact]
    public async Task InstallAsync_UnrecognizedOnDiskContent_WithForce_Overwrites()
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.GetDirectoryName(_extensionPath)!);
        await File.WriteAllTextAsync(_extensionPath, "// a hand-edited file, not ours", ct);
        var service = CreateService();

        var report = await service.InstallAsync(force: true, ct);

        Assert.Equal(CopilotExtensionInstallOutcome.Forced, report.Outcome);
        Assert.Equal(CopilotExtensionAsset.Content, await File.ReadAllTextAsync(_extensionPath, ct));
    }

    [Fact]
    public async Task InstallAsync_V1OnDisk_OverwritesReportingUpdated()
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.GetDirectoryName(_extensionPath)!);
        await File.WriteAllTextAsync(_extensionPath, await ReadV1FixtureAsync(ct), ct);
        var service = CreateService();

        var report = await service.InstallAsync(force: false, ct);

        Assert.Equal(CopilotExtensionInstallOutcome.Updated, report.Outcome);
        Assert.Equal(CopilotExtensionAsset.Content, await File.ReadAllTextAsync(_extensionPath, ct));
    }

    [Fact]
    public async Task StatusAsync_V1OnDisk_ReportsOutdated()
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.GetDirectoryName(_extensionPath)!);
        var v1Content = await ReadV1FixtureAsync(ct);
        await File.WriteAllTextAsync(_extensionPath, v1Content, ct);
        var service = CreateService();

        var report = await service.StatusAsync(ct);

        Assert.Equal(CopilotExtensionStatusOutcome.Outdated, report.Outcome);
        // Pins CopilotExtensionAsset.KnownPriorHashes[0] to the fixture it is
        // supposed to describe, so a wrong constant would fail here, not
        // just silently report the fixture as Unrecognized above.
        Assert.Equal(CopilotExtensionAsset.KnownPriorHashes[0], CopilotExtensionAsset.Hash(v1Content));
    }

    [Fact]
    public async Task StatusAsync_MissingFile_ReportsMissing()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();

        var report = await service.StatusAsync(ct);

        Assert.Equal(CopilotExtensionStatusOutcome.Missing, report.Outcome);
    }

    [Fact]
    public async Task StatusAsync_AfterInstall_ReportsCurrent()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();
        await service.InstallAsync(force: false, ct);

        var report = await service.StatusAsync(ct);

        Assert.Equal(CopilotExtensionStatusOutcome.Current, report.Outcome);
    }

    [Fact]
    public async Task StatusAsync_UnrecognizedOnDiskContent_ReportsUnrecognized()
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.GetDirectoryName(_extensionPath)!);
        await File.WriteAllTextAsync(_extensionPath, "// not ours", ct);
        var service = CreateService();

        var report = await service.StatusAsync(ct);

        Assert.Equal(CopilotExtensionStatusOutcome.Unrecognized, report.Outcome);
    }

    [Fact]
    public async Task UninstallAsync_RemovesTheAssetAndTheConfig()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();
        await service.InstallAsync(force: false, ct);

        var report = await service.UninstallAsync(ct);

        Assert.True(report.Removed);
        Assert.False(File.Exists(_extensionPath));
        Assert.False(File.Exists(_configPath));
    }

    [Fact]
    public async Task UninstallAsync_NothingInstalled_IsANoOp()
    {
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();

        var report = await service.UninstallAsync(ct);

        Assert.False(report.Removed);
    }

    private CopilotExtensionInstallerService CreateService() => new(
        _fileSystem,
        new FixedCopilotExtensionPathResolver(_extensionPath, _configPath),
        new FixedLaunchDescriptorResolver(Descriptor),
        _timeProvider);

    /// <summary>
    /// The exact byte-for-byte version-1 <c>extension.mjs</c> (extracted from
    /// commit 7e199f8e90, before the DRAINING-wedge/version-2 fixes),
    /// checked in so <see cref="CopilotExtensionAsset.KnownPriorHashes"/>'s
    /// first entry is tested against real prior bytes, not a hand-typed hash.
    /// </summary>
    private static Task<string> ReadV1FixtureAsync(CancellationToken ct, [CallerFilePath] string thisFilePath = "")
    {
        // thisFilePath: .../test/CommandLine.Tests/Services/Hook/CopilotExtensionInstallerServiceTests.cs
        var directory = Path.GetDirectoryName(thisFilePath)!; // .../Services/Hook

        for (var i = 0; i < 3; i++)
        {
            directory = Path.GetDirectoryName(directory)
                ?? throw new InvalidOperationException($"Could not walk up from '{thisFilePath}'.");
        }

        // directory is now .../test
        var fixturePath = Path.Combine(directory, "fixtures", "copilot-extension", "extension.v1.mjs");

        return File.ReadAllTextAsync(fixturePath, ct);
    }

    private sealed class FixedCopilotExtensionPathResolver(string extensionPath, string configPath)
        : ICopilotExtensionPathResolver
    {
        public string ResolveExtensionFile() => extensionPath;

        public string ResolveConfigFile() => configPath;
    }

    private sealed class FixedLaunchDescriptorResolver(LaunchDescriptor descriptor) : ILaunchDescriptorResolver
    {
        public LaunchDescriptor Resolve() => descriptor;
    }
}
