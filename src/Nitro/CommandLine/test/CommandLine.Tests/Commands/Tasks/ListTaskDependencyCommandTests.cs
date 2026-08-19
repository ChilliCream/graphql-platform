
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class ListTaskDependencyCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List a task's dependencies.

            Usage:
              nitro agent tasks dep list <id> [options]

            Arguments:
              <id>  The task ID

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks dep list "acme-1a2"
            """);
    }

    [Fact]
    public async Task NoDependencies_PrintsNoDependencies()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "list", id);

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
        await ExecuteCommandAsync("agent", "tasks", "dep", "add", id, dependsOnId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "list", id);

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
        await ExecuteCommandAsync("agent", "tasks", "dep", "add", dependentId, id);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "list", id);

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
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "list", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }

    [Fact]
    public async Task JsonOutput_ReturnsStructuredDependenciesAndDependents()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var dependsOnId = await CreateTaskAsync("Write the tokenizer");
        await ExecuteCommandAsync("agent", "tasks", "dep", "add", id, dependsOnId);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "list", id);

        // assert
        result.AssertSuccess(
            $$"""
            {
              "dependencies": [
                {
                  "type": "blocks",
                  "dependsOnId": "{{dependsOnId}}",
                  "status": "open",
                  "title": "Write the tokenizer"
                }
              ],
              "dependents": []
            }
            """);
    }

    [Fact]
    public async Task JsonOutput_NoDependencies_ReturnsEmptyArrays()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "list", id);

        // assert
        result.AssertSuccess(
            """
            {
              "dependencies": [],
              "dependents": []
            }
            """);
    }
}
