
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Tasks;

public sealed class RemoveTaskDependencyCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "remove", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Remove a dependency between two tasks.

            Usage:
              nitro agent tasks dep remove <id> <depends-on-id> [options]

            Arguments:
              <id>             The task ID
              <depends-on-id>  The task this dependency points to

            Options:
              --actor <actor> (REQUIRED)  The actor recorded on the audit log; allocate one with `nitro agent login`
              --output <json>             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help              Show help and usage information

            Example:
              nitro agent tasks dep remove "acme-1a2" "acme-9z8"
            """);
    }

    [Fact]
    public async Task ExistingDependency_RemovesSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var dependsOnId = await CreateTaskAsync("Write the tokenizer");
        await ExecuteCommandAsync("agent", "tasks", "dep", "add", id, dependsOnId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "remove", id, dependsOnId);

        // assert
        result.AssertSuccess($"✓ Removed dependency: '{id}' -> '{dependsOnId}'.");
        Assert.Equal(
            "0",
            await QueryScalarAsync(
                "SELECT COUNT(*) FROM dependencies "
                + $"WHERE task_id = '{id}' AND depends_on_id = '{dependsOnId}'"));
    }

    [Fact]
    public async Task RecordsRemovedEvent_WithTypeAndTarget()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var dependsOnId = await CreateTaskAsync("Write the tokenizer");
        await ExecuteCommandAsync("agent", "tasks", "dep", "add", id, dependsOnId, "--type", "related");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "remove", id, dependsOnId);

        // assert
        result.AssertSuccess($"✓ Removed dependency: '{id}' -> '{dependsOnId}'.");
        Assert.Equal(
            $"related:{dependsOnId}",
            await QueryScalarAsync(
                "SELECT new_value FROM events "
                + $"WHERE task_id = '{id}' AND event_type = 'dependency_removed'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsMinimalDependencyChange()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var dependsOnId = await CreateTaskAsync("Write the tokenizer");
        await ExecuteCommandAsync("agent", "tasks", "dep", "add", id, dependsOnId);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "remove", id, dependsOnId);

        // assert
        result.AssertSuccess(
            $$"""
            {
              "id": "{{id}}",
              "dependsOnId": "{{dependsOnId}}"
            }
            """);
    }

    [Fact]
    public async Task MissingEdge_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var dependsOnId = await CreateTaskAsync("Write the tokenizer");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "remove", id, dependsOnId);

        // assert
        result.AssertError("Dependency does not exist.");
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "remove", "acme-999", "acme-998");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }
}
