namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class BoardMailCommandTests(NitroCommandFixture fixture) : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task RegistersLiveOperatorBoardSession_WithDbWatchEndpoint_And_RemovesItOnClose()
    {
        // arrange: pinned so the board's own session-generation resolution
        // never touches the real machine's application data directory.
        SetupInstanceId("board-test-host");
        SetupGlobalConfigDirectory(WorkingDirectory);
        await InitWorkspaceAsync();

        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(15));
        var board = StartInteractiveCommand("agent", "mail", "board");
        var runTask = board.RunToCompletionAsync(cancellationTokenSource.Token);

        // act: wait for the board's own live presence row to appear.
        await WaitUntilAsync(
            async () => await QueryScalarAsync(
                "SELECT COUNT(*) FROM agent_sessions WHERE harness = 'nitro-board'") == "1",
            cancellationTokenSource.Token);

        var role = await QueryScalarAsync("SELECT role FROM agent_sessions WHERE harness = 'nitro-board'");
        var endpointKind = await QueryScalarAsync(
            "SELECT endpoint_kind FROM agent_sessions WHERE harness = 'nitro-board'");
        var agentName = await QueryScalarAsync("SELECT agent_name FROM agent_sessions WHERE harness = 'nitro-board'");
        var bindingKind = await QueryScalarAsync(
            "SELECT binding_kind FROM agent_sessions WHERE harness = 'nitro-board'");

        await cancellationTokenSource.CancelAsync();
        var result = await runTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert: the resolved mail actor ("test-agent", from
        // MailCommandTestBase's NITRO_MAIL_ACTOR) is bound as the operator,
        // reporting online through the shared database file rather than a
        // routable transport, and closing the board removes only this live
        // row.
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("operator", role);
        Assert.Equal("db-watch", endpointKind);
        Assert.Equal("test-agent", agentName);
        Assert.Equal("env", bindingKind);

        var remaining = await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions WHERE harness = 'nitro-board'");
        Assert.Equal("0", remaining);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 500; i++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(10, cancellationToken);
        }

        throw new TimeoutException("Condition was not met in time.");
    }

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "board", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Open the interactive mail board.

            Usage:
              nitro agent mail board [options]

            Options:
              -?, -h, --help  Show help and usage information

            Example:
              nitro agent mail board
            """);
    }

    [Fact]
    public async Task NonInteractive_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "board");

        // assert
        result.AssertError("agent mail board requires an interactive terminal.");
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "board");

        // assert
        result.AssertError("No agent workspace found. Run `nitro agent init` first.");
    }
}
