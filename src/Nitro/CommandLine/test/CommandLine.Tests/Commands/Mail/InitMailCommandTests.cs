using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class InitMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "init", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Initialize a mail workspace in the current directory.

            Usage:
              nitro agent mail init [options]

            Options:
              --force          Reinitialize an existing mail workspace
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent mail init
              nitro agent mail init --force
            """);
    }

    [Fact]
    public async Task EmptyDirectory_InitializesWorkspace()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "init");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized mail workspace at '.nitro/agents'.
            """);
        Assert.True(File.Exists(DatabasePath));
        var gitIgnoreText = await File.ReadAllTextAsync(
            Path.Combine(WorkspaceDirectory, AgentWorkspace.GitIgnoreFileName),
            TestContext.Current.CancellationToken);
        gitIgnoreText.MatchInlineSnapshot(
            """
            # The agent database is local state; tasks.jsonl is the source of truth in git.
            agents.db
            agents.db-wal
            agents.db-shm
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsWorkspacePath()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "init");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(WorkspaceDirectory, root.GetProperty("path").GetString());
    }

    [Fact]
    public async Task AlreadyInitialized_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "init");

        // assert
        result.AssertError(
            """
            Already initialized at '.nitro/agents'. Use --force to reinitialize.
            """);
    }

    [Fact]
    public async Task AlreadyInitialized_Force_Reinitializes()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "init", "--force");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized mail workspace at '.nitro/agents'.
            """);
        Assert.True(File.Exists(DatabasePath));
    }
}
