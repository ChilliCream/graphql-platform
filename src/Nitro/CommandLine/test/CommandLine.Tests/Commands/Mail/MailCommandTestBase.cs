using ChilliCream.Nitro.CommandLine.Services.Mail;
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
