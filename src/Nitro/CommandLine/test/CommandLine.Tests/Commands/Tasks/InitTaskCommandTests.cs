using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class InitTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "init", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Initialize a task workspace in the current directory.

            Usage:
              nitro task init [options]

            Options:
              --prefix <prefix>  The task ID prefix (defaults to the current directory name)
              --force            Reinitialize an existing task workspace
              -?, -h, --help     Show help and usage information

            Example:
              nitro task init
              nitro task init --prefix "app"
            """);
    }

    [Fact]
    public async Task EmptyDirectory_InitializesWorkspace()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "init");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized task workspace at '.nitro/tasks'.
            ✓ Task ID prefix set to 'acme'.
            """);
        Assert.True(File.Exists(DatabasePath));
        Assert.Equal("acme", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'prefix'"));
        (await File.ReadAllTextAsync(
            Path.Combine(WorkspaceDirectory, TaskWorkspace.GitIgnoreFileName),
            TestContext.Current.CancellationToken))
            .MatchInlineSnapshot(
                """
                # The task database is local state and must not be committed.
                *
                !.gitignore
                """);
    }

    [Fact]
    public async Task PrefixOption_NormalizesValue()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "init", "--prefix", "My App!");

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
        var result = await ExecuteCommandAsync("task", "init");

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
        var result = await ExecuteCommandAsync("task", "init", "--force", "--prefix", "core");

        // assert
        result.AssertSuccess(
            """
            ✓ Initialized task workspace at '.nitro/tasks'.
            ✓ Task ID prefix set to 'core'.
            """);
        Assert.Equal("core", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'prefix'"));
    }
}
