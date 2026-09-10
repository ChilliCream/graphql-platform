using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using ChilliCream.Nitro.CommandLine.Tests.Hook;
using Microsoft.Data.Sqlite;

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
        => new(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase(), CreateRegistry());

    /// <summary>
    /// Creates an <see cref="IMailStore"/> like <see cref="CreateStore"/>,
    /// additionally wired with the instance id and global config directory
    /// providers <see cref="MailWakePolicy.Enqueue"/> requires, pinned to
    /// <paramref name="instanceId"/> so a directly-enqueued generation lines
    /// up with a command run under the matching <see cref="CommandTestBase.SetupInstanceId"/>.
    /// </summary>
    internal MailStore CreateWakeStore(string instanceId)
        => new(
            new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase(), CreateRegistry(),
            new FixedInstanceIdProvider(instanceId), new FixedGlobalConfigDirectoryProvider(WorkingDirectory));

    /// <summary>
    /// Creates an <see cref="IAgentRegistry"/> bound to this test's workspace
    /// and clock, for seeding agents without going through the CLI.
    /// </summary>
    internal AgentRegistry CreateRegistry()
        => new(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase());

    /// <summary>
    /// Creates an <see cref="IAgentSessionRegistry"/> bound to this test's
    /// workspace, clock, and instance id, for resolving live participants
    /// directly without going through the CLI: every row it acts on must
    /// already exist, seeded directly against the database.
    /// </summary>
    internal AgentSessionRegistry CreateSessions(string host)
        => new(
            new TestFileSystem(WorkingDirectory),
            FakeTime,
            new AgentDatabase(),
            CreateRegistry(),
            new FixedInstanceIdProvider(host),
            new FixedGlobalConfigDirectoryProvider(WorkingDirectory));

    /// <summary>
    /// Registers an agent directly against the registry.
    /// </summary>
    internal Task<AgentRecord> SeedAgentAsync(string name, string role = "")
        => CreateRegistry().RegisterAsync(name, role, client: "", TestContext.Current.CancellationToken);

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
    /// Seeds an alive, explicitly-claimed <c>codex-thread</c> session for
    /// <paramref name="agentName"/> directly against the workspace database,
    /// on the host id <see cref="CommandTestBase.SetupInstanceId"/> was pointed at (a test
    /// calling this must call that first, so the notifier's own host
    /// resolution matches this row). Used to exercise auto-ping through the
    /// CLI without a live harness process.
    /// </summary>
    private protected Task SeedAliveCodexThreadSessionAsync(string agentName, string threadId, string host)
        => SeedAliveSessionAsync(
            "session-1", agentName, role: "", host,
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: threadId);

    /// <summary>
    /// Configures successful foreground wake delivery for each named agent.
    /// Use this in command tests whose primary concern requires a successful
    /// send but is unrelated to the wake transport itself.
    /// </summary>
    private protected async Task SetupSuccessfulWakeAsync(string host, params string[] agentNames)
    {
        SetupInstanceId(host);
        SetupCodexQueueClient(new FakeCodexQueueClient());

        foreach (var agentName in agentNames)
        {
            await SeedAliveSessionAsync(
                $"session-{agentName}", agentName, role: "", host,
                endpointKind: AgentSessionEndpointKind.CodexThread,
                endpointAddr: $"thread-{agentName}");
        }
    }

    /// <summary>
    /// Seeds an alive <c>agent_sessions</c> row directly against the
    /// workspace database, on the host id <see cref="CommandTestBase.SetupInstanceId"/> was
    /// pointed at (a test calling this must call that first, so the
    /// notifier's own host resolution matches this row). A null
    /// <paramref name="agentName"/> seeds an unbound row. Used to exercise
    /// role-targeted mail discovery and auto-ping through the CLI without a
    /// live harness process.
    /// </summary>
    private protected async Task SeedAliveSessionAsync(
        string sessionId,
        string? agentName,
        string role,
        string host,
        string endpointKind = AgentSessionEndpointKind.None,
        string endpointAddr = "")
    {
        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        if (agentName is not null)
        {
            await using var agentCommand = connection.CreateCommand();
            agentCommand.CommandText =
                "INSERT OR IGNORE INTO agents (name, registered_at, last_seen_at) "
                + "VALUES ($name, $now, $now);";
            agentCommand.Parameters.AddWithValue("$name", agentName);
            agentCommand.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);
            await agentCommand.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, role, host,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'codex', $sessionId, $agentName, $bindingKind, $role, $host,
                '/work', '/work/.nitro/agents', $endpointKind, $endpointAddr, $now, $now
            );
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$agentName", (object?)agentName ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "$bindingKind", agentName is null ? AgentSessionBindingKind.None : AgentSessionBindingKind.Explicit);
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$host", host);
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
    /// Runs a non-query statement against the workspace database, for
    /// mutating a seeded row mid-test (e.g. simulating a role change or a
    /// session ending between discovery and send).
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
