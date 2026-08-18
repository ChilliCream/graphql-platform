
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class DeferTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "defer", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Defer a task until a future date.

            Usage:
              nitro task defer <id> [options]

            Arguments:
              <id>  The task ID

            Options:
              --until <until> (REQUIRED)  Hide the task from ready work until this ISO 8601 date or timestamp
              --actor <actor>             The acting identity recorded on the audit log (defaults to NITRO_TASK_ACTOR or the OS user name)
              -?, -h, --help              Show help and usage information

            Example:
              nitro task defer "acme-1a2" --until "2026-02-01"
            """);
    }

    [Fact]
    public async Task OpenTask_DefersSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "defer", id, "--until", "2026-02-01");

        // assert
        result.AssertSuccess($"✓ Deferred task '{id}' until 2026-02-01 00:00.");
        Assert.Equal("deferred", await QueryScalarAsync(
            $"SELECT status FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task InProgressTask_DefersSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("task", "update", id, "--status", "in_progress");

        // act
        var result = await ExecuteCommandAsync(
            "task", "defer", id, "--until", "2026-03-15T10:30:00Z");

        // assert
        result.AssertSuccess($"✓ Deferred task '{id}' until 2026-03-15 10:30.");
    }

    [Fact]
    public async Task ClosedTask_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("task", "close", id);

        // act
        var result = await ExecuteCommandAsync("task", "defer", id, "--until", "2026-02-01");

        // assert
        result.AssertError("Only open or in-progress tasks can be deferred.");
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "task", "defer", "acme-999", "--until", "2026-02-01");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }

    [Fact]
    public async Task InvalidUntilDate_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "defer", id, "--until", "not-a-date");

        // assert
        result.AssertError(
            "Invalid date 'not-a-date' for '--until'. Use an ISO 8601 date or timestamp.");
    }
}
