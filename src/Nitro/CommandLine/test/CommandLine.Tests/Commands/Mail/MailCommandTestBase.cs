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
    private protected async Task SeedAliveCodexThreadSessionAsync(string agentName, string threadId, string host)
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
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'codex', 'session-1', $agentName, 'explicit', $host, $pid, $procStart,
                '/work', '/work/.nitro/agents', 'codex-thread', $threadId, $now, $now
            );
            """;
        command.Parameters.AddWithValue("$agentName", agentName);
        command.Parameters.AddWithValue("$host", host);
        command.Parameters.AddWithValue("$pid", pid);
        command.Parameters.AddWithValue("$procStart", procStart);
        command.Parameters.AddWithValue("$threadId", threadId);
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
