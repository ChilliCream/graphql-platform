using ChilliCream.Nitro.CommandLine.Tests.Commands;

namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class WatchMailCommandTests(NitroCommandFixture fixture)
    : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "watch", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Wait for new mail addressed to the acting agent and print it. Messages already unread at start do not trigger; see `inbox --unread` for those. Never marks anything read.

            Usage:
              nitro agent mail watch [options]

            Options:
              --timeout <timeout>  Exit with an error after this many seconds if no new mail arrives (waits until cancelled when omitted)
              --actor <actor>      The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --output <json>      The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help       Show help and usage information

            Example:
              nitro agent mail watch
              nitro agent mail watch --timeout 30
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "mail", "watch", "--timeout", "1");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task NewMessage_PrintsAndExitsSuccess_When_ArrivesAfterBaseline()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        using var cancellationTokenSource = CreateWatchCancellationTokenSource();
        var watchCommand = StartInteractiveCommand("agent", "mail", "watch");
        var runTask = watchCommand.RunToCompletionAsync(cancellationTokenSource.Token);

        // act
        await Task.Delay(50, cancellationTokenSource.Token);
        var message = await SeedMessageAsync("bob", "Ping", ["test-agent"], body: "Are you there?");
        var result = await AdvanceUntilCompleteAsync(runTask, cancellationTokenSource.Token);

        // assert
        result.AssertSuccess(
            $"""
            From: bob
            To: test-agent
            Date: 2026-01-01 00:00
            Subject: Ping
            Thread: {message.Id}

            Are you there?
            """);
    }

    [Fact]
    public async Task NewMessages_PrintOldestFirst_When_SeveralArrive()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        using var cancellationTokenSource = CreateWatchCancellationTokenSource();
        var watchCommand = StartInteractiveCommand("agent", "mail", "watch");
        var runTask = watchCommand.RunToCompletionAsync(cancellationTokenSource.Token);

        // act
        // Both messages are seeded, in order, before the watch command's
        // first poll fires (its 1-second timer is still pending), so both
        // arrive together on the same poll and must print oldest first.
        await Task.Delay(50, cancellationTokenSource.Token);
        var first = await SeedMessageAsync("bob", "First", ["test-agent"]);
        FakeTime.Advance(TimeSpan.FromMilliseconds(100));
        var second = await SeedMessageAsync("bob", "Second", ["test-agent"]);
        var result = await AdvanceUntilCompleteAsync(runTask, cancellationTokenSource.Token);

        // assert
        result.AssertSuccess(
            $"""
            From: bob
            To: test-agent
            Date: 2026-01-01 00:00
            Subject: First
            Thread: {first.Id}

            body

            ---

            From: bob
            To: test-agent
            Date: 2026-01-01 00:00
            Subject: Second
            Thread: {second.Id}

            body
            """);
    }

    [Fact]
    public async Task PreExistingUnread_DoesNotTrigger()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        await SeedMessageAsync("bob", "Already here", ["test-agent"]);

        // act
        var result = await AdvanceUntilCompleteAsync(
            StartInteractiveCommand("agent", "mail", "watch", "--timeout", "2")
                .RunToCompletionAsync(TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken);

        // assert
        result.AssertError(
            """
            Timed out waiting for new mail.
            """);
    }

    [Fact]
    public async Task Timeout_ExitsError_When_NoNewMailArrives()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");

        // act
        var result = await AdvanceUntilCompleteAsync(
            StartInteractiveCommand("agent", "mail", "watch", "--timeout", "3")
                .RunToCompletionAsync(TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken);

        // assert
        result.AssertError(
            """
            Timed out waiting for new mail.
            """);
    }

    [Fact]
    public async Task DoesNotMarkRead_When_MessageArrives()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        using var cancellationTokenSource = CreateWatchCancellationTokenSource();
        var watchCommand = StartInteractiveCommand("agent", "mail", "watch");
        var runTask = watchCommand.RunToCompletionAsync(cancellationTokenSource.Token);
        await Task.Delay(50, cancellationTokenSource.Token);
        var message = await SeedMessageAsync("bob", "Ping", ["test-agent"]);

        // act
        var result = await AdvanceUntilCompleteAsync(runTask, cancellationTokenSource.Token);

        // assert
        Assert.Equal(0, result.ExitCode);
        var readAt = await QueryScalarAsync(
            "SELECT read_at FROM message_recipients "
            + $"WHERE message_id = '{message.Id}' AND recipient = 'test-agent'");
        Assert.Null(readAt);
    }

    [Fact]
    public async Task Cancellation_ExitsCleanly_When_NoMessageArrives()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        using var cancellationTokenSource = CreateWatchCancellationTokenSource();
        var watchCommand = StartInteractiveCommand("agent", "mail", "watch");
        var runTask = watchCommand.RunToCompletionAsync(cancellationTokenSource.Token);

        // act
        await Task.Delay(50, cancellationTokenSource.Token);
        await cancellationTokenSource.CancelAsync();
        var result = await runTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        Assert.Empty(result.StdErr);
    }

    [Fact]
    public async Task JsonOutput_ReturnsMessageDetailList()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("test-agent");
        await SeedAgentAsync("bob");
        SetupInteractionMode(InteractionMode.JsonOutput);
        using var cancellationTokenSource = CreateWatchCancellationTokenSource();
        var watchCommand = StartInteractiveCommand("agent", "mail", "watch");
        var runTask = watchCommand.RunToCompletionAsync(cancellationTokenSource.Token);

        // act
        await Task.Delay(50, cancellationTokenSource.Token);
        var message = await SeedMessageAsync("bob", "Ping", ["test-agent"], body: "Are you there?");
        var result = await AdvanceUntilCompleteAsync(runTask, cancellationTokenSource.Token);

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(message.Id, items[0].GetProperty("id").GetString());
        Assert.Equal("Are you there?", items[0].GetProperty("body").GetString());
        Assert.False(items[0].GetProperty("read").GetBoolean());
    }

    private static CancellationTokenSource CreateWatchCancellationTokenSource()
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(15));
        return cancellationTokenSource;
    }

    /// <summary>
    /// Advances the fake clock by one second at a time, on a short real
    /// interval, until the watch command's task completes. The real delay
    /// only paces test synchronization; the simulated one-second poll
    /// cadence and any --timeout are driven entirely by <see cref="FakeTime"/>.
    /// </summary>
    private async Task<CommandResult> AdvanceUntilCompleteAsync(
        Task<CommandResult> runTask,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < 200 && !runTask.IsCompleted; i++)
        {
            await Task.Delay(10, cancellationToken);
            FakeTime.Advance(TimeSpan.FromSeconds(1));
        }

        return await runTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
    }
}
