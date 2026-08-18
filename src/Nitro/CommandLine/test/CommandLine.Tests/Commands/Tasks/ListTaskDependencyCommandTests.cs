
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class ListTaskDependencyCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "dep", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List a task's dependencies.

            Usage:
              nitro task dep list <id> [options]

            Arguments:
              <id>  The task ID

            Options:
              -?, -h, --help  Show help and usage information

            Example:
              nitro task dep list "acme-1a2"
            """);
    }

    [Fact]
    public async Task NoDependencies_PrintsNoDependencies()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "dep", "list", id);

        // assert
        result.AssertSuccess("No dependencies.");
    }

    [Fact]
    public async Task OutgoingDependency_ListsUnderDependenciesSection()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var dependsOnId = await CreateTaskAsync("Write the tokenizer");
        await ExecuteCommandAsync("task", "dep", "add", id, dependsOnId);

        // act
        var result = await ExecuteCommandAsync("task", "dep", "list", id);

        // assert
        result.AssertSuccess(
            $"""
            Dependencies of {id}:
              blocks -> {dependsOnId} (open) Write the tokenizer
            """);
    }

    [Fact]
    public async Task IncomingDependency_ListsUnderDependedOnBySection()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var dependentId = await CreateTaskAsync("Write the docs");
        await ExecuteCommandAsync("task", "dep", "add", dependentId, id);

        // act
        var result = await ExecuteCommandAsync("task", "dep", "list", id);

        // assert
        result.AssertSuccess(
            $"""
            Depended on by:
              {dependentId} (blocks, open) Write the docs
            """);
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "dep", "list", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }
}
