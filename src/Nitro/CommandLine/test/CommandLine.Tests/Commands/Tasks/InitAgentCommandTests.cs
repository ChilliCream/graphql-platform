using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

/// <summary>
/// Covers the unified <c>nitro agent init</c> command: fresh init, --force,
/// and migration from a legacy pre-unification <c>.nitro/tasks</c> and/or
/// <c>.nitro/mail</c> workspace.
/// </summary>
public sealed class InitAgentCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    private string LegacyTasksDirectory => Path.Combine(WorkingDirectory, ".nitro", "tasks");

    private string LegacyTasksDatabasePath => Path.Combine(LegacyTasksDirectory, "tasks.db");

    private string LegacyTasksJsonlPath => Path.Combine(LegacyTasksDirectory, "tasks.jsonl");

    private string LegacyMailDatabasePath => Path.Combine(WorkingDirectory, ".nitro", "mail", "mail.db");

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "init", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Initialize an agent workspace in the current directory.

            Usage:
              nitro agent init [options]

            Options:
              --prefix <prefix>  The task ID prefix (defaults to the current directory name)
              --force            Reinitialize an existing agent workspace
              --output <json>    The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help     Show help and usage information

            Example:
              nitro agent init
              nitro agent init --prefix "app"
            """);
    }

    [Fact]
    public async Task EmptyDirectory_InitializesWorkspace()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'acme'.
            """);
        Assert.True(File.Exists(DatabasePath));
        Assert.True(File.Exists(Path.Combine(WorkspaceDirectory, AgentWorkspace.JsonlFileName)));
        Assert.Equal("acme", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'prefix'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsWorkspacePathPrefixAndCounts()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(WorkspaceDirectory, root.GetProperty("path").GetString());
        Assert.Equal("acme", root.GetProperty("prefix").GetString());
        Assert.Equal(0, root.GetProperty("migratedTasks").GetInt32());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("importedCount").ValueKind);
    }

    [Fact]
    public async Task PrefixOption_NormalizesValue()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "init", "--prefix", "My App!");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'myapp'.
            """);
    }

    [Fact]
    public async Task AlreadyInitialized_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        result.AssertError(
            """
            Already initialized at '.nitro/agents'. Use --force to reinitialize.
            """);
    }

    [Fact]
    public async Task AlreadyInitialized_Force_ResetsPrefix()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--force", "--prefix", "core");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'core'.
            """);
        Assert.Equal("core", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'prefix'"));
    }

    [Fact]
    public async Task AlreadyInitialized_Force_RefreshesStaleGitIgnore()
    {
        // arrange
        await InitWorkspaceAsync();
        var gitIgnorePath = Path.Combine(WorkspaceDirectory, AgentWorkspace.GitIgnoreFileName);
        await File.WriteAllTextAsync(
            gitIgnorePath, "*\n!.gitignore\n", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--force");

        // assert
        Assert.Equal(0, result.ExitCode);
        var gitIgnoreText = await File.ReadAllTextAsync(
            gitIgnorePath, TestContext.Current.CancellationToken);
        gitIgnoreText.MatchInlineSnapshot(
            """
            # The agent database is local state; tasks.jsonl is the source of truth in git.
            agents.db
            agents.db-wal
            agents.db-shm
            """);
    }

    [Fact]
    public async Task AlreadyInitialized_Force_NeverTouchesTasksJsonl()
    {
        // arrange
        await InitWorkspaceAsync();
        var jsonlPath = Path.Combine(WorkspaceDirectory, AgentWorkspace.JsonlFileName);
        await File.WriteAllTextAsync(jsonlPath, "sentinel\n", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--force");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "sentinel\n",
            await File.ReadAllTextAsync(jsonlPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LegacyTasksDatabase_MigratesTasksEventsAndChildCounters()
    {
        // arrange: a task-v1 seed with a task, an event, and an advanced
        // child counter, at the pre-unification .nitro/tasks/tasks.db path.
        await SeedLegacyTasksDatabaseAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'legacy'.
            ✓ Migrated 1 task from the legacy '.nitro/tasks' workspace.

            The legacy '.nitro/tasks' workspace has been migrated. Remove it from git with:
              git rm -r .nitro/tasks
              git add .nitro/agents
            """);
        Assert.Equal("1", await QueryScalarAsync(
            "SELECT COUNT(*) FROM events WHERE task_id = 'legacy-1'"));
        Assert.Equal("3", await QueryScalarAsync(
            "SELECT last_child FROM child_counters WHERE parent_id = 'legacy-1'"));
    }

    [Fact]
    public async Task LegacyTasksJsonlOnly_ImportsTasks()
    {
        // arrange: a legacy tasks.jsonl with no database beside it.
        Directory.CreateDirectory(LegacyTasksDirectory);
        await File.WriteAllTextAsync(
            LegacyTasksJsonlPath,
            """
            {"key":"prefix","value":"jsonl"}
            {"id":"jsonl-1","title":"From jsonl","description":"","design":"","acceptanceCriteria":"","notes":"","status":"open","priority":2,"type":"task","createdAt":"2026-01-01T00:00:00+00:00","createdBy":"","updatedAt":"2026-01-01T00:00:00+00:00","closeReason":"","deleteReason":"","labels":[],"dependencies":[],"comments":[]}

            """,
            TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'jsonl'.
            ✓ Imported 1 task from 'tasks.jsonl'.

            The legacy '.nitro/tasks' workspace has been migrated. Remove it from git with:
              git rm -r .nitro/tasks
              git add .nitro/agents
            """);
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM tasks WHERE id = 'jsonl-1'"));
    }

    [Fact]
    public async Task FreshCloneOfNewLayout_BuildsDatabaseWithoutTouchingCommittedFiles()
    {
        // arrange: the unified layout already committed (tasks.jsonl and
        // .gitignore present) but no database, as after a fresh git clone.
        Directory.CreateDirectory(WorkspaceDirectory);
        var jsonlPath = Path.Combine(WorkspaceDirectory, AgentWorkspace.JsonlFileName);
        var gitIgnorePath = Path.Combine(WorkspaceDirectory, AgentWorkspace.GitIgnoreFileName);
        const string committedJsonl =
            """
            {"key":"prefix","value":"cloned"}
            {"id":"cloned-1","title":"From clone","description":"","design":"","acceptanceCriteria":"","notes":"","status":"open","priority":2,"type":"task","createdAt":"2026-01-01T00:00:00+00:00","createdBy":"","updatedAt":"2026-01-01T00:00:00+00:00","closeReason":"","deleteReason":"","labels":[],"dependencies":[],"comments":[]}

            """;
        await File.WriteAllTextAsync(jsonlPath, committedJsonl, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(gitIgnorePath, "custom\n", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'cloned'.
            ✓ Imported 1 task from 'tasks.jsonl'.
            """);
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM tasks WHERE id = 'cloned-1'"));
        Assert.Equal(
            committedJsonl,
            await File.ReadAllTextAsync(jsonlPath, TestContext.Current.CancellationToken));
        Assert.Equal(
            "custom\n",
            await File.ReadAllTextAsync(gitIgnorePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LegacyMailDatabase_ReportedObsoleteAndNotMigrated()
    {
        // arrange
        var legacyMailDirectory = Path.GetDirectoryName(LegacyMailDatabasePath)!;
        Directory.CreateDirectory(legacyMailDirectory);
        await File.WriteAllTextAsync(
            LegacyMailDatabasePath, "not a real db", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'acme'.

            Found a legacy mail workspace at '.nitro/mail'. It was never released, so its data was not migrated. Delete it with:
              rm -rf .nitro/mail
            """);
        Assert.True(File.Exists(LegacyMailDatabasePath));
    }

    [Fact]
    public async Task MalformedLegacyJsonl_LeavesNoWorkspace_RetryableAfterward()
    {
        // arrange
        Directory.CreateDirectory(LegacyTasksDirectory);
        await File.WriteAllTextAsync(
            LegacyTasksJsonlPath, "not json\n", TestContext.Current.CancellationToken);

        // act
        var firstResult = await ExecuteCommandAsync("agent", "init");

        // assert: the failed attempt left no database behind, so fixing the
        // source and retrying succeeds.
        Assert.Equal(1, firstResult.ExitCode);
        Assert.False(File.Exists(DatabasePath));

        await File.WriteAllTextAsync(LegacyTasksJsonlPath, "", TestContext.Current.CancellationToken);
        var secondResult = await ExecuteCommandAsync("agent", "init");
        Assert.Equal(0, secondResult.ExitCode);
    }

    [Fact]
    public async Task TasksAndMail_ShareTheSameUnifiedWorkspace()
    {
        // arrange
        await InitWorkspaceAsync();
        var taskId = await CreateTaskAsync("Ship the unified workspace");

        // act
        var registerResult = await ExecuteCommandAsync("agent", "mail", "register", "--actor", "bob");
        var sendResult = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "Merged.");

        // assert
        Assert.Equal(0, registerResult.ExitCode);
        Assert.Equal(0, sendResult.ExitCode);
        Assert.Equal("2", await QueryScalarAsync("PRAGMA user_version;"));
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM messages"));

        var listResult = await ExecuteCommandAsync("agent", "tasks", "list");
        Assert.Contains(taskId, listResult.StdOut);
    }

    private async Task SeedLegacyTasksDatabaseAsync()
    {
        Directory.CreateDirectory(LegacyTasksDirectory);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection =
            new SqliteConnection($"Data Source={LegacyTasksDatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = TaskStoreSchema.Create;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA user_version = 1;";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                INSERT INTO tasks (id, title, status, task_type, created_at, updated_at)
                VALUES ('legacy-1', 'Legacy task', 'open', 'task', '2026-01-01T00:00:00+00:00', '2026-01-01T00:00:00+00:00');
                INSERT INTO events (task_id, event_type, created_at)
                VALUES ('legacy-1', 'created', '2026-01-01T00:00:00+00:00');
                INSERT INTO child_counters (parent_id, last_child) VALUES ('legacy-1', 3);
                INSERT INTO config (key, value) VALUES ('prefix', 'legacy');
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
