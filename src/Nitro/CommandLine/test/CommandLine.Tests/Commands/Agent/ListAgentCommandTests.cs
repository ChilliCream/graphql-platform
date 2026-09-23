using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers <c>agent list</c> against the unified agents table: role filtering, board-style
/// ordering, and the Name/Role/Harness/Started/Last Seen/Online columns.
/// </summary>
public sealed class ListAgentCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Lists the agents in the workspace.

            Usage:
              nitro agent list [options]

            Options:
              --role <role>    The actor role, normalized lowercase. Known roles: orchestrator, planner, implementer, reviewer, researcher; any other value is accepted.
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent list
              nitro agent list --role "orchestrator"
            """);
    }

    [Fact]
    public async Task Execute_Should_PrintNoActors_When_NoAgentsExist()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        result.AssertSuccess("No actors.");
    }

    [Fact]
    public async Task Execute_Should_FilterByRole_When_RoleIsGiven()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya", "orchestrator");
        await SeedAgentAsync("nova", "planner");

        // act
        var result = await ExecuteCommandAsync("agent", "list", "--role", "orchestrator");

        // assert
        var line = Assert.Single(result.StdOut.Trim().Split('\n'));
        Assert.StartsWith("maya", line);
    }

    [Fact]
    public async Task Execute_Should_PrintTheBoardColumns_When_AgentsAreInEveryState()
    {
        // arrange
        // Online, login-only, ended and deleted agents; the deleted one must not appear.
        await InitWorkspaceAsync();
        await SeedBoardScenarioAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        // Online first, then unreachable, then offline; the deleted agent is absent.
        result.AssertSuccess(
            """
            maya  orchestrator  Claude Code  30m  10m  yes
            nova  -             -            30m  10m  no
            ada   researcher    Codex        30m  10m  no
            """);
    }

    [Fact]
    public async Task JsonOutput_Should_PrintTheBoardColumns_When_AgentsAreInEveryState()
    {
        // arrange
        // Same board scenario as the human-readable rendering.
        await InitWorkspaceAsync();
        await SeedBoardScenarioAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "items": [
                {
                  "name": "maya",
                  "role": "orchestrator",
                  "harness": "claude-code",
                  "startedAt": "2026-01-01T00:00:00+00:00",
                  "lastSeenAt": "2026-01-01T00:20:00+00:00",
                  "online": true
                },
                {
                  "name": "nova",
                  "role": "",
                  "harness": null,
                  "startedAt": "2026-01-01T00:00:00+00:00",
                  "lastSeenAt": "2026-01-01T00:20:00+00:00",
                  "online": false
                },
                {
                  "name": "ada",
                  "role": "researcher",
                  "harness": "codex",
                  "startedAt": "2026-01-01T00:00:00+00:00",
                  "lastSeenAt": "2026-01-01T00:20:00+00:00",
                  "online": false
                }
              ]
            }
            """);
    }

    /// <summary>
    /// Seeds one online Claude Code hook agent ("maya"), one login-only agent with no
    /// harness or session ("nova", unreachable since it has no endpoint), one Codex agent
    /// whose session already ended ("ada", offline regardless of its endpoint), and one
    /// deleted agent ("zoe") that <c>agent list</c> must never show. All four start at the
    /// same time and are last seen 20 minutes later; the command runs 30 minutes after they
    /// started, so "started" reads 30m and "last seen" reads 10m for every visible row.
    /// </summary>
    private async Task SeedBoardScenarioAsync()
    {
        var startedAt = FakeTime.GetUtcNow();
        var lastSeenAt = startedAt.AddMinutes(20);

        await InsertAgentRowAsync(
            "maya",
            role: "orchestrator",
            harness: AgentSessionHarness.ClaudeCode,
            sessionId: "session-maya",
            endpointKind: AgentSessionEndpointKind.ClaudePeer,
            startedAt: startedAt,
            lastSeenAt: lastSeenAt);

        await InsertAgentRowAsync(
            "nova",
            startedAt: startedAt,
            lastSeenAt: lastSeenAt);

        await InsertAgentRowAsync(
            "ada",
            role: "researcher",
            harness: AgentSessionHarness.Codex,
            sessionId: "session-ada",
            endpointKind: AgentSessionEndpointKind.CodexThread,
            startedAt: startedAt,
            lastSeenAt: lastSeenAt,
            endedAt: lastSeenAt);

        await InsertAgentRowAsync(
            "zoe",
            startedAt: startedAt,
            lastSeenAt: lastSeenAt,
            deletedAt: lastSeenAt);

        FakeTime.Advance(TimeSpan.FromMinutes(30));
    }

    /// <summary>
    /// Inserts one row directly into the unified <c>agents</c> table, bypassing
    /// <see cref="IAgentStore"/> so the row's name, timestamps, and state are fully
    /// controlled by the caller instead of coming from the actor name pool.
    /// </summary>
    private async Task InsertAgentRowAsync(
        string name,
        string role = "",
        string? harness = null,
        string? sessionId = null,
        string endpointKind = AgentSessionEndpointKind.None,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? lastSeenAt = null,
        DateTimeOffset? endedAt = null,
        DateTimeOffset? deletedAt = null)
    {
        var now = FakeTime.GetUtcNow();

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agents (
                name, role, harness, session_id, endpoint_kind,
                registered_at, started_at, last_seen_at, ended_at, deleted_at
            ) VALUES (
                $name, $role, $harness, $sessionId, $endpointKind,
                $now, $startedAt, $lastSeenAt, $endedAt, $deletedAt
            );
            """;
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$harness", (object?)harness ?? DBNull.Value);
        command.Parameters.AddWithValue("$sessionId", (object?)sessionId ?? DBNull.Value);
        command.Parameters.AddWithValue("$endpointKind", endpointKind);
        command.Parameters.AddWithValue("$now", now);
        command.Parameters.AddWithValue("$startedAt", startedAt ?? now);
        command.Parameters.AddWithValue("$lastSeenAt", lastSeenAt ?? now);
        command.Parameters.AddWithValue("$endedAt", (object?)endedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("$deletedAt", (object?)deletedAt ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
