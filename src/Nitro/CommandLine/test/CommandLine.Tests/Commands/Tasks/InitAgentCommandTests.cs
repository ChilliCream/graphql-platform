using ChilliCream.Nitro.CommandLine.Services.Mail;
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
              --migrate          Move an existing .nitro/agents workspace into the repository's .git/nitro directory
              --output <json>    The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help     Show help and usage information

            Example:
              nitro agent init
              nitro agent init --prefix "app"
              nitro agent init --migrate
            """);
    }

    private string GitWorkspaceDirectory => Path.Combine(WorkingDirectory, ".git", "nitro");

    private string GitDatabasePath => Path.Combine(GitWorkspaceDirectory, "agents.db");

    [Fact]
    public async Task GitRepository_FreshInit_CreatesWorkspaceInGitDirectory()
    {
        // arrange
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert: the workspace lives inside .git, no .nitro directory and
        // no .gitignore are created.
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.git/nitro'.
            ✓ Task ID prefix set to 'acme'.
            """);
        Assert.True(File.Exists(GitDatabasePath));
        Assert.False(Directory.Exists(Path.Combine(WorkingDirectory, ".nitro")));
        Assert.False(File.Exists(Path.Combine(GitWorkspaceDirectory, AgentWorkspace.GitIgnoreFileName)));
    }

    [Fact]
    public async Task GitRepository_FreshInitFromLinkedWorktree_UsesCommonGitDirectory()
    {
        // arrange: a .git pointer file as a linked worktree has, naming a
        // gitdir under the main checkout's .git with a commondir redirect.
        var mainGitDirectory = Path.Combine(WorkingDirectory, "main", ".git");
        var worktreeGitDirectory = Path.Combine(mainGitDirectory, "worktrees", "wt");
        Directory.CreateDirectory(worktreeGitDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(worktreeGitDirectory, "commondir"),
            "../..\n",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(WorkingDirectory, ".git"),
            "gitdir: main/.git/worktrees/wt\n",
            TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert: the workspace lands in the main checkout's .git and the
        // prefix derives from the main checkout's directory name.
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.git/nitro'.
            ✓ Task ID prefix set to 'main'.
            """);
        Assert.True(File.Exists(Path.Combine(mainGitDirectory, "nitro", "agents.db")));
    }

    [Fact]
    public async Task GitRepository_ExistingNitroWorkspace_TakesPrecedence_AndHintsMigrate()
    {
        // arrange: a .nitro/agents workspace initialized before the
        // directory became a git repository.
        await InitWorkspaceAsync();
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        result.AssertError(
            """
            Already initialized at '.nitro/agents'. Use --force to reinitialize, or `nitro agent init --migrate` to move the workspace into '.git/nitro'.
            """);
        Assert.True(File.Exists(DatabasePath));
        Assert.False(Directory.Exists(GitWorkspaceDirectory));
    }

    [Fact]
    public async Task Migrate_MovesWorkspaceIntoGitDirectory()
    {
        // arrange
        await InitWorkspaceAsync();
        var taskId = await CreateTaskAsync("Survive the move");
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--migrate");

        // assert: the database moved with its data, the old .nitro tree and
        // the fallback .gitignore are gone.
        result.AssertSuccess(
            """
            ✓ Moved agent workspace from '.nitro/agents' to '.git/nitro'.

            If '.nitro/agents' was committed, remove it from git with:
              git rm -r --cached .nitro/agents
            """);
        Assert.Equal("1", await QueryScalarAsync(
            $"SELECT COUNT(*) FROM tasks WHERE id = '{taskId}'", GitDatabasePath));
        Assert.False(Directory.Exists(Path.Combine(WorkingDirectory, ".nitro")));
        Assert.False(File.Exists(Path.Combine(GitWorkspaceDirectory, AgentWorkspace.GitIgnoreFileName)));

        var listResult = await ExecuteCommandAsync("agent", "tasks", "list");
        Assert.Contains(taskId, listResult.StdOut);
    }

    [Fact]
    public async Task Migrate_ImportsAndRemovesLegacyJsonl_AtTheNewLocation()
    {
        // arrange: a workspace with a leftover tasks.jsonl from the retired
        // JSONL sync model that moves along with the migration.
        await InitWorkspaceAsync();
        await File.WriteAllTextAsync(
            Path.Combine(WorkspaceDirectory, AgentWorkspace.LegacyJsonlFileName),
            """
            {"id":"acme-9","title":"From jsonl","description":"","design":"","acceptanceCriteria":"","notes":"","status":"open","priority":2,"type":"task","createdAt":"2026-01-01T00:00:00+00:00","createdBy":"","updatedAt":"2026-01-01T00:00:00+00:00","closeReason":"","deleteReason":"","labels":[],"dependencies":[],"comments":[]}

            """,
            TestContext.Current.CancellationToken);
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--migrate");

        // assert
        result.AssertSuccess(
            """
            ✓ Moved agent workspace from '.nitro/agents' to '.git/nitro'.
            ✓ Imported 1 task from 'tasks.jsonl' and removed it.

            If '.nitro/agents' was committed, remove it from git with:
              git rm -r --cached .nitro/agents
            """);
        Assert.Equal("1", await QueryScalarAsync(
            "SELECT COUNT(*) FROM tasks WHERE id = 'acme-9'", GitDatabasePath));
        Assert.False(File.Exists(
            Path.Combine(GitWorkspaceDirectory, AgentWorkspace.LegacyJsonlFileName)));
    }

    [Fact]
    public async Task Migrate_WithoutGitRepository_Errors()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--migrate");

        // assert
        result.AssertError(
            """
            No git repository found. '--migrate' moves the workspace into the repository's .git directory.
            """);
        Assert.True(File.Exists(DatabasePath));
    }

    [Fact]
    public async Task Migrate_WorkspaceAlreadyInGitDirectory_ReportsNothingToMigrate()
    {
        // arrange
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--migrate");

        // assert
        result.AssertSuccess(
            """
            ✓ Workspace already at '.git/nitro'; nothing to migrate.
            """);
    }

    [Fact]
    public async Task Migrate_CannotCombineWithForce()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "init", "--migrate", "--force");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("'--migrate' cannot be combined with '--force' or '--prefix'.", result.StdErr);
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
        Assert.False(File.Exists(Path.Combine(WorkspaceDirectory, AgentWorkspace.LegacyJsonlFileName)));
        Assert.Equal("acme", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'prefix'"));
    }

    [Fact]
    public async Task EmptyDirectory_ProvisionsMemoryDirectories()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        Assert.Equal(0, result.ExitCode);
        var memoryDirectory = AgentWorkspace.GetMemoryDirectory(WorkspaceDirectory);
        Assert.True(Directory.Exists(AgentWorkspace.GetMemoryCuratedDirectory(memoryDirectory)));
        Assert.True(Directory.Exists(AgentWorkspace.GetMemoryJournalDirectory(memoryDirectory)));
        Assert.True(Directory.Exists(AgentWorkspace.GetMemoryLocalDirectory(memoryDirectory)));
    }

    [Fact]
    public async Task AlreadyInitialized_Force_ProvisionsMissingMemoryDirectories()
    {
        // arrange: a workspace initialized before memory storage existed.
        await InitWorkspaceAsync();
        var memoryDirectory = AgentWorkspace.GetMemoryDirectory(WorkspaceDirectory);
        Directory.Delete(memoryDirectory, recursive: true);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--force");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(Directory.Exists(AgentWorkspace.GetMemoryCuratedDirectory(memoryDirectory)));
        Assert.True(Directory.Exists(AgentWorkspace.GetMemoryJournalDirectory(memoryDirectory)));
        Assert.True(Directory.Exists(AgentWorkspace.GetMemoryLocalDirectory(memoryDirectory)));
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

    /// <summary>
    /// The migration path this bead adds: plain <c>init</c> (no --force)
    /// against an existing database at an upgradable schema version (v3,
    /// this bead's starting point) applies the non-destructive schema
    /// upgrade in place instead of throwing "Already initialized", and
    /// touches neither the prefix nor the gitignore.
    /// </summary>
    [Fact]
    public async Task PlainInit_UpgradesSchemaOnly_When_ExistingVersionIsUpgradable()
    {
        // arrange
        await SeedV3WorkspaceAsync("legacy3");
        var gitIgnorePath = Path.Combine(WorkspaceDirectory, AgentWorkspace.GitIgnoreFileName);
        await File.WriteAllTextAsync(gitIgnorePath, "sentinel\n", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Upgraded agent workspace schema at '.nitro/agents' to v{AgentDatabase.CurrentVersion}.
            """);
        Assert.Equal(
            AgentDatabase.CurrentVersion.ToString(), await QueryScalarAsync("PRAGMA user_version;"));
        Assert.Equal("1", await QueryScalarAsync(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'agent_sessions'"));
        Assert.Equal("legacy3", await QueryScalarAsync("SELECT value FROM config WHERE key = 'prefix'"));
        Assert.Equal(
            "sentinel\n",
            await File.ReadAllTextAsync(gitIgnorePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PlainInit_ImportsAndRemovesLegacyJsonl_When_ExistingVersionIsUpgradable()
    {
        // arrange: an upgradable workspace with a tasks.jsonl left over from
        // the retired JSONL sync model.
        await SeedV3WorkspaceAsync("legacy3");
        var jsonlPath = Path.Combine(WorkspaceDirectory, AgentWorkspace.LegacyJsonlFileName);
        await File.WriteAllTextAsync(
            jsonlPath,
            """
            {"key":"prefix","value":"jsonl"}
            {"id":"legacy3-1","title":"From jsonl","description":"","design":"","acceptanceCriteria":"","notes":"","status":"open","priority":2,"type":"task","createdAt":"2026-01-01T00:00:00+00:00","createdBy":"","updatedAt":"2026-01-01T00:00:00+00:00","closeReason":"","deleteReason":"","labels":[],"dependencies":[],"comments":[]}

            """,
            TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert: the task is imported, the file is gone, and the database's
        // own prefix wins over the one in the file.
        result.AssertSuccess(
            $"""
            ✓ Upgraded agent workspace schema at '.nitro/agents' to v{AgentDatabase.CurrentVersion}.
            ✓ Imported 1 task from 'tasks.jsonl'.
            ✓ Removed 'tasks.jsonl'; the task database is the source of truth.

            If 'tasks.jsonl' was committed, remove it from git with:
              git rm --cached .nitro/agents/tasks.jsonl
            """);
        Assert.False(File.Exists(jsonlPath));
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM tasks WHERE id = 'legacy3-1'"));
        Assert.Equal("legacy3", await QueryScalarAsync("SELECT value FROM config WHERE key = 'prefix'"));
    }

    [Fact]
    public async Task JsonOutput_UpgradeWithLegacyJsonl_ReportsImportedCount()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        await SeedV3WorkspaceAsync("legacy3");
        var jsonlPath = Path.Combine(WorkspaceDirectory, AgentWorkspace.LegacyJsonlFileName);
        await File.WriteAllTextAsync(
            jsonlPath,
            """
            {"id":"legacy3-1","title":"From jsonl","description":"","design":"","acceptanceCriteria":"","notes":"","status":"open","priority":2,"type":"task","createdAt":"2026-01-01T00:00:00+00:00","createdBy":"","updatedAt":"2026-01-01T00:00:00+00:00","closeReason":"","deleteReason":"","labels":[],"dependencies":[],"comments":[]}

            """,
            TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("legacy3", root.GetProperty("prefix").GetString());
        Assert.Equal(0, root.GetProperty("migratedTasks").GetInt32());
        Assert.Equal(1, root.GetProperty("importedCount").GetInt32());
        Assert.False(File.Exists(jsonlPath));
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
            # The agent database is the source of truth for tasks and mail. It is
            # local, machine-specific state and is never committed.
            agents.db
            agents.db-wal
            agents.db-shm

            # The memory index is a disposable, rebuildable cache; the curated and
            # journal markdown under memory/ is the source of truth in git.
            memory/.local/
            """);
    }

    [Fact]
    public async Task AlreadyInitialized_Force_ImportsAndRemovesLegacyJsonl()
    {
        // arrange: a workspace with a tasks.jsonl left over from the retired
        // JSONL sync model.
        await InitWorkspaceAsync();
        var jsonlPath = Path.Combine(WorkspaceDirectory, AgentWorkspace.LegacyJsonlFileName);
        await File.WriteAllTextAsync(
            jsonlPath,
            """
            {"key":"prefix","value":"jsonl"}
            {"id":"acme-9","title":"From jsonl","description":"","design":"","acceptanceCriteria":"","notes":"","status":"open","priority":2,"type":"task","createdAt":"2026-01-01T00:00:00+00:00","createdBy":"","updatedAt":"2026-01-01T00:00:00+00:00","closeReason":"","deleteReason":"","labels":[],"dependencies":[],"comments":[]}

            """,
            TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--force");

        // assert: the task is imported, the file is gone, and the reinit
        // prefix wins over the one in the file.
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'acme'.
            ✓ Imported 1 task from 'tasks.jsonl'.
            ✓ Removed 'tasks.jsonl'; the task database is the source of truth.

            If 'tasks.jsonl' was committed, remove it from git with:
              git rm --cached .nitro/agents/tasks.jsonl
            """);
        Assert.False(File.Exists(jsonlPath));
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM tasks WHERE id = 'acme-9'"));
        Assert.Equal("acme", await QueryScalarAsync("SELECT value FROM config WHERE key = 'prefix'"));
    }

    [Fact]
    public async Task AlreadyInitialized_Force_MalformedJsonl_FailsAndLeavesFileAndDatabase()
    {
        // arrange
        await InitWorkspaceAsync();
        var jsonlPath = Path.Combine(WorkspaceDirectory, AgentWorkspace.LegacyJsonlFileName);
        await File.WriteAllTextAsync(jsonlPath, "not json\n", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--force");

        // assert: the failed import leaves the file for the user to fix or
        // delete, and the database keeps working.
        Assert.Equal(1, result.ExitCode);
        Assert.True(File.Exists(jsonlPath));
        Assert.True(File.Exists(DatabasePath));
        Assert.Equal("acme", await QueryScalarAsync("SELECT value FROM config WHERE key = 'prefix'"));
    }

    [Fact]
    public async Task AlreadyInitialized_Force_PreservesSeededTaskAndMailRows()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Ship the unified workspace");
        var registerResult = await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        Assert.Equal(0, registerResult.ExitCode);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--force");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM tasks"));
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM agents WHERE name = 'bob'"));
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
        Assert.False(File.Exists(Path.Combine(WorkspaceDirectory, AgentWorkspace.LegacyJsonlFileName)));
    }

    [Fact]
    public async Task LegacyAndWorkspaceJsonl_ImportsBothAndRemovesWorkspaceFile()
    {
        // arrange: a legacy-path tasks.jsonl and a workspace-path tasks.jsonl
        // at the same time, each holding a different task.
        Directory.CreateDirectory(LegacyTasksDirectory);
        await File.WriteAllTextAsync(
            LegacyTasksJsonlPath,
            """
            {"key":"prefix","value":"jsonl"}
            {"id":"jsonl-1","title":"From legacy","description":"","design":"","acceptanceCriteria":"","notes":"","status":"open","priority":2,"type":"task","createdAt":"2026-01-01T00:00:00+00:00","createdBy":"","updatedAt":"2026-01-01T00:00:00+00:00","closeReason":"","deleteReason":"","labels":[],"dependencies":[],"comments":[]}

            """,
            TestContext.Current.CancellationToken);
        Directory.CreateDirectory(WorkspaceDirectory);
        var workspaceJsonlPath = Path.Combine(WorkspaceDirectory, AgentWorkspace.LegacyJsonlFileName);
        await File.WriteAllTextAsync(
            workspaceJsonlPath,
            """
            {"key":"prefix","value":"cloned"}
            {"id":"cloned-1","title":"From workspace","description":"","design":"","acceptanceCriteria":"","notes":"","status":"open","priority":2,"type":"task","createdAt":"2026-01-01T00:00:00+00:00","createdBy":"","updatedAt":"2026-01-01T00:00:00+00:00","closeReason":"","deleteReason":"","labels":[],"dependencies":[],"comments":[]}

            """,
            TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'cloned'.
            ✓ Imported 2 tasks from 'tasks.jsonl'.
            ✓ Removed 'tasks.jsonl'; the task database is the source of truth.

            If 'tasks.jsonl' was committed, remove it from git with:
              git rm --cached .nitro/agents/tasks.jsonl

            The legacy '.nitro/tasks' workspace has been migrated. Remove it from git with:
              git rm -r .nitro/tasks
              git add .nitro/agents
            """);
        Assert.Equal("2", await QueryScalarAsync(
            "SELECT COUNT(*) FROM tasks WHERE id IN ('jsonl-1', 'cloned-1')"));
        Assert.False(File.Exists(workspaceJsonlPath));
    }

    [Fact]
    public async Task FreshCloneOfJsonlLayout_ImportsAndRemovesJsonl_KeepsCommittedGitIgnore()
    {
        // arrange: the retired JSONL sync layout as committed (tasks.jsonl
        // and .gitignore present) but no database, as after a fresh git clone.
        Directory.CreateDirectory(WorkspaceDirectory);
        var jsonlPath = Path.Combine(WorkspaceDirectory, AgentWorkspace.LegacyJsonlFileName);
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
            ✓ Removed 'tasks.jsonl'; the task database is the source of truth.

            If 'tasks.jsonl' was committed, remove it from git with:
              git rm --cached .nitro/agents/tasks.jsonl
            """);
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM tasks WHERE id = 'cloned-1'"));
        Assert.False(File.Exists(jsonlPath));
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
        var registerResult = await ExecuteCommandAsync("agent", "register", "--actor", "bob");
        var sendResult = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--no-ping", "--subject", "Status", "--body", "Merged.");

        // assert
        Assert.Equal(0, registerResult.ExitCode);
        Assert.Equal(0, sendResult.ExitCode);
        Assert.Equal(
            AgentDatabase.CurrentVersion.ToString(), await QueryScalarAsync("PRAGMA user_version;"));
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM messages"));

        var listResult = await ExecuteCommandAsync("agent", "tasks", "list");
        Assert.Contains(taskId, listResult.StdOut);
    }

    /// <summary>
    /// Seeds a full v3-shaped unified database (this bead's starting schema:
    /// tasks, agents with role/implicit/client, and mail, but none of the
    /// v4 session tables) directly at the unified path, with the given
    /// prefix in config, mirroring an existing workspace from before this
    /// bead.
    /// </summary>
    private async Task SeedV3WorkspaceAsync(string prefix)
    {
        Directory.CreateDirectory(WorkspaceDirectory);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = TaskStoreSchema.Create;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = AgentRegistrySchema.Create;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = MailStoreSchema.Create;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO config (key, value) VALUES ('prefix', @prefix);";
            command.Parameters.AddWithValue("@prefix", prefix);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA user_version = 3;";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
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
