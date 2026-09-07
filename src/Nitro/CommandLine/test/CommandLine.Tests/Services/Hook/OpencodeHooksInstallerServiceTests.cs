using System.Diagnostics;
using System.Text.Json;
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
    public void Build_Should_EmbedTheReservedPushedPromptPrefix()
    {
        // arrange
        var descriptor = new LaunchDescriptor("nitro", []);

        // act
        var template = OpencodeHooksTemplate.Build(descriptor);

        // assert
        Assert.Contains($"const nitroPushedPrefix = \"{OpencodeHookProtocol.PushedPromptPrefix}\"", template);
        Assert.Contains("function stripNitroPushedPrefix(parts)", template);
        Assert.Contains("nitroPushed,", template);
    }

    /// <summary>
    /// Regression for a prior generated-shim defect: the pushed-prompt
    /// prefix must be detected and stripped from <c>output.parts</c> (the
    /// message opencode actually delivers to the model), not
    /// <c>input.parts</c>. Runs the generated JavaScript itself under Node,
    /// with <c>Bun.spawn</c> stubbed to capture the payload the shim sends
    /// to the hook process instead of a real CLI process, so the regression
    /// is caught even though the two objects would look identical to a
    /// purely textual assertion on the template source. Fails when
    /// <c>CI_BUILD</c> is set and node is not found; skips when node is not
    /// found and <c>CI_BUILD</c> is not set.
    /// </summary>
    [Fact]
    public async Task Build_Should_StripThePrefixFromOutputPartsOnly_When_TheGeneratedShimRunsAChatMessage()
    {
        // arrange
        var node = FindNode();

        if (node is null)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI_BUILD")))
            {
                Assert.Fail("node was not found on PATH; CI must provide node for the generated-JavaScript regression.");
            }

            Assert.Skip("node was not found on PATH.");
        }

        var ct = TestContext.Current.CancellationToken;
        var template = OpencodeHooksTemplate.Build(new LaunchDescriptor("nitro", []));
        var scriptPath = Path.Combine(_tempRoot.FullName, "shim-regression.mjs");
        await File.WriteAllTextAsync(scriptPath, template + BuildChatMessageDriverScript(), ct);

        // act
        var (exitCode, stdOut, stdErr) = await RunNodeAsync(node!, scriptPath, ct);

        // assert
        Assert.True(exitCode == 0, $"node exited with {exitCode}: {stdErr}");
        var result = JsonDocument.Parse(stdOut).RootElement;
        Assert.Equal("real prompt", result.GetProperty("outputText").GetString());
        Assert.True(result.GetProperty("nitroPushed").GetBoolean());
    }

    /// <summary>
    /// A driver appended to the generated shim module: stubs
    /// <c>Bun.spawn</c> so <c>chat.message</c> can run under plain Node,
    /// then feeds it an <c>input.parts</c> WITHOUT the pushed prefix and an
    /// <c>output.parts</c> WITH it, printing the stripped output text and
    /// the <c>nitroPushed</c> flag the shim sent to the hook process.
    /// </summary>
    private static string BuildChatMessageDriverScript()
        => """


        globalThis.Bun = {
          spawn() {
            return {
              stdin: { write: (chunk) => { globalThis.__capturedStdin = chunk; }, end() {} },
              stdout: "",
              exited: Promise.resolve(1),
            };
          },
        };

        const hooks = await nitroHooks({ serverUrl: "http://127.0.0.1:4096" });
        const input = { sessionID: "ses_1", parts: [{ type: "text", text: "should not be read" }] };
        const output = { parts: [{ type: "text", text: __PREFIX__ + "real prompt" }] };

        await hooks["chat.message"](input, output);

        console.log(JSON.stringify({
          outputText: output.parts[0].text,
          nitroPushed: JSON.parse(globalThis.__capturedStdin).nitroPushed,
        }));
        """.Replace("__PREFIX__", JsonSerializer.Serialize(OpencodeHookProtocol.PushedPromptPrefix), StringComparison.Ordinal);

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunNodeAsync(
        string nodePath, string scriptPath, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(nodePath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        process.StartInfo.ArgumentList.Add(scriptPath);
        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, await stdOutTask, await stdErrTask);
    }

    private static string? FindNode()
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathVariable))
        {
            return null;
        }

        var exeName = OperatingSystem.IsWindows() ? "node.exe" : "node";

        foreach (var directory in pathVariable.Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(directory, exeName);

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
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
