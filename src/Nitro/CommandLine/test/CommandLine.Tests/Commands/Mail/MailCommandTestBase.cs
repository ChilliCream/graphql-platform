using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

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
        SetupEnvironmentVariable("MAIL_ACTOR", "test-agent");

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
    /// Creates an <see cref="IAgentRegistry"/> bound to this test's workspace
    /// and clock, for seeding agents without going through the CLI.
    /// </summary>
    internal AgentRegistry CreateRegistry()
        => new(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase());

    /// <summary>
    /// Creates an <see cref="IAgentSessionRegistry"/> bound to this test's
    /// workspace, clock, and instance id, for resolving live participants
    /// directly without going through the CLI. No ancestor process is ever
    /// found: every row it acts on must already exist, seeded directly
    /// against the database.
    /// </summary>
    internal AgentSessionRegistry CreateSessions(string host)
        => new(
            new TestFileSystem(WorkingDirectory),
            FakeTime,
            new AgentDatabase(),
            CreateRegistry(),
            new FixedInstanceIdProvider(host),
            new FixedGlobalConfigDirectoryProvider(WorkingDirectory),
            new ProcessInfoProvider(),
            new FixedClaudeAncestorSessionResolver(null));

    /// <summary>
    /// Registers an agent directly against the registry.
    /// </summary>
    internal Task<AgentRecord> SeedAgentAsync(string name)
        => CreateRegistry().RegisterAsync(name, role: "", client: "", TestContext.Current.CancellationToken);

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
    /// on the host id <see cref="SetupInstanceId"/> was pointed at (a test
    /// calling this must call that first, so the notifier's own host
    /// resolution matches this row). Used to exercise auto-ping through the
    /// CLI without a live harness process.
    /// </summary>
    private protected Task SeedAliveCodexThreadSessionAsync(string agentName, string threadId, string host)
        => SeedAliveSessionAsync(
            "session-1", agentName, role: "", host,
            endpointKind: AgentSessionEndpointKind.CodexThread, endpointAddr: threadId);

    /// <summary>
    /// Seeds an alive <c>agent_sessions</c> row directly against the
    /// workspace database, on the host id <see cref="SetupInstanceId"/> was
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
        using var process = Process.GetCurrentProcess();
        var pid = process.Id;

        // A genuine DateTimeOffset, not a bare DateTime: TryClaimPingCooldownAsync
        // matches proc_start with a raw SQL string equality against the exact
        // text a DateTimeOffset-typed Dapper parameter serializes, which is
        // not byte-identical to how Microsoft.Data.Sqlite serializes a bare
        // DateTime value.
        var procStart = new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, role, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'codex', $sessionId, $agentName, $bindingKind, $role, $host, $pid, $procStart,
                '/work', '/work/.nitro/agents', $endpointKind, $endpointAddr, $now, $now
            );
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$agentName", (object?)agentName ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "$bindingKind", agentName is null ? AgentSessionBindingKind.None : AgentSessionBindingKind.Explicit);
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$host", host);
        command.Parameters.AddWithValue("$pid", pid);
        command.Parameters.AddWithValue("$procStart", procStart);
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

/// <summary>
/// Reports every launch as failed, without spawning anything - proves the
/// notifier's spawn-failure recording without a real detached process.
/// </summary>
internal sealed class FailingPingWorkerLauncher : IPingWorkerLauncher
{
    public bool TryLaunch(LaunchDescriptor descriptor, IReadOnlyList<string> workerArgs) => false;
}

/// <summary>
/// Records every launch it is asked to perform, without spawning anything -
/// proves a suppressed notify path never reaches the launcher at all.
/// </summary>
internal sealed class RecordingPingWorkerLauncher : IPingWorkerLauncher
{
    public List<IReadOnlyList<string>> Calls { get; } = [];

    public bool TryLaunch(LaunchDescriptor descriptor, IReadOnlyList<string> workerArgs)
    {
        Calls.Add(workerArgs);
        return false;
    }
}
