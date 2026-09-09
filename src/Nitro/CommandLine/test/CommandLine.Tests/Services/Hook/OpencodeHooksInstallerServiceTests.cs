using System.Diagnostics;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

public sealed class OpencodeHooksInstallerServiceTests : IDisposable
{
    private static readonly LaunchDescriptor s_descriptor = new("nitro", []);

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
    /// purely textual assertion on the template source. The stub returns a
    /// hook response with parts to append, so this also exercises
    /// <c>appendParts</c> end to end. Fails when <c>CI_BUILD</c> is set and
    /// node is not found; skips when node is not found and
    /// <c>CI_BUILD</c> is not set.
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
        await File.WriteAllTextAsync(
            scriptPath,
            template + BuildChatMessageDriverScript(exitCode: 0, stdout: """{"parts":["injected one",{"type":"text","text":"injected two"}]}""", includeMessage: true),
            ct);

        // act
        var (exitCode, stdOut, stdErr) = await RunNodeAsync(node!, scriptPath, ct);

        // assert
        Assert.True(exitCode == 0, $"node exited with {exitCode}: {stdErr}");
        var result = JsonDocument.Parse(stdOut).RootElement;
        Assert.Equal("real prompt", result.GetProperty("firstPartText").GetString());
        Assert.True(result.GetProperty("nitroPushed").GetBoolean());
        Assert.Equal(2, result.GetProperty("appendedCount").GetInt32());
        Assert.True(result.GetProperty("appendedPartsAreWellFormed").GetBoolean(), stdOut);
    }

    /// <summary>
    /// Nothing is appended when <c>output.message</c> is absent: without
    /// sessionID/messageID a synthetic part cannot be minted, and appending
    /// one anyway is what kills the prompt with a 500.
    /// </summary>
    [Fact]
    public async Task Build_Should_AppendNothing_When_OutputMessageIsAbsent()
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
        var scriptPath = Path.Combine(_tempRoot.FullName, "shim-no-message.mjs");
        await File.WriteAllTextAsync(
            scriptPath,
            template + BuildChatMessageDriverScript(exitCode: 0, stdout: """{"parts":["injected"]}""", includeMessage: false),
            ct);

        // act
        var (exitCode, stdOut, stdErr) = await RunNodeAsync(node!, scriptPath, ct);

        // assert
        Assert.True(exitCode == 0, $"node exited with {exitCode}: {stdErr}");
        var result = JsonDocument.Parse(stdOut).RootElement;
        Assert.Equal(1, result.GetProperty("outputPartsCount").GetInt32());
        Assert.Equal("real prompt", result.GetProperty("firstPartText").GetString());
    }

    /// <summary>
    /// Nothing is appended when the hook process exits non-zero: <c>invoke</c>
    /// returns <c>{}</c> in that case, so <c>appendParts</c> sees no parts.
    /// </summary>
    [Fact]
    public async Task Build_Should_AppendNothing_When_TheHookProcessExitsNonZero()
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
        var scriptPath = Path.Combine(_tempRoot.FullName, "shim-nonzero-exit.mjs");
        await File.WriteAllTextAsync(
            scriptPath,
            template + BuildChatMessageDriverScript(exitCode: 1, stdout: """{"parts":["injected"]}""", includeMessage: true),
            ct);

        // act
        var (exitCode, stdOut, stdErr) = await RunNodeAsync(node!, scriptPath, ct);

        // assert
        Assert.True(exitCode == 0, $"node exited with {exitCode}: {stdErr}");
        var result = JsonDocument.Parse(stdOut).RootElement;
        Assert.Equal(1, result.GetProperty("outputPartsCount").GetInt32());
        Assert.Equal("real prompt", result.GetProperty("firstPartText").GetString());
    }

    /// <summary>
    /// A driver appended to the generated shim module: stubs
    /// <c>Bun.spawn</c> so <c>chat.message</c> can run under plain Node,
    /// then feeds it an <c>input.parts</c> WITHOUT the pushed prefix and an
    /// <c>output.parts</c> WITH it, printing the stripped output text, the
    /// <c>nitroPushed</c> flag the shim sent to the hook process, and the
    /// parts <c>appendParts</c> appended to <c>output.parts</c>.
    /// </summary>
    private static string BuildChatMessageDriverScript(int exitCode, string stdout, bool includeMessage)
        => """


        globalThis.Bun = {
          spawn() {
            return {
              stdin: { write: (chunk) => { globalThis.__capturedStdin = chunk; }, end() {} },
              stdout: __STDOUT__,
              exited: Promise.resolve(__EXIT_CODE__),
            };
          },
        };

        const hooks = await nitroHooks({ serverUrl: "http://127.0.0.1:4096" });
        const input = { sessionID: "ses_1", parts: [{ type: "text", text: "should not be read" }] };
        const output = {
          message: __MESSAGE__,
          parts: [{ id: "prt_1", type: "text", text: __PREFIX__ + "real prompt" }],
        };

        await hooks["chat.message"](input, output);

        const appended = output.parts.slice(1);
        const ids = appended.map((part) => part.id);
        const appendedPartsAreWellFormed =
          appended.every((part) =>
            typeof part.id === "string" && part.id.length > 0 && part.id.startsWith("prt_") &&
            part.sessionID === "ses_1" && part.messageID === "msg_1" && part.synthetic === true) &&
          new Set(ids).size === ids.length &&
          [...ids].sort().every((id, index) => id === ids[index]);

        console.log(JSON.stringify({
          firstPartText: output.parts[0].text,
          outputPartsCount: output.parts.length,
          appendedCount: appended.length,
          appendedPartsAreWellFormed,
          nitroPushed: JSON.parse(globalThis.__capturedStdin).nitroPushed,
        }));
        """
            .Replace("__PREFIX__", JsonSerializer.Serialize(OpencodeHookProtocol.PushedPromptPrefix), StringComparison.Ordinal)
            .Replace("__STDOUT__", JsonSerializer.Serialize(stdout), StringComparison.Ordinal)
            .Replace("__EXIT_CODE__", exitCode.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("__MESSAGE__", includeMessage ? """{ "sessionID": "ses_1", "id": "msg_1" }""" : "undefined", StringComparison.Ordinal);

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
        new FixedLaunchDescriptorResolver(s_descriptor),
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
