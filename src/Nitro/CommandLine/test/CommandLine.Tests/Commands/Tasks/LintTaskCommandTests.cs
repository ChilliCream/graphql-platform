namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class LintTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "lint", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Report task quality findings.

            Usage:
              nitro task lint [options]

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro task lint
            """);
    }

    [Fact]
    public async Task NoFindings_PrintsNoFindings()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser", "--description", "Some detail.");

        // act
        var result = await ExecuteCommandAsync("task", "lint");

        // assert
        result.AssertSuccess("No lint findings.");
    }

    [Fact]
    public async Task OpenTaskWithEmptyDescription_IsReported()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "lint");

        // assert
        result.AssertSuccess(
            $"{id}  empty-description: Open task has an empty description.");
    }

    [Fact]
    public async Task DeferredWithoutDeferUntil_IsReported()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser", "--description", "Some detail.");
        var updateResult = await ExecuteCommandAsync("task", "update", id, "--status", "deferred");
        Assert.Equal(0, updateResult.ExitCode);

        // act
        var result = await ExecuteCommandAsync("task", "lint");

        // assert
        result.AssertSuccess(
            $"{id}  deferred-without-defer-until: Deferred task has no defer_until.");
    }

    [Fact]
    public async Task InProgressWithoutAssignee_IsReported()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser", "--description", "Some detail.");
        var updateResult = await ExecuteCommandAsync("task", "update", id, "--status", "in_progress");
        Assert.Equal(0, updateResult.ExitCode);

        // act
        var result = await ExecuteCommandAsync("task", "lint");

        // assert
        result.AssertSuccess(
            $"{id}  in-progress-without-assignee: In-progress task has no assignee.");
    }

    [Fact]
    public async Task EpicWithZeroChildren_IsReported()
    {
        // arrange
        await InitWorkspaceAsync();
        var epicId = await CreateTaskAsync(
            "Parent epic", "--type", "epic", "--description", "Some detail.");

        // act
        var result = await ExecuteCommandAsync("task", "lint");

        // assert
        result.AssertSuccess($"{epicId}  epic-no-children: Epic has zero children.");
    }

    [Fact]
    public async Task ClosedEpicWithOpenChildren_IsReported()
    {
        // arrange
        await InitWorkspaceAsync();
        var epicId = await CreateTaskAsync(
            "Parent epic", "--type", "epic", "--description", "Some detail.");
        await CreateTaskAsync("Child task", "--parent", epicId, "--description", "Some detail.");
        var closeResult = await ExecuteCommandAsync("task", "close", epicId);
        Assert.Equal(0, closeResult.ExitCode);

        // act
        var result = await ExecuteCommandAsync("task", "lint");

        // assert
        result.AssertSuccess(
            $"{epicId}  closed-epic-open-children: Closed epic has open children.");
    }

    [Fact]
    public async Task TombstonedTasks_AreExcluded()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var deleteResult = await ExecuteCommandAsync("task", "delete", id, "--force");
        Assert.Equal(0, deleteResult.ExitCode);

        // act
        var result = await ExecuteCommandAsync("task", "lint");

        // assert
        result.AssertSuccess("No lint findings.");
    }

    [Fact]
    public async Task JsonOutput_ReturnsStructuredFindings()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("task", "lint");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "items": [
                {
                  "taskId": "{{id}}",
                  "rule": "empty-description",
                  "message": "Open task has an empty description."
                }
              ]
            }
            """);
    }
}
