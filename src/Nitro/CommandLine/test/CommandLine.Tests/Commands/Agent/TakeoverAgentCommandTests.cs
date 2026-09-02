using ChilliCream.Nitro.CommandLine.Tests.Commands;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class TakeoverAgentCommandTests(NitroCommandFixture fixture)
    : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_Should_DescribeTakeoverOptionsAndExamples()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "takeover", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Take over another actor's mail and tasks.

            Usage:
              nitro agent takeover [options]

            Options:
              --from <from> (REQUIRED)    The actor whose mail and tasks to take over
              --actor <actor> (REQUIRED)  The actor taking over the mail and tasks
              --force                     Take over even when the source actor still has a live session
              --reason <reason>           The reason recorded for the takeover
              --output <json>             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help              Show help and usage information

            Example:
              nitro agent takeover --from "maya" --actor "nora"
              nitro agent takeover --from "maya" --actor "nora" --force --reason "session ended"
            """);
    }

    [Fact]
    public async Task Takeover_Should_MoveMailAndTasks_InheritRole_AndWriteHumanOutput()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya", "planner");
        await SeedAgentAsync("nora");
        await SeedAgentAsync("sender");
        await SendMailAsync("sender", "maya", "received");
        await SendMailAsync("maya", "sender", "sent");
        var taskId = await CreateTaskAsync("maya");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "takeover", "--from", "maya", "--actor", "nora", "--reason", "handoff");

        // assert
        result.AssertSuccess(
            $"✓ 'nora' took over from 'maya': role 'planner', 2 messages, 1 tasks ({taskId}).");
        var state = string.Join(
            "|",
            await QueryScalarAsync("SELECT role FROM agents WHERE name = 'nora'"),
            await QueryScalarAsync($"SELECT assignee FROM tasks WHERE id = '{taskId}'"),
            await QueryScalarAsync($"SELECT text FROM comments WHERE task_id = '{taskId}'"));
        state.MatchInlineSnapshot("planner|nora|Taken over from 'maya' by 'nora'.");
    }

    [Fact]
    public async Task Takeover_Should_ReturnExactJsonShape()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya", "planner");
        await SeedAgentAsync("nora");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "takeover", "--from", "maya", "--actor", "nora");

        // assert
        var id = await QueryScalarAsync("SELECT id FROM agent_takeovers");
        result.StdOut.Replace(id!, "<id>").MatchInlineSnapshot(
            """
            {
              "id": "<id>",
              "from": "maya",
              "to": "nora",
              "role": "planner",
              "recipientsMoved": 0,
              "sendersMoved": 0,
              "tasks": []
            }
            """);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Takeover_Should_RefuseLiveSourceSession_UnlessForced()
    {
        // arrange
        SetupInstanceId("local-instance");
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya");
        await SeedAgentAsync("nora");
        await InsertAliveSessionRowAsync("local-instance", "session-1", "maya");

        // act
        var refused = await ExecuteCommandAsync(
            "agent", "takeover", "--from", "maya", "--actor", "nora");
        var forced = await ExecuteCommandAsync(
            "agent", "takeover", "--from", "maya", "--actor", "nora", "--force");

        // assert
        refused.AssertError(
            "Actor 'maya' still has a live session; pass --force to take over anyway.");
        forced.AssertSuccess("✓ 'nora' took over from 'maya': role '', 0 messages, no tasks.");
        Assert.Equal("1", await QueryScalarAsync("SELECT forced FROM agent_takeovers"));
    }

    [Theory]
    [InlineData("maya", "maya", "The source and target actors must be different.")]
    [InlineData("unknown", "nora", "Unknown actor 'unknown'. Run `nitro agent list` to see the actors this workspace knows.")]
    [InlineData("maya", "unknown", "Unknown actor 'unknown'. Run `nitro agent list` to see the actors this workspace knows.")]
    public async Task Takeover_Should_RejectEqualOrUnknownActors(
        string from,
        string to,
        string expected)
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya");
        await SeedAgentAsync("nora");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "takeover", "--from", from, "--actor", to);

        // assert
        result.AssertError(expected);
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_takeovers"));
    }

    [Theory]
    [InlineData("", "planner")]
    [InlineData("reviewer", "reviewer")]
    public async Task Takeover_Should_OnlyInheritRole_When_TargetRoleIsBlank(
        string targetRole,
        string expectedRole)
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya", "planner");
        await SeedAgentAsync("nora", targetRole);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "takeover", "--from", "maya", "--actor", "nora");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(expectedRole, await QueryScalarAsync("SELECT role FROM agents WHERE name = 'nora'"));
        Assert.Equal(expectedRole, await QueryScalarAsync("SELECT role FROM agent_takeovers"));
    }

    [Fact]
    public async Task Takeover_Should_RecordItemsAndAHeader_OnEveryRun()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya", "planner");
        await SeedAgentAsync("nora");
        await SeedAgentAsync("sender");
        await SendMailAsync("sender", "maya", "received");
        await SendMailAsync("maya", "sender", "sent");
        await CreateTaskAsync("maya");

        // act
        await ExecuteCommandAsync(
            "agent", "takeover", "--from", "maya", "--actor", "nora", "--reason", "handoff");
        var repeated = await ExecuteCommandAsync(
            "agent", "takeover", "--from", "maya", "--actor", "nora");

        // assert
        repeated.AssertSuccess("✓ 'nora' took over from 'maya': role 'planner', 0 messages, no tasks.");
        var ledger = string.Join(
            "|",
            await QueryScalarAsync("SELECT COUNT(*) FROM agent_takeovers"),
            await QueryScalarAsync("SELECT COUNT(*) FROM agent_takeover_items"),
            await QueryScalarAsync(
                "SELECT group_concat(kind || ':' || count, ',') FROM "
                + "(SELECT kind, COUNT(*) AS count FROM agent_takeover_items GROUP BY kind ORDER BY kind)"),
            await QueryScalarAsync(
                "SELECT COUNT(*) FROM agent_takeover_items WHERE takeover_id = "
                + "(SELECT id FROM agent_takeovers WHERE reason IS NULL)"),
            await QueryScalarAsync(
                "SELECT from_actor || ':' || to_actor || ':' || actor || ':' || role || ':' || reason "
                + "FROM agent_takeovers WHERE reason IS NOT NULL"));
        ledger.MatchInlineSnapshot(
            "2|3|message_recipient:1,message_sender:1,task:1|0|maya:nora:nora:planner:handoff");
    }

    private async Task SendMailAsync(string from, string to, string subject)
    {
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send",
            "--to", to,
            "--subject", subject,
            "--body", "body",
            "--actor", from);
        Assert.Equal(0, result.ExitCode);
    }

    private async Task<string> CreateTaskAsync(string assignee)
    {
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "create", "Takeover task",
            "--assignee", assignee,
            "--actor", "sender");
        Assert.Equal(0, result.ExitCode);

        return (await QueryScalarAsync("SELECT id FROM tasks WHERE title = 'Takeover task'"))!;
    }
}
