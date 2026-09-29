using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Tests that <see cref="AgentRow.ReadFrom"/> round-trips every column of the
/// unified <c>agents</c> table.
/// </summary>
public sealed class AgentRowTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly AgentDatabase _database;

    public AgentRowTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-agent-row-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _database = new AgentDatabase();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task ReadFrom_Should_RoundTripEveryColumn_When_AllOptionalValuesArePresent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO agents (
                name, role, harness, harness_version, session_id, cwd, workspace_path,
                registered_at, started_at, last_seen_at, ended_at, deleted_at,
                endpoint_kind, endpoint_addr, endpoint_secret, block_budget_used,
                last_ping_at, last_ping_attempt, last_ping_result, last_ping_detail,
                announcement_pending, idle_push_armed
            ) VALUES (
                'maya', 'backend', 'claude-code', '1.2.3', 'session-full', '/tmp/work',
                '/tmp/work/.nitro/agents', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:01+00:00',
                '2026-01-10T12:00:02+00:00', '2026-01-10T12:00:03+00:00', '2026-01-10T12:00:04+00:00',
                'claude-peer', 'peer-a', 'secret-a', 2,
                '2026-01-10T12:00:05+00:00', 'attempt-1', 'ok', 'all good',
                1, 1
            );
            """,
            cancellationToken);

        // act
        var row = await ReadRowAsync(connection, "maya", cancellationToken);

        // assert
        row.MatchInlineSnapshot(
            """
            {
              "Name": "maya",
              "Role": "backend",
              "Harness": "claude-code",
              "HarnessVersion": "1.2.3",
              "SessionId": "session-full",
              "Cwd": "/tmp/work",
              "WorkspacePath": "/tmp/work/.nitro/agents",
              "RegisteredAt": "2026-01-10T12:00:00+00:00",
              "StartedAt": "2026-01-10T12:00:01+00:00",
              "LastSeenAt": "2026-01-10T12:00:02+00:00",
              "EndedAt": "2026-01-10T12:00:03+00:00",
              "DeletedAt": "2026-01-10T12:00:04+00:00",
              "EndpointKind": "claude-peer",
              "EndpointAddr": "peer-a",
              "EndpointSecret": "secret-a",
              "BlockBudgetUsed": 2,
              "LastPingAt": "2026-01-10T12:00:05+00:00",
              "LastPingAttempt": "attempt-1",
              "LastPingResult": "ok",
              "LastPingDetail": "all good",
              "AnnouncementPending": true,
              "IdlePushArmed": true,
              "IsDeleted": true
            }
            """);
    }

    [Fact]
    public async Task ReadFrom_Should_RoundTripEveryColumn_When_AllOptionalValuesAreNull()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await _database.InitializeAsync(_workspaceDirectory, cancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO agents (name, registered_at, started_at, last_seen_at)
            VALUES ('codex', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');
            """,
            cancellationToken);

        // act
        var row = await ReadRowAsync(connection, "codex", cancellationToken);

        // assert
        row.MatchInlineSnapshot(
            """
            {
              "Name": "codex",
              "Role": "",
              "Harness": null,
              "HarnessVersion": "",
              "SessionId": null,
              "Cwd": "",
              "WorkspacePath": "",
              "RegisteredAt": "2026-01-10T12:00:00+00:00",
              "StartedAt": "2026-01-10T12:00:00+00:00",
              "LastSeenAt": "2026-01-10T12:00:00+00:00",
              "EndedAt": null,
              "DeletedAt": null,
              "EndpointKind": "none",
              "EndpointAddr": "",
              "EndpointSecret": null,
              "BlockBudgetUsed": 0,
              "LastPingAt": null,
              "LastPingAttempt": null,
              "LastPingResult": null,
              "LastPingDetail": null,
              "AnnouncementPending": false,
              "IdlePushArmed": false,
              "IsDeleted": false
            }
            """);
    }

    private static async Task<AgentRow> ReadRowAsync(
        SqliteConnection connection, string name, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {AgentRow.Columns} FROM agents WHERE name = @name";
        command.Parameters.AddWithValue("@name", name);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return AgentRow.ReadFrom(reader);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
