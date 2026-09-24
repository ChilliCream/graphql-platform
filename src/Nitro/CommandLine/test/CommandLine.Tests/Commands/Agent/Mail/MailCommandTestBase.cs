using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Agents;
using ChilliCream.Nitro.CommandLine.Tests.Hook;
using Microsoft.Data.Sqlite;
using TestFileSystem = ChilliCream.Nitro.CommandLine.Tests.Hook.TestFileSystem;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Mail;

/// <summary>
/// Runs mail commands against a real SQLite workspace in a per-test temp
/// directory named "acme".
/// </summary>
public abstract class MailCommandTestBase : CommandTestBase
{
    private readonly DirectoryInfo _tempRoot;

    protected MailCommandTestBase(NitroCommandFixture fixture) : base(fixture)
    {
        SetupNoAuthentication();
        SetupActingActor("test-agent");
        DefaultActor = "test-agent";

        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-tests");
        WorkingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(WorkingDirectory);
        SetupFileSystem(new TestFileSystem(WorkingDirectory));
    }

    protected string WorkingDirectory { get; }

    protected string WorkspaceDirectory
        => AgentWorkspace.GetDirectory(WorkingDirectory);

    protected string DatabasePath
        => AgentWorkspace.GetDatabasePath(WorkspaceDirectory);

    protected async Task InitWorkspaceAsync()
    {
        var result = await ExecuteCommandAsync("agent", "init");
        Assert.Equal(0, result.ExitCode);
    }

    /// <summary>
    /// Creates an <see cref="IMailStore"/> bound to this test's workspace and
    /// clock, for seeding data without going through the CLI.
    /// </summary>
    internal MailStore CreateStore()
        => new(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase(), CreateAgentStore());

    /// <summary>
    /// Creates an <see cref="IAgentStore"/> bound to this test's workspace and clock.
    /// </summary>
    internal AgentStore CreateAgentStore()
        => new(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase());

    /// <summary>
    /// Registers an agent directly against the unified <c>agents</c> table, bypassing
    /// the store's allocated-name contract so tests can seed a deterministic name.
    /// </summary>
    internal async Task SeedAgentAsync(string name, string role = "")
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
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$now", FakeTime.GetUtcNow());

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Marks the named agent as deleted.
    /// </summary>
    internal Task MarkAgentDeletedAsync(string name)
        => ExecuteAsync($"UPDATE agents SET deleted_at = '{FakeTime.GetUtcNow():O}' WHERE name = '{name}'");

    /// <summary>
    /// Sends a message directly against the store, starting a new thread.
    /// The sender and every recipient must already be registered.
    /// </summary>
    internal Task<MailMessage> SeedMessageAsync(
        string sender,
        string subject,
        IReadOnlyList<string> to,
        IReadOnlyList<string>? cc = null,
        string body = "body")
        => CreateStore().SendMessageAsync(
            new MailMessageCreation
            {
                Sender = sender,
                Subject = subject,
                Body = body,
                To = to,
                Cc = cc ?? []
            },
            TestContext.Current.CancellationToken);

    /// <summary>
    /// Configures successful foreground wake delivery for each named agent.
    /// </summary>
    private protected async Task<FakeCodexQueueClient> SetupSuccessfulWakeAsync(
        params string[] agentNames)
    {
        var queueClient = new FakeCodexQueueClient();
        SetupCodexQueueClient(queueClient);

        foreach (var agentName in agentNames)
        {
            await SeedAliveSessionAsync(
                $"session-{agentName}", agentName, role: "",
                endpointKind: AgentSessionEndpointKind.CodexThread,
                endpointAddr: $"thread-{agentName}");
        }

        return queueClient;
    }

    private protected MailNudge CreateMailNudge(string host, FakeCodexQueueClient queueClient)
    {
        var fileSystem = new TestFileSystem(WorkingDirectory);
        var database = new AgentDatabase();

        return new MailNudge(
            CreateAgentStore(),
            CreateStore(),
            new AgentDeliveryLedger(fileSystem, database),
            new FakeClaudePeerClient(),
            queueClient,
            FakeTime);
    }

    private protected static (string ThreadId, string Id, string Body) ReadDigestCall(
        (string ThreadId, string Message) call)
    {
        using var document = System.Text.Json.JsonDocument.Parse(
            call.Message[(call.Message.IndexOf('\n') + 1)..]);
        var item = document.RootElement.GetProperty("items")[0];

        return (
            call.ThreadId,
            item.GetProperty("id").GetString()!,
            item.GetProperty("body").GetString()!);
    }

    private protected static bool ReadDigestReadFlag((string ThreadId, string Message) call)
    {
        using var document = System.Text.Json.JsonDocument.Parse(
            call.Message[(call.Message.IndexOf('\n') + 1)..]);

        return document.RootElement.GetProperty("items")[0].GetProperty("read").GetBoolean();
    }

    /// <summary>
    /// Seeds a fresh codex session directly on the unified <c>agents</c> row for
    /// <paramref name="agentName"/>, mirroring what <c>StartSessionAsync</c> would write.
    /// </summary>
    private protected async Task SeedAliveSessionAsync(
        string sessionId,
        string agentName,
        string role,
        string endpointKind = AgentSessionEndpointKind.None,
        string endpointAddr = "")
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agents (
                name, role, harness, session_id, endpoint_kind, endpoint_addr,
                registered_at, started_at, last_seen_at
            )
            VALUES ($name, $role, 'codex', $sessionId, $endpointKind, $endpointAddr, $now, $now, $now)
            ON CONFLICT (name) DO UPDATE SET
                role = excluded.role,
                harness = excluded.harness,
                session_id = excluded.session_id,
                endpoint_kind = excluded.endpoint_kind,
                endpoint_addr = excluded.endpoint_addr,
                last_seen_at = excluded.last_seen_at;
            """;
        command.Parameters.AddWithValue("$name", agentName);
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$endpointKind", endpointKind);
        command.Parameters.AddWithValue("$endpointAddr", endpointAddr);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
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

    /// <summary>
    /// Runs a non-query SQL statement against the workspace database.
    /// </summary>
    protected async Task ExecuteAsync(string sql)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection =
            new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _tempRoot.Delete(recursive: true);
    }
}
