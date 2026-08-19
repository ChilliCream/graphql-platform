namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class UpdateTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "update", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Update a task's fields.

            Usage:
              nitro task update <id> [options]

            Arguments:
              <id>  The task ID

            Options:
              --title <title>                              The task title
              --description <description>                  The task description
              --status <status>                            The task status (open, in_progress, blocked, deferred, closed, or custom)
              --priority <priority>                        The task priority, 0-4 or p0-p4 (0 = critical, 4 = backlog); list/ready also accept a range like 0-1 or p0-p1
              --type <type>                                The task type (task, bug, feature, epic, chore, docs, question, or custom)
              --assignee <assignee>                        The assignee
              --notes <notes>                              The task notes
              --design <design>                            The task design
              --acceptance-criteria <acceptance-criteria>  The acceptance criteria
              --due <due>                                  The due date as an ISO 8601 date or timestamp
              --defer-until <defer-until>                  Hide the task from ready work until this ISO 8601 date or timestamp
              --estimate <estimate>                        The estimated effort in minutes
              --actor <actor>                              The acting identity recorded on the audit log (defaults to NITRO_TASK_ACTOR or the OS user name)
              --output <json>                              The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                               Show help and usage information

            Example:
              nitro task update "app-1a2" --status in_progress
              nitro task update "app-1a2" --priority p1 --assignee alice
            """);
    }

    [Fact]
    public async Task NoOptionsGiven_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "update", "acme-1a2");

        // assert
        result.AssertError(
            """
            Nothing to update. Pass at least one option.
            """);
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "task", "update", "acme-9z9", "--title", "New title");

        // assert
        result.AssertError(
            """
            Task 'acme-9z9' does not exist.
            """);
    }

    [Fact]
    public async Task EmptyTitle_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var createResult = await ExecuteCommandAsync("task", "create", "Fix the parser");
        var id = createResult.StdOut.Split('\'')[1];

        // act
        var result = await ExecuteCommandAsync("task", "update", id, "--title", "");

        // assert
        result.AssertError(
            """
            The title must be 1-500 characters.
            """);
    }

    [Fact]
    public async Task SimpleFields_UpdatesTask_RecordsUpdatedEvent()
    {
        // arrange
        await InitWorkspaceAsync();
        var createResult = await ExecuteCommandAsync("task", "create", "Fix the parser");
        var id = createResult.StdOut.Split('\'')[1];

        // act
        var result = await ExecuteCommandAsync(
            "task", "update", id,
            "--title", "Fix the parser properly",
            "--description", "Investigate the tokenizer",
            "--type", "bug",
            "--notes", "Talked to Alice",
            "--design", "Rewrite the lexer",
            "--acceptance-criteria", "All tests green");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{id}'.
            """);
        Assert.Equal(
            "Fix the parser properly|Investigate the tokenizer|bug|Talked to Alice|"
            + "Rewrite the lexer|All tests green",
            await QueryScalarAsync(
                $"""
                SELECT title || '|' || description || '|' || task_type || '|' || notes || '|'
                    || design || '|' || acceptance_criteria
                FROM tasks WHERE id = '{id}'
                """));
        Assert.Equal(
            "title, description, task_type, notes, design, acceptance_criteria",
            await QueryScalarAsync(
                $"SELECT comment FROM events WHERE task_id = '{id}' AND event_type = 'updated'"));
    }

    [Fact]
    public async Task StatusPriorityAssignee_RecordsTypedEvents()
    {
        // arrange
        await InitWorkspaceAsync();
        var createResult = await ExecuteCommandAsync("task", "create", "Fix the parser");
        var id = createResult.StdOut.Split('\'')[1];

        // act
        var result = await ExecuteCommandAsync(
            "task", "update", id,
            "--status", "in_progress",
            "--priority", "p1",
            "--assignee", "alice");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{id}'.
            """);
        Assert.Equal(
            "status_changed:open->in_progress; priority_changed:P2->P1; assignee_changed:->alice",
            await QueryScalarAsync(
                $"""
                SELECT group_concat(entry, '; ') FROM (
                    SELECT event_type || ':' || COALESCE(old_value, '') || '->'
                        || COALESCE(new_value, '') AS entry
                    FROM events
                    WHERE task_id = '{id}'
                        AND event_type IN ('status_changed', 'priority_changed', 'assignee_changed')
                    ORDER BY id
                )
                """));
    }

    [Fact]
    public async Task EmptyAssignee_ClearsAssignee()
    {
        // arrange
        await InitWorkspaceAsync();
        var createResult = await ExecuteCommandAsync(
            "task", "create", "Fix the parser", "--assignee", "alice");
        var id = createResult.StdOut.Split('\'')[1];

        // act
        var result = await ExecuteCommandAsync("task", "update", id, "--assignee", "");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{id}'.
            """);
        Assert.Equal(
            "1",
            await QueryScalarAsync($"SELECT assignee IS NULL FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsUpdatedTaskSnapshot()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "task", "update", id, "--title", "Fix the parser properly", "--priority", "p1");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(id, root.GetProperty("id").GetString());
        Assert.Equal("Fix the parser properly", root.GetProperty("title").GetString());
        Assert.Equal(1, root.GetProperty("priority").GetInt32());
    }

    [Fact]
    public async Task StatusTransitions_AreRejectedForTerminalStates()
    {
        // arrange
        await InitWorkspaceAsync();
        var createResult = await ExecuteCommandAsync("task", "create", "Fix the parser");
        var id = createResult.StdOut.Split('\'')[1];
        var closeResult = await ExecuteCommandAsync("task", "close", id);
        Assert.Equal(0, closeResult.ExitCode);

        // act
        var closeAttempt = await ExecuteCommandAsync("task", "update", id, "--status", "closed");
        var deleteAttempt = await ExecuteCommandAsync("task", "update", id, "--status", "tombstone");
        var reopenAttempt = await ExecuteCommandAsync("task", "update", id, "--status", "open");

        // assert
        closeAttempt.AssertError(
            """
            Use `nitro task close` to close a task.
            """);
        deleteAttempt.AssertError(
            """
            Use `nitro task delete` to delete a task.
            """);
        reopenAttempt.AssertError(
            """
            Use `nitro task reopen` to reopen a task.
            """);
    }
}
