using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class InitTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "init", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Initialize a task workspace in the current directory.

            Usage:
              nitro agent tasks init [options]

            Options:
              --prefix <prefix>  The task ID prefix (defaults to the current directory name)
              --force            Reinitialize an existing task workspace
              --output <json>    The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help     Show help and usage information

            Example:
              nitro agent tasks init
              nitro agent tasks init --prefix "app"
            """);
    }

    [Fact]
    public async Task EmptyDirectory_InitializesWorkspace()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "init");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized task workspace at '.nitro/tasks'.
            ✓ Task ID prefix set to 'acme'.
            """);
        Assert.True(File.Exists(DatabasePath));
        Assert.Equal("acme", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'prefix'"));
        var gitIgnoreText = await File.ReadAllTextAsync(
            Path.Combine(WorkspaceDirectory, TaskWorkspace.GitIgnoreFileName),
            TestContext.Current.CancellationToken);
        gitIgnoreText.MatchInlineSnapshot(
            """
            # The task database is local state; tasks.jsonl is the source of truth in git.
            tasks.db
            tasks.db-wal
            tasks.db-shm
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsWorkspacePathAndPrefix()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "init");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(WorkspaceDirectory, root.GetProperty("path").GetString());
        Assert.Equal("acme", root.GetProperty("prefix").GetString());
    }

    [Fact]
    public async Task PrefixOption_NormalizesValue()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "init", "--prefix", "My App!");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized task workspace at '.nitro/tasks'.
            ✓ Task ID prefix set to 'myapp'.
            """);
    }

    [Fact]
    public async Task AlreadyInitialized_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "init");

        // assert
        result.AssertError(
            """
            Already initialized at '.nitro/tasks'. Use --force to reinitialize.
            """);
    }

    [Fact]
    public async Task AlreadyInitialized_Force_ResetsPrefix()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "init", "--force", "--prefix", "core");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized task workspace at '.nitro/tasks'.
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
        var gitIgnorePath = Path.Combine(WorkspaceDirectory, TaskWorkspace.GitIgnoreFileName);
        await File.WriteAllTextAsync(
            gitIgnorePath, "*\n!.gitignore\n", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "init", "--force");

        // assert
        Assert.Equal(0, result.ExitCode);
        var gitIgnoreText = await File.ReadAllTextAsync(
            gitIgnorePath, TestContext.Current.CancellationToken);
        gitIgnoreText.MatchInlineSnapshot(
            """
            # The task database is local state; tasks.jsonl is the source of truth in git.
            tasks.db
            tasks.db-wal
            tasks.db-shm
            """);
    }
}
