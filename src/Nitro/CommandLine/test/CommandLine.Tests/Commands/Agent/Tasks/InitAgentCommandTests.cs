using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Tasks;

/// <summary>
/// Covers the unified <c>nitro agent init</c> command: fresh init in both
/// the <c>.git/nitro</c> and <c>.nitro/agents</c> layouts, --force,
/// and --migrate, including in-place database schema upgrades.
/// </summary>
public sealed class InitAgentCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    private string GitWorkspaceDirectory => Path.Combine(WorkingDirectory, ".git", "nitro");

    private string GitDatabasePath => Path.Combine(GitWorkspaceDirectory, "agents.db");

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
              --prefix <prefix>                The task ID prefix (defaults to the current directory name)
              --force                          Reinitialize an existing agent workspace
              --migrate                        Move an existing .nitro/agents workspace into the repository's .git/nitro directory
              --database-path <database-path>  Create the workspace in this .nitro directory instead of the nearest existing one
              --output <json>                  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                   Show help and usage information

            Example:
              nitro agent init
              nitro agent init --prefix "app"
              nitro agent init --migrate
              nitro agent init --database-path "./.nitro"
            """);
    }

    [Fact]
    public async Task GitRepository_FreshInit_CreatesWorkspaceInGitDirectory()
    {
        // arrange
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
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
        // arrange
        // A linked-worktree gitdir redirects to the main checkout through commondir.
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

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.git/nitro'.
            ✓ Task ID prefix set to 'main'.
            """);
        Assert.True(File.Exists(Path.Combine(mainGitDirectory, "nitro", "agents.db")));
    }

    [Fact]
    public async Task GitRepository_BareNitroDirectoryAboveRepo_DoesNotHijackInit()
    {
        // arrange
        // Create an empty fallback directory above the repository.
        var ancestorFallback = Path.Combine(
            Path.GetDirectoryName(WorkingDirectory)!, ".nitro", "agents");
        Directory.CreateDirectory(ancestorFallback);
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.git/nitro'.
            ✓ Task ID prefix set to 'acme'.
            """);
        Assert.True(File.Exists(GitDatabasePath));
        Assert.False(File.Exists(Path.Combine(ancestorFallback, "agents.db")));
    }

    [Fact]
    public async Task GitRepository_ExistingNitroWorkspace_TakesPrecedence_AndHintsMigrate()
    {
        // arrange
        // Initialize the fallback workspace before creating the Git directory.
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

        // assert
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
    public async Task Migrate_TargetDirectoryAlreadyExists_Errors()
    {
        // arrange
        await InitWorkspaceAsync();
        Directory.CreateDirectory(GitWorkspaceDirectory);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--migrate");

        // assert
        result.AssertError(
            """
            '.git/nitro' already exists. Remove it before migrating '.nitro/agents'.
            """);
        Assert.True(File.Exists(DatabasePath));
    }

    [Fact]
    public async Task Migrate_WithoutWorkspace_Errors()
    {
        // arrange
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--migrate");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task Migrate_RewritesSessionWorkspacePaths()
    {
        // arrange
        // Seed a session with the workspace path that will be migrated.
        await InitWorkspaceAsync();
        await QueryScalarAsync(
            $"""
            INSERT INTO agent_sessions (
                harness, session_id, host, cwd, workspace_path,
                endpoint_kind, endpoint_addr, started_at, last_beat_at)
            VALUES (
                'claude-code', 's1', 'host', '{WorkingDirectory}', '{WorkspaceDirectory}',
                'none', '', '2026-01-01T00:00:00+00:00', '2026-01-01T00:00:00+00:00');
            """);
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--migrate");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1", await QueryScalarAsync(
            $"SELECT COUNT(*) FROM agent_sessions WHERE workspace_path = '{GitWorkspaceDirectory}'",
            GitDatabasePath));
    }

    [Fact]
    public async Task Migrate_JsonOutput_ReportsFromAndTo()
    {
        // arrange
        await InitWorkspaceAsync();
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--migrate");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(WorkspaceDirectory, root.GetProperty("from").GetString());
        Assert.Equal(GitWorkspaceDirectory, root.GetProperty("to").GetString());
        Assert.False(root.TryGetProperty("importedCount", out _));
    }

    [Fact]
    public async Task PlainInit_Upgrade_PrintsMigrateHint_When_GitRepositoryExists()
    {
        // arrange
        await SeedV3WorkspaceAsync("legacy3");
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Upgraded agent workspace schema at '.nitro/agents' to v{AgentDatabase.CurrentVersion}.

            Run `nitro agent init --migrate` to move this workspace into '.git/nitro'.
            """);
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

    /// <summary>
    /// Tests schema upgrades in an existing <c>.git/nitro</c> workspace.
    /// </summary>
    [Fact]
    public async Task Migrate_WorkspaceAlreadyInGitDirectory_UpgradesStaleSchema()
    {
        // arrange
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));
        await SeedV3WorkspaceAsync("legacy3", GitWorkspaceDirectory);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--migrate");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Upgraded agent workspace schema at '.git/nitro' to v{AgentDatabase.CurrentVersion}.
            """);
        Assert.Equal(
            AgentDatabase.CurrentVersion.ToString(),
            await QueryScalarAsync("PRAGMA user_version;", GitDatabasePath));
        Assert.Equal(
            "legacy3", await QueryScalarAsync("SELECT value FROM config WHERE key = 'prefix'", GitDatabasePath));
    }

    [Fact]
    public async Task Migrate_WorkspaceAlreadyInGitDirectory_NewerSchema_Errors()
    {
        // arrange
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));
        Directory.CreateDirectory(GitWorkspaceDirectory);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using (var connection = new SqliteConnection($"Data Source={GitDatabasePath};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA user_version = {AgentDatabase.CurrentVersion + 1};";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--migrate");

        // assert
        result.AssertError(
            $"""
            The agent workspace was created by a newer version of the Nitro CLI (schema v{AgentDatabase.CurrentVersion + 1}, supported up to v{AgentDatabase.CurrentVersion}). Update the CLI to use it.
            """);
    }

    [Theory]
    [InlineData("--force")]
    [InlineData("--prefix", "app")]
    public async Task Migrate_CannotCombineWithForceOrPrefix(params string[] conflictingArgs)
    {
        // arrange & act
        var result = await ExecuteCommandAsync(["agent", "init", "--migrate", .. conflictingArgs]);

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("'--migrate' cannot be combined with '--force' or '--prefix'.", result.StdErr);
    }

    [Fact]
    public async Task Migrate_CannotCombineWithDatabasePath()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "agent", "init", "--migrate", "--database-path", "./.nitro");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("'--migrate' cannot be combined with '--database-path'.", result.StdErr);
    }

    /// <summary>
    /// Tests creation of a nested workspace with <c>--database-path</c> while
    /// preserving the parent workspace's prefix and task count.
    /// </summary>
    [Fact]
    public async Task DatabasePathOption_ParentHasInitializedBoard_CreatesNestedBoard()
    {
        // arrange
        var parentWorkspaceDirectory = await InitParentWorkspaceAsync("parent");

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--database-path", "./.nitro");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'acme'.
            """);
        Assert.True(File.Exists(DatabasePath));
        Assert.Equal("acme", await QueryScalarAsync("SELECT value FROM config WHERE key = 'prefix'"));
        Assert.Equal("parent", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'prefix'",
            AgentWorkspace.GetDatabasePath(parentWorkspaceDirectory)));
        Assert.Equal("0", await QueryScalarAsync(
            "SELECT COUNT(*) FROM tasks", AgentWorkspace.GetDatabasePath(parentWorkspaceDirectory)));
    }

    /// <summary>
    /// Tests omission of the migration hint when <c>--database-path</c> explicitly
    /// selects a fallback workspace inside a Git repository.
    /// </summary>
    [Fact]
    public async Task DatabasePathOption_GitRepositoryExists_DoesNotPrintMigrateHint()
    {
        // arrange
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--database-path", "./.nitro");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'acme'.
            """);
    }

    /// <summary>
    /// Tests that task creation uses the explicitly initialized nested workspace
    /// without repeating <c>--database-path</c>.
    /// </summary>
    [Fact]
    public async Task DatabasePathOption_LaterCommandsWithoutFlag_UseNestedBoard()
    {
        // arrange
        var parentWorkspaceDirectory = await InitParentWorkspaceAsync("parent");
        var initResult = await ExecuteCommandAsync("agent", "init", "--database-path", "./.nitro");
        Assert.Equal(0, initResult.ExitCode);

        // act
        var taskId = await CreateTaskAsync("Nested board task");

        // assert
        Assert.Equal("1", await QueryScalarAsync($"SELECT COUNT(*) FROM tasks WHERE id = '{taskId}'"));
        Assert.Equal("0", await QueryScalarAsync(
            "SELECT COUNT(*) FROM tasks", AgentWorkspace.GetDatabasePath(parentWorkspaceDirectory)));
    }

    [Fact]
    public async Task DatabasePathOption_BareNitroDirectoryInParent_DoesNotHijackInit()
    {
        // arrange
        // The parent fallback directory has no database.
        var parentFallback = Path.Combine(
            Path.GetDirectoryName(WorkingDirectory)!, ".nitro", "agents");
        Directory.CreateDirectory(parentFallback);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--database-path", "./.nitro");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'acme'.
            """);
        Assert.True(File.Exists(DatabasePath));
        Assert.False(File.Exists(Path.Combine(parentFallback, "agents.db")));
    }

    [Theory]
    [InlineData("./boards")]
    [InlineData("./.nitro/agents")]
    public async Task DatabasePathOption_LastSegmentNotNitro_ErrorsAndCreatesNothing(string value)
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "init", "--database-path", value);

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("'--database-path' must name a '.nitro' directory", result.StdErr);
        Assert.False(Directory.Exists(Path.Combine(WorkingDirectory, ".nitro")));
        Assert.False(Directory.Exists(Path.Combine(WorkingDirectory, "boards")));
    }

    /// <summary>
    /// Tests creation of a workspace below the current directory with a prefix
    /// derived from its project directory.
    /// </summary>
    [Fact]
    public async Task DatabasePathOption_SubdirectoryValue_CreatesBoardAtThatDirectory()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "init", "--database-path", "./sub/.nitro");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'sub'.
            """);
        var databasePath = AgentWorkspace.GetDatabasePath(
            Path.Combine(WorkingDirectory, "sub", ".nitro", "agents"));
        Assert.True(File.Exists(databasePath));
        Assert.Equal("sub", await QueryScalarAsync("SELECT value FROM config WHERE key = 'prefix'", databasePath));
    }

    /// <summary>
    /// Tests workspace creation and prefix initialization at an absolute
    /// <c>--database-path</c> outside the current directory.
    /// </summary>
    [Fact]
    public async Task DatabasePathOption_AbsoluteValueOutsideWorkingDirectory_CreatesBoardAtThatDirectory()
    {
        // arrange
        var externalRoot = Directory.CreateTempSubdirectory("nitro-database-path-test");

        try
        {
            var nitroDirectory = Path.Combine(externalRoot.FullName, "other-project", ".nitro");

            // act
            var result = await ExecuteCommandAsync("agent", "init", "--database-path", nitroDirectory);

            // assert
            result.AssertSuccess(
                """
                ✓ Initialized agent workspace at '.nitro/agents'.
                ✓ Task ID prefix set to 'other-project'.
                """);
            var databasePath = AgentWorkspace.GetDatabasePath(Path.Combine(nitroDirectory, "agents"));
            Assert.True(File.Exists(databasePath));
            Assert.Equal("other-project", await QueryScalarAsync(
                "SELECT value FROM config WHERE key = 'prefix'", databasePath));
        }
        finally
        {
            externalRoot.Delete(recursive: true);
        }
    }

    /// <summary>
    /// Tests that upgrading an explicitly selected nested workspace preserves its prefix.
    /// </summary>
    [Fact]
    public async Task DatabasePathOption_Upgrade_UsesNestedBoardOwnPrefix()
    {
        // arrange
        var workspaceDirectory = Path.Combine(WorkingDirectory, "sub", ".nitro", "agents");
        await SeedV3WorkspaceAsync("legacy-nested", workspaceDirectory);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--database-path", "./sub/.nitro");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Upgraded agent workspace schema at '.nitro/agents' to v{AgentDatabase.CurrentVersion}.
            """);
        var databasePath = AgentWorkspace.GetDatabasePath(workspaceDirectory);
        Assert.Equal(
            AgentDatabase.CurrentVersion.ToString(), await QueryScalarAsync("PRAGMA user_version;", databasePath));
        Assert.Equal("legacy-nested", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'prefix'", databasePath));
    }

    [Fact]
    public async Task DatabasePathOption_AlreadyInitialized_ReturnsError()
    {
        // arrange
        var firstResult = await ExecuteCommandAsync("agent", "init", "--database-path", "./sub/.nitro");
        Assert.Equal(0, firstResult.ExitCode);

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--database-path", "./sub/.nitro");

        // assert
        result.AssertError(
            """
            Already initialized at '.nitro/agents'. Use --force to reinitialize.
            """);
    }

    /// <summary>
    /// Tests that <c>--force</c> changes the explicitly selected workspace's prefix
    /// and preserves the current directory's workspace prefix.
    /// </summary>
    [Fact]
    public async Task DatabasePathOption_Force_ReinitializesOnlyTheFlaggedBoard()
    {
        // arrange
        var nestedInitResult = await ExecuteCommandAsync("agent", "init", "--database-path", "./sub/.nitro");
        Assert.Equal(0, nestedInitResult.ExitCode);
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "init", "--force", "--database-path", "./sub/.nitro", "--prefix", "forced");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized agent workspace at '.nitro/agents'.
            ✓ Task ID prefix set to 'forced'.
            """);
        var nestedDatabasePath = AgentWorkspace.GetDatabasePath(
            Path.Combine(WorkingDirectory, "sub", ".nitro", "agents"));
        Assert.Equal("forced", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'prefix'", nestedDatabasePath));
        Assert.Equal("acme", await QueryScalarAsync("SELECT value FROM config WHERE key = 'prefix'"));
    }

    /// <summary>
    /// Initializes a workspace in the parent of <c>WorkingDirectory</c> with the
    /// supplied prefix and returns its workspace directory.
    /// </summary>
    private async Task<string> InitParentWorkspaceAsync(string prefix)
    {
        var parentDirectory = Path.GetDirectoryName(WorkingDirectory)!;
        var parentWorkspaceDirectory = AgentWorkspace.GetDirectory(parentDirectory);
        Directory.CreateDirectory(parentWorkspaceDirectory);
        var store = new TaskStore(new TestFileSystem(parentDirectory), FakeTime, new AgentDatabase());

        await store.InitializeWorkspaceAsync(
            parentWorkspaceDirectory, prefix, TestContext.Current.CancellationToken);

        return parentWorkspaceDirectory;
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
        Assert.Equal("acme", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'prefix'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsWorkspacePathAndPrefix()
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
        Assert.False(root.TryGetProperty("migratedTasks", out _));
        Assert.False(root.TryGetProperty("importedCount", out _));
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
    /// Tests that plain <c>init</c> upgrades a v3 database while preserving its
    /// prefix and gitignore content.
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
    public async Task AlreadyInitialized_Force_PreservesSeededTaskAndMailRows()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Ship the unified workspace");
        await SeedAgentAsync("bob");

        // act
        var result = await ExecuteCommandAsync("agent", "init", "--force");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM tasks"));
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM agents WHERE name = 'bob'"));
    }

    [Fact]
    public async Task TasksAndMail_ShareTheSameUnifiedWorkspace()
    {
        // arrange
        await InitWorkspaceAsync();
        var taskId = await CreateTaskAsync("Ship the unified workspace");
        SetupInstanceId("host-unified-workspace-test");

        // act
        await SeedAgentAsync("bob");
        var sendResult = await ExecuteCommandAsync(
            "agent", "mail", "send", "--to", "bob", "--subject", "Status", "--body", "Merged.");

        // assert
        Assert.Equal(0, sendResult.ExitCode);
        Assert.Equal(
            AgentDatabase.CurrentVersion.ToString(), await QueryScalarAsync("PRAGMA user_version;"));
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM messages"));

        var listResult = await ExecuteCommandAsync("agent", "tasks", "list");
        Assert.Contains(taskId, listResult.StdOut);
    }

    /// <summary>
    /// Seeds a database marked as schema v3 at the fallback workspace path with
    /// task, agent, and mail tables and the supplied prefix, without session tables.
    /// </summary>
    private Task SeedV3WorkspaceAsync(string prefix)
        => SeedV3WorkspaceAsync(prefix, WorkspaceDirectory);

    /// <summary>
    /// Seeds a database marked as schema v3 at the specified workspace path with
    /// task, agent, and mail tables and the supplied prefix, without session tables.
    /// </summary>
    private async Task SeedV3WorkspaceAsync(string prefix, string workspaceDirectory)
    {
        Directory.CreateDirectory(workspaceDirectory);
        var databasePath = AgentWorkspace.GetDatabasePath(workspaceDirectory);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
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
}
