using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

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
    public async Task GitRepository_BareNitroDirectoryAboveRepo_DoesNotHijackInit()
    {
        // arrange: an empty leftover .nitro/agents in an ancestor directory
        // outside the repository, plus a git repository at the current
        // directory.
        var ancestorFallback = Path.Combine(
            Path.GetDirectoryName(WorkingDirectory)!, ".nitro", "agents");
        Directory.CreateDirectory(ancestorFallback);
        Directory.CreateDirectory(Path.Combine(WorkingDirectory, ".git"));

        // act
        var result = await ExecuteCommandAsync("agent", "init");

        // assert: the nearer repository wins over the farther bare directory.
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
        // arrange: a session row recorded against the pre-migration
        // workspace path.
        await InitWorkspaceAsync();
        await QueryScalarAsync(
            $"""
            INSERT INTO agent_sessions (
                harness, session_id, host, pid, proc_start, cwd, workspace_path,
                endpoint_kind, endpoint_addr, started_at, last_beat_at)
            VALUES (
                'claude-code', 's1', 'host', 1, 'ps', '{WorkingDirectory}', '{WorkspaceDirectory}',
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
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "Merged.");

        // assert
        Assert.Equal(1, sendResult.ExitCode);
        Assert.Contains("message stored, but wake failed: no-live-session.", sendResult.StdErr);
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
}
