using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers the <c>nitro agent hook venti</c> commands end to end: Venti-shaped payloads on stdin,
/// the Claude-shaped response on stdout, and the Venti rows they write.
/// </summary>
public sealed class VentiHookCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    private const string SessionId = "venti-command-session";

    [Fact]
    public async Task SessionStart_Should_RegisterAVentiAgent_When_ThePayloadHasNullTranscriptPath()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupStandardInput(
            $$"""
            {"session_id":"{{SessionId}}","transcript_path":null,"cwd":{{JsonSerializer.Serialize(WorkingDirectory)}},"hook_event_name":"SessionStart","source":"startup","model":"fake-model"}
            """);

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "venti", "session-start");

        // assert
        var actor = await QueryScalarAsync(
            $"SELECT name FROM agents WHERE harness = 'venti' AND session_id = '{SessionId}'");
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().Replace(actor!, "<actor>").MatchInlineSnapshot(
            """{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"Your Nitro actor name is \u0022<actor>\u0022. Pass this name to the \u0060--actor\u0060 option to act under this actor explicitly."}}""");
    }

    [Fact]
    public async Task AgentNaming_Should_NameTheSessionAfterItsActor_When_TheAgentIsMain()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupStandardInput(
            $$"""
            {"session_id":"{{SessionId}}","transcript_path":null,"cwd":{{JsonSerializer.Serialize(WorkingDirectory)}},"permission_mode":"default","hook_event_name":"AgentNaming","agent_kind":"main"}
            """);

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "venti", "agent-naming");

        // assert
        var actor = await QueryScalarAsync(
            $"SELECT name FROM agents WHERE harness = 'venti' AND session_id = '{SessionId}'");
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().Replace(actor!, "<actor>").MatchInlineSnapshot(
            """{"hookSpecificOutput":{"hookEventName":"AgentNaming","agentName":"<actor>"}}""");
    }

    [Theory]
    [InlineData(",\"agent_kind\":\"subagent\",\"agent_type\":\"default\",\"parent_agent_name\":\"main\"")]
    [InlineData("")]
    [InlineData(",\"agent_kind\":\"Main\"")]
    [InlineData(",\"agent_kind\":1")]
    [InlineData("\"agent_kind\":\"main\"")]
    public async Task AgentNaming_Should_WriteNeutralResponseWithoutWriting_When_ThePayloadNamesNoMainAgent(
        string kindProperties)
    {
        // arrange
        await InitWorkspaceAsync();
        SetupStandardInput(
            $$"""
            {"session_id":"{{SessionId}}","transcript_path":null,"cwd":{{JsonSerializer.Serialize(WorkingDirectory)}},"hook_event_name":"AgentNaming"{{kindProperties}}}
            """);

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "venti", "agent-naming");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agents"));
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task SessionStart_Should_AnnounceTheNamedActor_When_AgentNamingRegisteredTheSession()
    {
        // arrange
        await InitWorkspaceAsync();
        var cwd = JsonSerializer.Serialize(WorkingDirectory);
        SetupStandardInput(
            $$"""
            {"session_id":"{{SessionId}}","transcript_path":null,"cwd":{{cwd}},"hook_event_name":"AgentNaming","agent_kind":"main"}
            """);
        var naming = await ExecuteCommandAsync("agent", "hook", "venti", "agent-naming");
        var actor = JsonDocument.Parse(naming.StdOut).RootElement
            .GetProperty("hookSpecificOutput").GetProperty("agentName").GetString()!;
        SetupStandardInput(
            $$"""
            {"session_id":"{{SessionId}}","transcript_path":null,"cwd":{{cwd}},"hook_event_name":"SessionStart","source":"startup"}
            """);

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "venti", "session-start");

        // assert
        Assert.Equal(actor, await QueryScalarAsync("SELECT group_concat(name) FROM agents"));
        result.StdOut.Trim().Replace(actor, "<actor>").MatchInlineSnapshot(
            """{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"Your Nitro actor name is \u0022<actor>\u0022. Pass this name to the \u0060--actor\u0060 option to act under this actor explicitly."}}""");
    }

    [Fact]
    public async Task Stop_Should_BlockTheTurn_When_UnreadMailIsUndelivered()
    {
        // arrange
        await InitWorkspaceAsync();
        await BindAgentSessionAsync("maya", SessionId, AgentSessionHarness.Venti);
        var message = await SeedMailAsync();
        SetupStandardInput(
            $$"""
            {"session_id":"{{SessionId}}","transcript_path":null,"cwd":{{JsonSerializer.Serialize(WorkingDirectory)}},"hook_event_name":"Stop","stop_hook_active":false}
            """);

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "venti", "stop");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Trim().Replace(message.Id, "<id>").MatchInlineSnapshot(
            """
            {"decision":"block","reason":"Unread nitro mail is waiting; handle it before ending this turn, or ignore this once if it is not actionable right now.\nYou have 1 unread nitro message; 1 shown below as \u0060nitro agent mail read --thread --output json\u0060 prints them. Reply with \u0060nitro agent mail reply --message \u003Cid\u003E --actor maya --body \u0022...\u0022\u0060 or ack with \u0060nitro agent mail ack --message \u003Cid\u003E --actor maya\u0060; anything not shown is in \u0060nitro agent mail inbox --unread --actor maya\u0060.\n{\n  \u0022items\u0022: [\n    {\n      \u0022id\u0022: \u0022<id>\u0022,\n      \u0022threadId\u0022: \u0022<id>\u0022,\n      \u0022inReplyTo\u0022: null,\n      \u0022from\u0022: \u0022ada\u0022,\n      \u0022to\u0022: [\n        \u0022maya\u0022\n      ],\n      \u0022cc\u0022: [],\n      \u0022subject\u0022: \u0022Status\u0022,\n      \u0022body\u0022: \u0022All good.\u0022,\n      \u0022createdAt\u0022: \u00222026-01-01T00:00:00\u002B00:00\u0022,\n      \u0022read\u0022: false,\n      \u0022archived\u0022: false,\n      \u0022takeovers\u0022: []\n    }\n  ]\n}"}
            """);
    }

    [Fact]
    public async Task SessionEnd_Should_EndOnlyTheVentiAgent_When_AClaudeSessionSharesTheSessionId()
    {
        // arrange
        await InitWorkspaceAsync();
        await BindAgentSessionAsync("maya", SessionId, AgentSessionHarness.ClaudeCode);
        await BindAgentSessionAsync("nova", SessionId, AgentSessionHarness.Venti);
        SetupStandardInput(
            $$"""
            {"session_id":"{{SessionId}}","transcript_path":null,"cwd":{{JsonSerializer.Serialize(WorkingDirectory)}},"hook_event_name":"SessionEnd","reason":"prompt_input_exit"}
            """);

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "venti", "session-end");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "maya",
            await QueryScalarAsync(
                "SELECT group_concat(name) FROM agents WHERE ended_at IS NULL AND session_id IS NOT NULL"));
        Assert.Equal(
            "nova",
            await QueryScalarAsync("SELECT group_concat(name) FROM agents WHERE ended_at IS NOT NULL"));
    }

    [Fact]
    public async Task Notification_Should_WriteNeutralResponse_When_TheTypeIsAVentiPermissionPrompt()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupStandardInput(
            $$"""
            {"session_id":"{{SessionId}}","transcript_path":null,"cwd":{{JsonSerializer.Serialize(WorkingDirectory)}},"hook_event_name":"Notification","notification_type":"permission_prompt","message":"Approve Bash?"}
            """);

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "venti", "notification");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agents WHERE harness = 'venti'"));
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task UserPromptSubmit_Should_WriteNeutralResponseWithoutWriting_When_HooksAreSuppressed()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupRawEnvironmentVariable("NITRO_HOOK_SUPPRESS", "1");
        SetupStandardInput(
            $$"""
            {"session_id":"{{SessionId}}","transcript_path":null,"cwd":{{JsonSerializer.Serialize(WorkingDirectory)}},"hook_event_name":"UserPromptSubmit","prompt":"hi"}
            """);

        // act
        var result = await ExecuteCommandAsync("agent", "hook", "venti", "user-prompt-submit");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agents WHERE harness = 'venti'"));
        result.StdOut.Trim().MatchInlineSnapshot("{}");
    }

    [Fact]
    public async Task VentiHelp_Should_ListTheHookEvents_When_HelpIsRequested()
    {
        // arrange
        // act
        var result = await ExecuteCommandAsync("agent", "hook", "venti", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Adapt Venti turn-boundary hook events.

            Usage:
              nitro agent hook venti [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              agent-naming        Adapt Venti's AgentNaming hook: name a new main session after its actor.
              session-start       Adapt Venti's SessionStart hook: upsert this session's presence row.
              user-prompt-submit  Adapt Venti's UserPromptSubmit hook: reset the block budget and inject the unread-mail digest.
              stop                Adapt Venti's Stop hook: block the turn while unread mail is undelivered.
              notification        Adapt Venti's Notification hook: touch last-seen on an idle-prompt notification.
              session-end         Adapt Venti's SessionEnd hook: mark this session's agent row as ended.
            """);
    }

    private async Task<MailMessage> SeedMailAsync()
    {
        await SeedAgentAsync("ada");

        var store = new MailStore(
            new TestFileSystem(WorkingDirectory),
            FakeTime,
            new AgentDatabase(),
            new AgentStore(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase()));

        return await store.SendMessageAsync(
            new MailMessageCreation { Sender = "ada", Subject = "Status", Body = "All good.", To = ["maya"] },
            TestContext.Current.CancellationToken);
    }
}
