using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class OpencodeHooksCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_HooksOpencode_ExplainsTheFailOpenLocalOnlySetup()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("fail-open", result.StdOut, StringComparison.Ordinal);
        Assert.Contains(".gitignore", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Help_HooksOpencodeInstall_DescribesTheTwoScopes()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--scope <project|user>", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("[default: user]", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InstallStatusUninstall_OpencodeGroup_ProjectScope_RoundTripsWithoutUsingTheHomeDirectory()
    {
        // arrange
        var sidecarDirectory = Path.Combine(WorkingDirectory, "..", "app-data");
        SetupGlobalConfigDirectory(sidecarDirectory);
        await InitWorkspaceAsync();

        // act and assert
        var install = await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--scope", "project");
        Assert.Equal(0, install.ExitCode);
        Assert.True(File.Exists(Path.Combine(WorkingDirectory, ".opencode", "plugin", "nitro-hooks.js")));

        var statusAfterInstall =
            await ExecuteCommandAsync("agent", "hooks", "opencode", "status", "--scope", "project");
        Assert.Equal(0, statusAfterInstall.ExitCode);

        var uninstall = await ExecuteCommandAsync("agent", "hooks", "opencode", "uninstall", "--scope", "project");
        Assert.Equal(0, uninstall.ExitCode);

        var statusAfterUninstall =
            await ExecuteCommandAsync("agent", "hooks", "opencode", "status", "--scope", "project");
        Assert.Equal(1, statusAfterUninstall.ExitCode);
    }

    [Fact]
    public async Task ResolveAsync_FakeVersionReader_ParsesTheVersion()
    {
        // arrange
        var resolver = new OpencodeVersionResolver(_ => Task.FromResult<string?>("opencode 1.18.23"));

        // act
        var result = await resolver.ResolveAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(new Version(1, 18, 23), result);
    }
}
