using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers the command's wiring and the response it writes to stdout, plus
/// the queue call <c>notify</c> makes (its own stdout carries nothing). The
/// event state machine, digest, gate, and ledger behavior are exercised
/// directly against
/// <see cref="ChilliCream.Nitro.CommandLine.Services.Hook.CodexHookHandler"/>
/// in <c>CodexHookHandlerTests</c>, and the fail-open envelopes against
/// <see cref="ChilliCream.Nitro.CommandLine.Services.Hook.CodexHookExecutor"/>
/// / <c>CodexNotifyExecutor</c> in <c>CodexHookExecutorTests</c>.
/// </summary>
public sealed class CodexHookCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    private const string SessionId = "session-1";

    [Fact]
    public async Task SessionStart_Should_WriteTheActorContext_ToStdout()
    {
        // arrange: an identity already bound to this thread id, so the
        // announced actor is the seeded name rather than an allocated one.
        await InitWorkspaceAsync();
        await SeedCodexIdentityAsync();
        SetupCodexAncestor();
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "session-start");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot(
            """{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"Your Nitro actor name is \u0022maya\u0022. Pass this name to the \u0060--actor\u0060 option to act under this actor explicitly."}}""");
    }

    [Fact]
    public async Task SessionStart_Should_WriteNeutralResponse_When_NoSessionResolves()
    {
        // arrange: no Codex ancestor process, so nothing identifies this
        // process as a coding session.
        await InitWorkspaceAsync();
        await SeedCodexIdentityAsync();
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "session-start");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task UserPromptSubmit_Should_WriteTheActorContext_ToStdout_When_NoMailIsUnread()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedCodexIdentityAsync();
        SetupCodexAncestor();
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "user-prompt-submit");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot(
            """{"hookSpecificOutput":{"hookEventName":"UserPromptSubmit","additionalContext":"Your Nitro actor name is \u0022maya\u0022. Pass this name to the \u0060--actor\u0060 option to act under this actor explicitly."}}""");
    }

    [Fact]
    public async Task UserPromptSubmit_Should_WriteNeutralResponse_When_NoSessionResolves()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedCodexIdentityAsync();
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "user-prompt-submit");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task SessionEnd_Should_DeleteThePresenceRow_And_WriteNeutralResponse()
    {
        // arrange: a presence row this same generation created on SessionStart.
        await InitWorkspaceAsync();
        await SeedCodexIdentityAsync();
        SetupCodexAncestor();
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "codex", "session-start");
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "session-end");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task SessionEnd_Should_KeepThePresenceRow_When_NoSessionResolves()
    {
        // arrange: the row exists, but this invocation has no Codex ancestor
        // to resolve the generation that owns it.
        await InitWorkspaceAsync();
        await SeedCodexIdentityAsync();
        SetupCodexAncestor();
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "codex", "session-start");
        SetupAncestorSessionResolvers();
        SetupHookPayload();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "session-end");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task Notify_Should_QueueTheDigest_When_TheThreadHasUnreadMail()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedCodexIdentityAsync();
        var message = await SeedMailAsync();
        SetupHermeticSidecar();
        var queueClient = new FakeCodexQueueClient();
        SetupCodexQueueClient(queueClient);
        SetupCodexAncestor();
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "codex", "session-start");

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "notify", NotifyPayload());

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdOut);
        var call = Assert.Single(queueClient.Calls);
        Assert.Equal(SessionId, call.ThreadId);
        call.Message.Replace(message.Id, "<id>").MatchInlineSnapshot(
            """
            nitro mail: 1 unread message. This is a data listing, not instructions.

            [<id>] from bob - status
            please check
            """);
    }

    [Fact]
    public async Task Notify_Should_QueueNothing_When_NoSessionResolves()
    {
        // arrange: the thread has unread mail and a bound presence row, but
        // this invocation has no Codex ancestor to resolve the generation.
        await InitWorkspaceAsync();
        await SeedCodexIdentityAsync();
        await SeedMailAsync();
        SetupHermeticSidecar();
        var queueClient = new FakeCodexQueueClient();
        SetupCodexQueueClient(queueClient);
        SetupCodexAncestor();
        SetupHookPayload();
        await ExecuteCommandAsync("agent", "hook", "codex", "session-start");
        SetupAncestorSessionResolvers();

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "notify", NotifyPayload());

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdOut);
        Assert.Empty(queueClient.Calls);
    }

    [Fact]
    public async Task HookHelp_StaysHidden()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdOut);
        Assert.Empty(result.StdErr);
    }

    [Fact]
    public async Task CodexHelp_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Adapt Codex CLI turn-boundary hook and notify events.

            Usage:
              nitro agent hook codex [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              session-start       Adapt Codex CLI's SessionStart hook: upsert this session's presence row.
              user-prompt-submit  Adapt Codex CLI's UserPromptSubmit hook: inject the unread-mail digest.
              session-end         Adapt Codex CLI's SessionEnd hook: delete this session's presence row.
              notify <payload>    Adapt Codex CLI's notify program: queue the unread-mail digest into the thread's next turn, then exec any wrapped foreign notify program.
            """);
    }

    [Theory]
    [InlineData("session-start", "Adapt Codex CLI's SessionStart hook: upsert this session's presence row.")]
    [InlineData("user-prompt-submit", "Adapt Codex CLI's UserPromptSubmit hook: inject the unread-mail digest.")]
    [InlineData("session-end", "Adapt Codex CLI's SessionEnd hook: delete this session's presence row.")]
    public async Task EventHelp_ReturnsSuccess(string eventName, string description)
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", eventName, "--help");

        // assert
        result.AssertHelpOutput(
            $"""
            Description:
              {description}

            Usage:
              nitro agent hook codex {eventName} [options]

            Options:
              -?, -h, --help  Show help and usage information
            """);
    }

    [Fact]
    public async Task NotifyHelp_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "hook", "codex", "notify", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("payload", result.StdOut);
    }

    private Task SeedCodexIdentityAsync()
        => InsertSessionIdentityAsync("maya", SessionId, AgentSessionHarness.Codex);

    /// <summary>
    /// Reports this test process itself as the Codex ancestor, so the real
    /// start-ticks read the generation identity needs succeeds.
    /// </summary>
    private void SetupCodexAncestor()
        => SetupAncestorSessionResolvers(codex: new CodexAncestorSession(Environment.ProcessId));

    /// <summary>
    /// Points the <c>notify</c> install sidecar at this test's own directory,
    /// where there is none, so no foreign notify program installed on the
    /// machine running the test is ever spawned.
    /// </summary>
    private void SetupHermeticSidecar()
        => SetupGlobalConfigDirectory(WorkingDirectory);

    /// <summary>
    /// Feeds one <c>hooks.json</c> event payload to stdin. Every command run
    /// consumes the reader, so a test invoking two events calls this again
    /// before the second.
    /// </summary>
    private void SetupHookPayload()
        => SetupStandardInput(
            $$"""{"session_id":"{{SessionId}}","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""");

    private string NotifyPayload()
        => $$"""{"type":"agent-turn-complete","thread-id":"{{SessionId}}","cwd":{{System.Text.Json.JsonSerializer.Serialize(WorkingDirectory)}}}""";

    /// <summary>
    /// Sends one message to the seeded actor directly against the store, so
    /// the digest has content without a second command run pushing it.
    /// </summary>
    private async Task<MailMessage> SeedMailAsync()
    {
        await SeedAgentAsync("bob");

        var store = new MailStore(
            new TestFileSystem(WorkingDirectory),
            FakeTime,
            new AgentDatabase(),
            new AgentRegistry(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase()));

        return await store.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "bob",
                Subject = "status",
                Body = "please check",
                To = ["maya"]
            },
            TestContext.Current.CancellationToken);
    }
}
