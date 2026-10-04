namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Tasks;

public sealed class StatusTaskEpicCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "status", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show epics with their child completion counts.

            Usage:
              nitro agent tasks epic status [options]

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks epic status
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "status");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task NoEpics_ReturnsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Standalone task");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "status");

        // assert
        result.AssertSuccess(
            """
            No epics found.
            """);
    }

    [Fact]
    public async Task EpicWithOpenAndClosedChildren_ShowsPartialCompletion()
    {
        // arrange
        await InitWorkspaceAsync();
        var epicId = await CreateTaskAsync("Ship v2", "--type", "epic");
        var child1Id = await CreateTaskAsync("Design API", "--parent", epicId);
        await CreateTaskAsync("Implement API", "--parent", epicId);
        await ExecuteCommandAsync("agent", "tasks", "close", child1Id);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "status");

        // assert
        result.AssertSuccess($"{epicId}  1/2  Ship v2");
    }

    [Fact]
    public async Task EpicWithAllChildrenClosed_ShowsEligibleSuffix()
    {
        // arrange
        await InitWorkspaceAsync();
        var epicId = await CreateTaskAsync("Ship v3", "--type", "epic");
        var childId = await CreateTaskAsync("Design API v3", "--parent", epicId);
        await ExecuteCommandAsync("agent", "tasks", "close", childId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "status");

        // assert
        result.AssertSuccess($"{epicId}  1/1  Ship v3  (eligible for close)");
    }

    [Fact]
    public async Task EpicAlreadyClosed_SuffixOmitted()
    {
        // arrange
        await InitWorkspaceAsync();
        var epicId = await CreateTaskAsync("Ship v4", "--type", "epic");
        var childId = await CreateTaskAsync("Design API v4", "--parent", epicId);
        await ExecuteCommandAsync("agent", "tasks", "close", childId);
        await ExecuteCommandAsync("agent", "tasks", "close", epicId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "status");

        // assert
        result.AssertSuccess($"{epicId}  1/1  Ship v4");
    }

    [Fact]
    public async Task JsonOutput_EpicWithOpenAndClosedChildren_ReturnsStructuredStatus()
    {
        // arrange
        await InitWorkspaceAsync();
        var epicId = await CreateTaskAsync("Ship v2", "--type", "epic");
        var child1Id = await CreateTaskAsync("Design API", "--parent", epicId);
        await CreateTaskAsync("Implement API", "--parent", epicId);
        await ExecuteCommandAsync("agent", "tasks", "close", child1Id);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "status");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "items": [
                {
                  "id": "{{epicId}}",
                  "title": "Ship v2",
                  "status": "open",
                  "total": 2,
                  "closed": 1,
                  "isEligibleForClose": false
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task JsonOutput_NoEpics_ReturnsEmptyItems()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Standalone task");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "status");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }
}
