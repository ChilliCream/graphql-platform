using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class StatsTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "stats", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show summary statistics for the task workspace.

            Usage:
              nitro agent tasks stats [options]

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks stats
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "stats");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    [Fact]
    public async Task EmptyWorkspace_ReturnsZeroCounts()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "stats");

        // assert
        result.AssertSuccess(
            """
            Tasks: 0
            Ready: 0
            Blocked: 0
            Labels: 0
            Comments: 0
            Events: 0
            """);
    }

    [Fact]
    public async Task MixedStatuses_GroupsByStatusAndCountsReady()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");
        var id2 = await CreateTaskAsync("Write the docs");
        var id3 = await CreateTaskAsync("Deploy service");
        await ExecuteCommandAsync("agent", "tasks", "update", id2, "--status", "in_progress");
        await ExecuteCommandAsync("agent", "tasks", "close", id3);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "stats");

        // assert
        result.AssertSuccess(
            """
            Tasks: 3
              open: 1
              in_progress: 1
              closed: 1
            Ready: 1
            Blocked: 0
            Labels: 0
            Comments: 0
            Events: 5
            """);
    }

    [Fact]
    public async Task BlockedTask_CountsAsBlockedNotReady()
    {
        // arrange
        await InitWorkspaceAsync();
        var baseId = await CreateTaskAsync("Base task");
        await CreateTaskAsync("Dependent task", "--depends-on", baseId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "stats");

        // assert
        result.AssertSuccess(
            """
            Tasks: 2
              open: 2
            Ready: 1
            Blocked: 1
            Labels: 0
            Comments: 0
            Events: 2
            """);
    }

    [Fact]
    public async Task LabelsAndComments_CountsAcrossTasks()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync();
        var id1 = await CreateTaskAsync("Fix the parser", "--label", "api", "--label", "parser");
        await CreateTaskAsync("Write the docs", "--label", "api");
        await InsertCommentAsync(id1, "alice", "Looks good.", cancellationToken);
        await InsertCommentAsync(id1, "test-agent", "Thanks.", cancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "stats");

        // assert
        result.AssertSuccess(
            """
            Tasks: 2
              open: 2
            Ready: 2
            Blocked: 0
            Labels: 2
            Comments: 2
            Events: 2
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsStructuredStats()
    {
        // arrange
        await InitWorkspaceAsync();
        var baseId = await CreateTaskAsync("Base task");
        var dependentId = await CreateTaskAsync("Dependent task", "--depends-on", baseId);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "stats");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "statusCounts": [
                {
                  "value": "open",
                  "count": 2
                }
              ],
              "readyCount": 1,
              "blockedTaskStatuses": {
                "{{dependentId}}": "open"
              },
              "labelCount": 0,
              "commentCount": 0,
              "eventCount": 2
            }
            """);
    }

    /// <summary>
    /// Inserts a comment directly so the test controls created_at.
    /// </summary>
    private async Task InsertCommentAsync(
        string taskId, string author, string text, CancellationToken cancellationToken)
    {
        await using var connection =
            new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO comments (task_id, author, text, created_at) "
            + "VALUES (@taskId, @author, @text, @createdAt)";
        command.Parameters.AddWithValue("@taskId", taskId);
        command.Parameters.AddWithValue("@author", author);
        command.Parameters.AddWithValue("@text", text);
        command.Parameters.AddWithValue("@createdAt", FakeTime.GetUtcNow());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
