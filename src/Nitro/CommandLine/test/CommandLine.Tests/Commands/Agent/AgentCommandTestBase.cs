using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Runs the agent registry commands (register and list) against a real
/// SQLite workspace in a per-test temp directory named "acme".
/// </summary>
public abstract class AgentCommandTestBase : CommandTestBase
{
    private readonly DirectoryInfo _tempRoot;

    protected AgentCommandTestBase(NitroCommandFixture fixture) : base(fixture)
    {
        SetupNoAuthentication();
        SetupActingActor("test-agent");
        DefaultActor = "test-agent";

        _tempRoot = Directory.CreateTempSubdirectory("nitro-agent-registry-tests");
        WorkingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(WorkingDirectory);
        SetupFileSystem(new TestFileSystem(WorkingDirectory));
    }

    protected string WorkingDirectory { get; }

    protected string WorkspaceDirectory
        => AgentWorkspace.GetDirectory(WorkingDirectory);

    protected string DatabasePath
        => AgentWorkspace.GetDatabasePath(WorkspaceDirectory);

    protected async Task SeedAgentAsync(string actor, string role = "")
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agents (name, role, registered_at, started_at, last_seen_at)
            VALUES ($name, $role, $now, $now, $now)
            ON CONFLICT (name) DO UPDATE SET role = excluded.role, last_seen_at = excluded.last_seen_at;
            """;
        command.Parameters.AddWithValue("$name", actor);
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$now", FakeTime.GetUtcNow());

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Seeds the named agent and binds it to a Claude Code session directly on
    /// the unified <c>agents</c> row, mirroring what <c>StartSessionAsync</c>
    /// would write.
    /// </summary>
    protected async Task BindAgentSessionAsync(
        string actor,
        string sessionId,
        string harness = AgentSessionHarness.ClaudeCode)
    {
        await SeedAgentAsync(actor);

        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE agents SET harness = $harness, session_id = $sessionId, "
            + "started_at = $now, last_seen_at = $now WHERE name = $name";
        command.Parameters.AddWithValue("$harness", harness);
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$now", FakeTime.GetUtcNow());
        command.Parameters.AddWithValue("$name", actor);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    protected async Task InitWorkspaceAsync()
    {
        var result = await ExecuteCommandAsync("agent", "init");
        Assert.Equal(0, result.ExitCode);
    }

    /// <summary>
    /// Soft-deletes the named agent directly, bypassing the store, since delete mechanics
    /// are a separate ticket.
    /// </summary>
    protected async Task MarkAgentDeletedAsync(string name)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE agents SET deleted_at = $now WHERE name = $name";
        command.Parameters.AddWithValue("$now", FakeTime.GetUtcNow());
        command.Parameters.AddWithValue("$name", name);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Runs a scalar query against the workspace database and returns the
    /// first column of the first row as a string.
    /// </summary>
    protected async Task<string?> QueryScalarAsync(string sql)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection =
            new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is null or DBNull ? null : result.ToString();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _tempRoot.Delete(recursive: true);
    }
}
