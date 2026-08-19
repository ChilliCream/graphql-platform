namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class UpdateTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Update one or more tasks' fields.

            Usage:
              nitro agent tasks update <ids>... [options]

            Arguments:
              <ids>  One or more task IDs

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
              --add-label <add-label>                      A label to add; can be used multiple times
              --remove-label <remove-label>                A label to remove; can be used multiple times
              --parent <parent>                            The parent task ID; the new task becomes its child
              --claim                                      Shorthand for --status in_progress --assignee <actor>
              --actor <actor>                              The acting identity recorded on the audit log (defaults to NITRO_TASK_ACTOR or the OS user name)
              --output <json>                              The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                               Show help and usage information

            Example:
              nitro agent tasks update "app-1a2" --status in_progress
              nitro agent tasks update "app-1a2" --priority p1 --assignee alice
              nitro agent tasks update "app-1a2" "app-9z8" --priority p1
              nitro agent tasks update "app-1a2" --claim
              nitro agent tasks update "app-1a2" --add-label api --remove-label triage
              nitro agent tasks update "app-1a2" --parent "app-9z8"
            """);
    }

    [Fact]
    public async Task NoOptionsGiven_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", "acme-1a2");

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
            "agent", "tasks", "update", "acme-9z9", "--title", "New title");

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
        var createResult = await ExecuteCommandAsync("agent", "tasks", "create", "Fix the parser");
        var id = createResult.StdOut.Split('\'')[1];

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", id, "--title", "");

        // assert
        result.AssertError(
            """
            The title must be 1-500 characters.
            """);
    }

    [Fact]
    public async Task WhitespaceOnlyTitle_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var createResult = await ExecuteCommandAsync("agent", "tasks", "create", "Fix the parser");
        var id = createResult.StdOut.Split('\'')[1];

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", id, "--title", "   ");

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
        var createResult = await ExecuteCommandAsync("agent", "tasks", "create", "Fix the parser");
        var id = createResult.StdOut.Split('\'')[1];

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "update", id,
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
        var createResult = await ExecuteCommandAsync("agent", "tasks", "create", "Fix the parser");
        var id = createResult.StdOut.Split('\'')[1];

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "update", id,
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
            "agent", "tasks", "create", "Fix the parser", "--assignee", "alice");
        var id = createResult.StdOut.Split('\'')[1];

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", id, "--assignee", "");

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
            "agent", "tasks", "update", id, "--title", "Fix the parser properly", "--priority", "p1");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items");

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(id, items[0].GetProperty("id").GetString());
        Assert.Equal("Fix the parser properly", items[0].GetProperty("title").GetString());
        Assert.Equal(1, items[0].GetProperty("priority").GetInt32());
    }

    [Fact]
    public async Task JsonOutput_MultipleTasks_ReturnsAllSnapshots()
    {
        // arrange
        await InitWorkspaceAsync();
        var id1 = await CreateTaskAsync("Fix the parser");
        var id2 = await CreateTaskAsync("Write the docs");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", id1, id2, "--priority", "p1");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items");

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(id1, items[0].GetProperty("id").GetString());
        Assert.Equal(id2, items[1].GetProperty("id").GetString());
        Assert.Equal(1, items[0].GetProperty("priority").GetInt32());
        Assert.Equal(1, items[1].GetProperty("priority").GetInt32());
    }

    [Fact]
    public async Task MultipleTasks_UpdatesAllTasks()
    {
        // arrange
        await InitWorkspaceAsync();
        var id1 = await CreateTaskAsync("Fix the parser");
        var id2 = await CreateTaskAsync("Write the docs");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", id1, id2, "--priority", "p1");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{id1}'.
            ✓ Updated task '{id2}'.
            """);
        Assert.Equal(
            "1|1",
            await QueryScalarAsync(
                $"""
                SELECT group_concat(priority, '|') FROM (
                    SELECT priority FROM tasks WHERE id IN ('{id1}', '{id2}') ORDER BY id
                )
                """));
    }

    [Fact]
    public async Task AtomicFailure_OneBadId_UpdatesNothing()
    {
        // arrange
        await InitWorkspaceAsync();
        var id1 = await CreateTaskAsync("Fix the parser");
        var id2 = await CreateTaskAsync("Write the docs");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "update", id1, "acme-999", id2, "--priority", "p1");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
        Assert.Equal(
            "2|2",
            await QueryScalarAsync(
                $"""
                SELECT group_concat(priority, '|') FROM (
                    SELECT priority FROM tasks WHERE id IN ('{id1}', '{id2}') ORDER BY id
                )
                """));
        Assert.Equal(
            "0",
            await QueryScalarAsync(
                $"""
                SELECT COUNT(*) FROM events
                WHERE task_id IN ('{id1}', '{id2}') AND event_type = 'priority_changed'
                """));
    }

    [Fact]
    public async Task Claim_SetsStatusInProgressAndAssigneeToActor()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", id, "--claim");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{id}'.
            """);
        Assert.Equal(
            "in_progress|test-agent",
            await QueryScalarAsync(
                $"SELECT status || '|' || assignee FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task Claim_ComposesWithOtherOptions()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", id, "--claim", "--priority", "p1");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{id}'.
            """);
        Assert.Equal(
            "in_progress|test-agent|1",
            await QueryScalarAsync(
                $"SELECT status || '|' || assignee || '|' || priority FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task Claim_ExplicitAssigneeWins()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "update", id, "--claim", "--assignee", "alice");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{id}'.
            """);
        Assert.Equal(
            "in_progress|alice",
            await QueryScalarAsync(
                $"SELECT status || '|' || assignee FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task AddLabelRemoveLabel_UpdatesLabelsAndRecordsEvents()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser", "--label", "triage");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "update", id, "--add-label", "api", "--remove-label", "triage");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{id}'.
            """);
        Assert.Equal(
            "api",
            await QueryScalarAsync(
                $"SELECT group_concat(label, ',') FROM labels WHERE task_id = '{id}'"));
        Assert.Equal(
            "label_added:api;label_removed:triage",
            await QueryScalarAsync(
                $"""
                SELECT group_concat(entry, ';') FROM (
                    SELECT event_type || ':' || COALESCE(new_value, old_value) AS entry
                    FROM events
                    WHERE task_id = '{id}' AND event_type IN ('label_added', 'label_removed')
                    ORDER BY id
                )
                """));
    }

    [Fact]
    public async Task AddLabelOnly_SatisfiesNothingToUpdateGuard()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", id, "--add-label", "api");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{id}'.
            """);
        Assert.Equal(
            "api",
            await QueryScalarAsync(
                $"SELECT group_concat(label, ',') FROM labels WHERE task_id = '{id}'"));
    }

    [Fact]
    public async Task Parent_SetsParentChildEdge()
    {
        // arrange
        await InitWorkspaceAsync();
        var parentId = await CreateTaskAsync("Epic");
        var childId = await CreateTaskAsync("Child");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", childId, "--parent", parentId);

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{childId}'.
            """);
        Assert.Equal(
            parentId,
            await QueryScalarAsync(
                $"""
                SELECT depends_on_id FROM dependencies
                WHERE task_id = '{childId}' AND dependency_type = 'parent-child'
                """));
    }

    [Fact]
    public async Task Parent_ReplacesExistingParent()
    {
        // arrange
        await InitWorkspaceAsync();
        var oldParentId = await CreateTaskAsync("Old epic");
        var newParentId = await CreateTaskAsync("New epic");
        var childId = await CreateTaskAsync("Child", "--parent", oldParentId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", childId, "--parent", newParentId);

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{childId}'.
            """);
        Assert.Equal(
            newParentId,
            await QueryScalarAsync(
                $"""
                SELECT depends_on_id FROM dependencies
                WHERE task_id = '{childId}' AND dependency_type = 'parent-child'
                """));
        Assert.Equal(
            "1",
            await QueryScalarAsync(
                $"""
                SELECT COUNT(*) FROM dependencies
                WHERE task_id = '{childId}' AND dependency_type = 'parent-child'
                """));
    }

    [Fact]
    public async Task EmptyParent_ClearsParent()
    {
        // arrange
        await InitWorkspaceAsync();
        var parentId = await CreateTaskAsync("Epic");
        var childId = await CreateTaskAsync("Child", "--parent", parentId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", childId, "--parent", "");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{childId}'.
            """);
        Assert.Equal(
            "0",
            await QueryScalarAsync(
                $"""
                SELECT COUNT(*) FROM dependencies
                WHERE task_id = '{childId}' AND dependency_type = 'parent-child'
                """));
    }

    [Fact]
    public async Task Parent_SelfParent_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", id, "--parent", id);

        // assert
        result.AssertError("A task cannot be its own parent.");
    }

    [Fact]
    public async Task Parent_UnknownParent_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", id, "--parent", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }

    [Fact]
    public async Task Parent_ExistingDependencyOfAnyTypeForSamePair_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        var otherId = await CreateTaskAsync("Write the tokenizer");
        await ExecuteCommandAsync("agent", "tasks", "dep", "add", id, otherId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", id, "--parent", otherId);

        // assert
        result.AssertError("Dependency already exists.");
    }

    [Fact]
    public async Task Parent_CreatesCycle_RejectsBeforeCommit()
    {
        // arrange
        await InitWorkspaceAsync();
        var a = await CreateTaskAsync("Task A");
        var b = await CreateTaskAsync("Task B");
        await ExecuteCommandAsync("agent", "tasks", "dep", "add", a, b);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "update", b, "--parent", a);

        // assert
        result.AssertError($"Setting this parent would create a cycle: {b} -> {a} -> {b}.");
        Assert.Equal(
            "0",
            await QueryScalarAsync(
                "SELECT COUNT(*) FROM dependencies "
                + $"WHERE task_id = '{b}' AND depends_on_id = '{a}' AND dependency_type = 'parent-child'"));
    }

    [Fact]
    public async Task MultipleIds_AppliesAddLabelAndParentToEach()
    {
        // arrange
        await InitWorkspaceAsync();
        var parentId = await CreateTaskAsync("Epic");
        var id1 = await CreateTaskAsync("Fix the parser");
        var id2 = await CreateTaskAsync("Write the docs");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "update", id1, id2, "--add-label", "triage", "--parent", parentId);

        // assert
        result.AssertSuccess(
            $"""
            ✓ Updated task '{id1}'.
            ✓ Updated task '{id2}'.
            """);
        Assert.Equal(
            "2",
            await QueryScalarAsync(
                $"""
                SELECT COUNT(*) FROM labels
                WHERE task_id IN ('{id1}', '{id2}') AND label = 'triage'
                """));
        Assert.Equal(
            "2",
            await QueryScalarAsync(
                $"""
                SELECT COUNT(*) FROM dependencies
                WHERE task_id IN ('{id1}', '{id2}') AND dependency_type = 'parent-child'
                    AND depends_on_id = '{parentId}'
                """));
    }

    [Fact]
    public async Task StatusTransitions_AreRejectedForTerminalStates()
    {
        // arrange
        await InitWorkspaceAsync();
        var createResult = await ExecuteCommandAsync("agent", "tasks", "create", "Fix the parser");
        var id = createResult.StdOut.Split('\'')[1];
        var closeResult = await ExecuteCommandAsync("agent", "tasks", "close", id);
        Assert.Equal(0, closeResult.ExitCode);

        // act
        var closeAttempt = await ExecuteCommandAsync("agent", "tasks", "update", id, "--status", "closed");
        var deleteAttempt = await ExecuteCommandAsync("agent", "tasks", "update", id, "--status", "tombstone");
        var reopenAttempt = await ExecuteCommandAsync("agent", "tasks", "update", id, "--status", "open");

        // assert
        closeAttempt.AssertError(
            """
            Use `nitro agent tasks close` to close a task.
            """);
        deleteAttempt.AssertError(
            """
            Use `nitro agent tasks delete` to delete a task.
            """);
        reopenAttempt.AssertError(
            """
            Use `nitro agent tasks reopen` to reopen a task.
            """);
    }
}
