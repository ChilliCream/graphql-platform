namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Tasks;

public sealed class CloseEligibleTaskEpicCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "close-eligible", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Close every epic whose children are all closed.

            Usage:
              nitro agent tasks epic close-eligible [options]

            Options:
              --actor <actor>  The actor recorded on the audit log; inferred from the current session when omitted
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks epic close-eligible
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "close-eligible");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task NoEligibleEpics_ReturnsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();
        var epicId = await CreateTaskAsync("Ship v5", "--type", "epic");
        await CreateTaskAsync("Design API v5", "--parent", epicId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "close-eligible");

        // assert
        result.AssertSuccess(
            """
            No eligible epics.
            """);
    }

    [Fact]
    public async Task EligibleEpic_ClosesAndRecordsEvent()
    {
        // arrange
        await InitWorkspaceAsync();
        var epicId = await CreateTaskAsync("Ship v6", "--type", "epic");
        var childId = await CreateTaskAsync("Design API v6", "--parent", epicId);
        await ExecuteCommandAsync("agent", "tasks", "close", childId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "close-eligible");

        // assert
        result.AssertSuccess($"✓ Closed epic '{epicId}'.");
        Assert.Equal("closed", await QueryScalarAsync(
            $"SELECT status FROM tasks WHERE id = '{epicId}'"));
        Assert.Equal("All children are closed.", await QueryScalarAsync(
            $"SELECT close_reason FROM tasks WHERE id = '{epicId}'"));
        Assert.Equal("closed", await QueryScalarAsync(
            $"SELECT new_value FROM events WHERE task_id = '{epicId}' AND event_type = 'closed'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsClosedEpics()
    {
        // arrange
        await InitWorkspaceAsync();
        var epicId = await CreateTaskAsync("Ship v10", "--type", "epic");
        var childId = await CreateTaskAsync("Design API v10", "--parent", epicId);
        await ExecuteCommandAsync("agent", "tasks", "close", childId);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "close-eligible");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items");

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(epicId, items[0].GetProperty("id").GetString());
        Assert.Equal("closed", items[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task JsonOutput_NoEligibleEpics_ReturnsEmptyItems()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "close-eligible");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }

    [Fact]
    public async Task MultipleEligibleEpics_ClosesAllInAscendingIdOrder()
    {
        // arrange
        await InitWorkspaceAsync();
        var epicAId = await CreateTaskAsync("Ship v7", "--type", "epic");
        var childAId = await CreateTaskAsync("Design v7", "--parent", epicAId);
        await ExecuteCommandAsync("agent", "tasks", "close", childAId);
        var epicBId = await CreateTaskAsync("Ship v8", "--type", "epic");
        var childBId = await CreateTaskAsync("Design v8", "--parent", epicBId);
        await ExecuteCommandAsync("agent", "tasks", "close", childBId);
        var (firstId, secondId) = string.CompareOrdinal(epicAId, epicBId) < 0
            ? (epicAId, epicBId)
            : (epicBId, epicAId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "close-eligible");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Closed epic '{firstId}'.
            ✓ Closed epic '{secondId}'.
            """);
    }

    [Fact]
    public async Task AlreadyClosedEpic_IsNotReClosed()
    {
        // arrange
        await InitWorkspaceAsync();
        var epicId = await CreateTaskAsync("Ship v9", "--type", "epic");
        var childId = await CreateTaskAsync("Design API v9", "--parent", epicId);
        await ExecuteCommandAsync("agent", "tasks", "close", childId);
        await ExecuteCommandAsync("agent", "tasks", "epic", "close-eligible");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "epic", "close-eligible");

        // assert
        result.AssertSuccess(
            """
            No eligible epics.
            """);
    }
}
