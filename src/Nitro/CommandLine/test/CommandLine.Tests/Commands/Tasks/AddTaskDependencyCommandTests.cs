
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class AddTaskDependencyCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "dep", "add", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Add a dependency between two tasks.

            Usage:
              nitro task dep add <id> <depends-on-id> [options]

            Arguments:
              <id>             The task ID
              <depends-on-id>  The task this dependency points to

            Options:
              --type <type>    The dependency type (blocks, parent-child, waits-for, related, ...; default blocks)
              --actor <actor>  The acting identity recorded on the audit log (defaults to NITRO_TASK_ACTOR or the OS user name)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro task dep add "acme-1a2" "acme-9z8"
              nitro task dep add "acme-1a2" "acme-9z8" --type waits-for
            """);
    }

    [Fact]
    public async Task DefaultType_AddsBlockingDependency()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var dependsOnId = await CreateTaskAsync("Write the tokenizer");

        // act
        var result = await ExecuteCommandAsync("task", "dep", "add", id, dependsOnId);

        // assert
        result.AssertSuccess($"✓ Added blocks dependency: '{id}' -> '{dependsOnId}'.");
        Assert.Equal(
            "blocks",
            await QueryScalarAsync(
                "SELECT dependency_type FROM dependencies "
                + $"WHERE task_id = '{id}' AND depends_on_id = '{dependsOnId}'"));
    }

    [Fact]
    public async Task WithType_RecordsGivenTypeAndEvent()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var dependsOnId = await CreateTaskAsync("Write the tokenizer");

        // act
        var result = await ExecuteCommandAsync(
            "task", "dep", "add", id, dependsOnId, "--type", "waits-for");

        // assert
        result.AssertSuccess($"✓ Added waits-for dependency: '{id}' -> '{dependsOnId}'.");
        Assert.Equal(
            $"waits-for:{dependsOnId}",
            await QueryScalarAsync(
                "SELECT new_value FROM events "
                + $"WHERE task_id = '{id}' AND event_type = 'dependency_added'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsMinimalDependencyChange()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var dependsOnId = await CreateTaskAsync("Write the tokenizer");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "task", "dep", "add", id, dependsOnId, "--type", "waits-for");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "id": "{{id}}",
              "dependsOnId": "{{dependsOnId}}",
              "type": "waits-for",
              "cycle": null
            }
            """);
    }

    [Fact]
    public async Task JsonOutput_CreatesCycle_IncludesCycleInResult()
    {
        // arrange
        await InitWorkspaceAsync();
        var a = await CreateTaskAsync("Task A");
        var b = await CreateTaskAsync("Task B");
        await ExecuteCommandAsync("task", "dep", "add", a, b);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("task", "dep", "add", b, a);

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, root.GetProperty("cycle").ValueKind);
        Assert.True(root.GetProperty("cycle").GetArrayLength() > 0);
    }

    [Fact]
    public async Task SelfDependency_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "dep", "add", id, id);

        // assert
        result.AssertError("A task cannot depend on itself.");
    }

    [Fact]
    public async Task DuplicateDependency_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var dependsOnId = await CreateTaskAsync("Write the tokenizer");
        await ExecuteCommandAsync("task", "dep", "add", id, dependsOnId);

        // act
        var result = await ExecuteCommandAsync("task", "dep", "add", id, dependsOnId);

        // assert
        result.AssertError("Dependency already exists.");
    }

    [Fact]
    public async Task MissingDependsOnTask_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "dep", "add", id, "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }

    [Fact]
    public async Task CreatesCycle_PrintsWarning()
    {
        // arrange
        await InitWorkspaceAsync();
        var a = await CreateTaskAsync("Task A");
        var b = await CreateTaskAsync("Task B");
        await ExecuteCommandAsync("task", "dep", "add", a, b);

        // act
        var result = await ExecuteCommandAsync("task", "dep", "add", b, a);

        // assert
        var smaller = string.CompareOrdinal(a, b) < 0 ? a : b;
        var larger = smaller == a ? b : a;
        result.AssertSuccess(
            $"""
            ✓ Added blocks dependency: '{b}' -> '{a}'.
            Warning: dependency cycle: {smaller} -> {larger} -> {smaller}
            """);
    }
}
